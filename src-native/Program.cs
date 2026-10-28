using System;
using System.Collections.Generic;
using System.Drawing;
using System.IO;
using System.Runtime.InteropServices;
using System.Text;
using System.Web.Script.Serialization;
using System.Windows.Forms;
using Microsoft.Win32;
using Microsoft.Web.WebView2.Core;
using Microsoft.Web.WebView2.WinForms;

namespace DesktopToDoWidget
{
    static class Program
    {
        private static System.Threading.Mutex mutex = null;

        [STAThread]
        static void Main()
        {
            try
            {
                LogMain("Main started. ProcessId: " + System.Diagnostics.Process.GetCurrentProcess().Id);

                Application.SetUnhandledExceptionMode(UnhandledExceptionMode.CatchException);
                Application.ThreadException += (s, e) => {
                    LogMain("ThreadException: " + e.Exception.ToString());
                };
                AppDomain.CurrentDomain.UnhandledException += (s, e) => {
                    LogMain("UnhandledException: " + e.ExceptionObject.ToString());
                };

                const string appName = "Local\\DesktopToDoWidget_SingleInstanceMutex";
                bool createdNew;
                mutex = new System.Threading.Mutex(true, appName, out createdNew);
                LogMain("Mutex created. createdNew = " + createdNew);

                if (!createdNew)
                {
                    LogMain("Another instance is running. Finding window to focus...");
                    IntPtr hWnd = FindWindow(null, "Desktop To-Do Widget");
                    if (hWnd != IntPtr.Zero)
                    {
                        SetForegroundWindow(hWnd);
                    }
                    return;
                }

                Application.EnableVisualStyles();
                Application.SetCompatibleTextRenderingDefault(false);
                LogMain("Calling Application.Run...");
                Application.Run(new WidgetForm());
                LogMain("Application.Run returned.");
            }
            catch (Exception ex)
            {
                LogMain("Main Exception: " + ex.ToString());
            }
        }

        private static void LogMain(string msg)
        {
            try
            {
                string path = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "native_runtime.log");
                File.AppendAllText(path, DateTime.Now.ToString("HH:mm:ss.fff") + " [MAIN] " + msg + Environment.NewLine);
            }
            catch { }
        }

        [DllImport("user32.dll", SetLastError = true)]
        static extern IntPtr FindWindow(string lpClassName, string lpWindowName);

        [DllImport("user32.dll")]
        [return: MarshalAs(UnmanagedType.Bool)]
        static extern bool SetForegroundWindow(IntPtr hWnd);
    }

    public class WidgetForm : Form
    {
        // Win32 Constants
        private const int WM_NCLBUTTONDOWN = 0x00A1;
        private const int HTCAPTION = 0x0002;
        private const int WM_SYSCOMMAND = 0x0112;
        private const int SC_MINIMIZE = 0xF020;
        private const int WM_EXITSIZEMOVE = 0x0232;
        private const int WM_HOTKEY = 0x0312;
        private const int CS_DROPSHADOW = 0x00020000;

        private const uint MOD_ALT = 0x0001;
        private const uint MOD_CONTROL = 0x0002;
        private const uint MOD_SHIFT = 0x0004;
        private const uint VK_F9 = 0x78;
        private const uint VK_T = 0x54;

        private const int HOTKEY_ID_F9 = 9001;
        private const int HOTKEY_ID_ALT_SHIFT_T = 9002;
        private const int HOTKEY_ID_CTRL_ALT_T = 9003;

        private static readonly IntPtr HWND_BOTTOM = new IntPtr(1);
        private const uint SWP_NOSIZE = 0x0001;
        private const uint SWP_NOMOVE = 0x0002;
        private const uint SWP_NOACTIVATE = 0x0010;

        // DWM Windows 11 rounded corners
        private const int DWMWA_WINDOW_CORNER_PREFERENCE = 33;
        private const int DWMWCP_ROUND = 2;

        [DllImport("user32.dll")]
        private static extern bool ReleaseCapture();

        [DllImport("user32.dll")]
        private static extern IntPtr SendMessage(IntPtr hWnd, int Msg, IntPtr wParam, IntPtr lParam);

        [DllImport("user32.dll")]
        private static extern bool SetWindowPos(IntPtr hWnd, IntPtr hWndInsertAfter, int X, int Y, int cx, int cy, uint uFlags);

        [DllImport("user32.dll")]
        private static extern bool RegisterHotKey(IntPtr hWnd, int id, uint fsModifiers, uint vk);

        [DllImport("user32.dll")]
        private static extern bool UnregisterHotKey(IntPtr hWnd, int id);

        [DllImport("dwmapi.dll")]
        private static extern int DwmSetWindowAttribute(IntPtr hwnd, int attr, ref int attrValue, int attrSize);

        // Fields
        private WebView2 webView;
        private NotifyIcon trayIcon;
        private ContextMenuStrip trayMenu;
        private JavaScriptSerializer jsonSerializer;
        private string dataFilePath;
        private string appDirectory;
        private bool isMiniMode = false;
        private bool isPositionLocked = false;
        private int fullHeight = 560;
        private int fullWidth = 350;
        private int miniHeight = 380;
        private const int SNAP_THRESHOLD = 45;

        protected override CreateParams CreateParams
        {
            get
            {
                CreateParams cp = base.CreateParams;
                cp.ClassStyle |= CS_DROPSHADOW;
                return cp;
            }
        }

        public WidgetForm()
        {
            this.Text = "Desktop To-Do Widget";
            this.FormBorderStyle = FormBorderStyle.None;
            this.StartPosition = FormStartPosition.Manual;
            this.ShowInTaskbar = false;
            this.BackColor = Color.Black;
            this.Width = fullWidth;
            this.Height = fullHeight;

            jsonSerializer = new JavaScriptSerializer();
            appDirectory = AppDomain.CurrentDomain.BaseDirectory;

            string appDataDir = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData), "windows11-todo-widget");
            if (!Directory.Exists(appDataDir))
            {
                Directory.CreateDirectory(appDataDir);
            }
            dataFilePath = Path.Combine(appDataDir, "todo-widget-data.json");

            RestoreWindowBounds();
            InitTray();

            this.FormClosing += (s, e) => {
                Log("FormClosing triggered: CloseReason = " + e.CloseReason + ", Cancel = " + e.Cancel);
            };
            this.FormClosed += (s, e) => {
                Log("FormClosed triggered: CloseReason = " + e.CloseReason);
            };
        }

        private void Log(string msg)
        {
            try
            {
                File.AppendAllText(Path.Combine(appDirectory, "native_runtime.log"),
                    DateTime.Now.ToString("HH:mm:ss.fff") + " " + msg + Environment.NewLine);
            }
            catch { }
        }

        protected override void OnLoad(EventArgs e)
        {
            base.OnLoad(e);
            Log("Form OnLoad started");
            InitWebView();
        }

        protected override void OnHandleCreated(EventArgs e)
        {
            base.OnHandleCreated(e);

            try
            {
                int cornerPref = DWMWCP_ROUND;
                DwmSetWindowAttribute(this.Handle, DWMWA_WINDOW_CORNER_PREFERENCE, ref cornerPref, sizeof(int));
            }
            catch { }

            RegisterHotKey(this.Handle, HOTKEY_ID_F9, 0, VK_F9);
            RegisterHotKey(this.Handle, HOTKEY_ID_ALT_SHIFT_T, MOD_ALT | MOD_SHIFT, VK_T);
            RegisterHotKey(this.Handle, HOTKEY_ID_CTRL_ALT_T, MOD_CONTROL | MOD_ALT, VK_T);

            SendToWallpaperLayer();
        }

        protected override void OnFormClosing(FormClosingEventArgs e)
        {
            UnregisterHotKey(this.Handle, HOTKEY_ID_F9);
            UnregisterHotKey(this.Handle, HOTKEY_ID_ALT_SHIFT_T);
            UnregisterHotKey(this.Handle, HOTKEY_ID_CTRL_ALT_T);

            if (trayIcon != null)
            {
                trayIcon.Visible = false;
                trayIcon.Dispose();
            }

            base.OnFormClosing(e);
        }

        protected override void WndProc(ref Message m)
        {
            if (m.Msg == WM_SYSCOMMAND && (m.WParam.ToInt32() & 0xFFF0) == SC_MINIMIZE)
            {
                return;
            }

            if (m.Msg == WM_EXITSIZEMOVE)
            {
                SnapToEdges();
                SaveCurrentBounds();
            }

            if (m.Msg == WM_HOTKEY)
            {
                ToggleWidgetFocus();
                return;
            }

            base.WndProc(ref m);
        }

        private void SendToWallpaperLayer()
        {
            SetWindowPos(this.Handle, HWND_BOTTOM, 0, 0, 0, 0, SWP_NOMOVE | SWP_NOSIZE | SWP_NOACTIVATE);
        }

        private void ToggleWidgetFocus()
        {
            if (Form.ActiveForm == this)
            {
                SendToWallpaperLayer();
            }
            else
            {
                this.BringToFront();
                this.Activate();
                this.Focus();
                if (webView != null) webView.Focus();
            }
        }

        private void SnapToEdges()
        {
            Rectangle wa = Screen.FromControl(this).WorkingArea;
            int x = this.Left;
            int y = this.Top;

            if (Math.Abs(x - wa.Left) <= SNAP_THRESHOLD)
                x = wa.Left;
            else if (Math.Abs((x + this.Width) - wa.Right) <= SNAP_THRESHOLD)
                x = wa.Right - this.Width;

            if (Math.Abs(y - wa.Top) <= SNAP_THRESHOLD)
                y = wa.Top;
            else if (Math.Abs((y + this.Height) - wa.Bottom) <= SNAP_THRESHOLD)
                y = wa.Bottom - this.Height;

            this.Location = new Point(x, y);
        }

        private void RestoreWindowBounds()
        {
            try
            {
                if (File.Exists(dataFilePath))
                {
                    string json = File.ReadAllText(dataFilePath, Encoding.UTF8);
                    Dictionary<string, object> root = jsonSerializer.Deserialize<Dictionary<string, object>>(json);
                    if (root != null && root.ContainsKey("settings"))
                    {
                        var settings = root["settings"] as Dictionary<string, object>;
                        if (settings != null)
                        {
                            if (settings.ContainsKey("locked"))
                            {
                                isPositionLocked = Convert.ToBoolean(settings["locked"]);
                            }
                            if (settings.ContainsKey("miniMode"))
                            {
                                isMiniMode = Convert.ToBoolean(settings["miniMode"]);
                            }
                            if (settings.ContainsKey("bounds"))
                            {
                                var b = settings["bounds"] as Dictionary<string, object>;
                                if (b != null)
                                {
                                    int x = Convert.ToInt32(b["x"]);
                                    int y = Convert.ToInt32(b["y"]);
                                    int w = Convert.ToInt32(b["width"]);
                                    int h = Convert.ToInt32(b["height"]);

                                    Rectangle screenBounds = Screen.PrimaryScreen.WorkingArea;
                                    x = Math.Max(screenBounds.Left, Math.Min(screenBounds.Right - w, x));
                                    y = Math.Max(screenBounds.Top, Math.Min(screenBounds.Bottom - h, y));

                                    this.Location = new Point(x, y);
                                    this.Size = new Size(Math.Max(280, w), Math.Max(200, h));
                                    return;
                                }
                            }
                        }
                    }
                }
            }
            catch { }

            Rectangle primary = Screen.PrimaryScreen.WorkingArea;
            this.Location = new Point(primary.Right - 380, primary.Top + 40);
            this.Size = new Size(fullWidth, fullHeight);
        }

        private void SaveCurrentBounds()
        {
            try
            {
                Dictionary<string, object> root = LoadStoredDataDirect();
                var settings = root.ContainsKey("settings") ? root["settings"] as Dictionary<string, object> : new Dictionary<string, object>();
                if (settings == null) settings = new Dictionary<string, object>();

                var bounds = new Dictionary<string, object>
                {
                    { "x", this.Left },
                    { "y", this.Top },
                    { "width", this.Width },
                    { "height", this.Height }
                };
                settings["bounds"] = bounds;
                settings["miniMode"] = isMiniMode;
                settings["locked"] = isPositionLocked;
                root["settings"] = settings;

                File.WriteAllText(dataFilePath, jsonSerializer.Serialize(root), Encoding.UTF8);
            }
            catch { }
        }

        private Dictionary<string, object> LoadStoredDataDirect()
        {
            if (File.Exists(dataFilePath))
            {
                try
                {
                    string json = File.ReadAllText(dataFilePath, Encoding.UTF8);
                    var dict = jsonSerializer.Deserialize<Dictionary<string, object>>(json);
                    if (dict != null) return dict;
                }
                catch { }
            }

            return new Dictionary<string, object>
            {
                { "tasks", new object[0] },
                { "categories", new string[] { "Personal", "Work" } },
                { "settings", new Dictionary<string, object> {
                    { "locked", false },
                    { "opacity", 1.0 },
                    { "miniMode", false },
                    { "autoStart", true },
                    { "themeColor", "#000000" }
                }}
            };
        }

        private void InitTray()
        {
            trayIcon = new NotifyIcon();
            string iconPath = Path.Combine(appDirectory, "assets", "icon.ico");
            if (File.Exists(iconPath))
            {
                trayIcon.Icon = new Icon(iconPath);
            }
            else
            {
                trayIcon.Icon = SystemIcons.Application;
            }
            trayIcon.Text = "Desktop To-Do Widget";
            trayIcon.Visible = true;

            trayIcon.DoubleClick += (s, e) => ToggleWidgetFocus();

            trayMenu = new ContextMenuStrip();
            RebuildTrayMenu();
            trayIcon.ContextMenuStrip = trayMenu;
        }

        private void RebuildTrayMenu()
        {
            trayMenu.Items.Clear();

            var titleItem = new ToolStripMenuItem("Desktop To-Do Widget") { Enabled = false };
            trayMenu.Items.Add(titleItem);
            trayMenu.Items.Add(new ToolStripSeparator());

            var showItem = new ToolStripMenuItem(this.Visible ? "Focus Widget" : "Show Widget", null, (s, e) => ToggleWidgetFocus());
            trayMenu.Items.Add(showItem);

            var lockItem = new ToolStripMenuItem(isPositionLocked ? "Position: Locked 🔒" : "Position: Draggable 🔓", null, (s, e) => {
                isPositionLocked = !isPositionLocked;
                SaveCurrentBounds();
                NotifyWeb("tray-action", "toggle-lock", isPositionLocked);
                RebuildTrayMenu();
            });
            trayMenu.Items.Add(lockItem);

            var miniItem = new ToolStripMenuItem(isMiniMode ? "Expand to Full Mode" : "Collapse to Tasks Only", null, (s, e) => {
                ApplyMiniMode(!isMiniMode);
                NotifyWeb("tray-action", "toggle-mini", isMiniMode);
                RebuildTrayMenu();
            });
            trayMenu.Items.Add(miniItem);

            trayMenu.Items.Add(new ToolStripSeparator());

            var pinTopLeft = new ToolStripMenuItem("Pin to Top-Left 📌", null, (s, e) => PinToCorner("top-left"));
            var pinTopRight = new ToolStripMenuItem("Pin to Top-Right 📌", null, (s, e) => PinToCorner("top-right"));
            var pinBottomLeft = new ToolStripMenuItem("Pin to Bottom-Left 📌", null, (s, e) => PinToCorner("bottom-left"));
            var pinBottomRight = new ToolStripMenuItem("Pin to Bottom-Right 📌", null, (s, e) => PinToCorner("bottom-right"));
            var resetPos = new ToolStripMenuItem("Reset Position", null, (s, e) => ResetPosition());

            trayMenu.Items.Add(pinTopLeft);
            trayMenu.Items.Add(pinTopRight);
            trayMenu.Items.Add(pinBottomLeft);
            trayMenu.Items.Add(pinBottomRight);
            trayMenu.Items.Add(resetPos);

            trayMenu.Items.Add(new ToolStripSeparator());

            var autoStartItem = new ToolStripMenuItem("Start with Windows", null, (s, e) => {
                bool current = IsAutoStartEnabled();
                SetAutoStart(!current);
                RebuildTrayMenu();
            }) { Checked = IsAutoStartEnabled() };
            trayMenu.Items.Add(autoStartItem);

            trayMenu.Items.Add(new ToolStripSeparator());
            var exitItem = new ToolStripMenuItem("Exit", null, (s, e) => Application.Exit());
            trayMenu.Items.Add(exitItem);
        }

        private void PinToCorner(string corner)
        {
            Rectangle wa = Screen.FromControl(this).WorkingArea;
            switch (corner.ToLowerInvariant())
            {
                case "top-left":
                    this.Location = new Point(wa.Left, wa.Top);
                    break;
                case "top-right":
                    this.Location = new Point(wa.Right - this.Width, wa.Top);
                    break;
                case "bottom-left":
                    this.Location = new Point(wa.Left, wa.Bottom - this.Height);
                    break;
                case "bottom-right":
                    this.Location = new Point(wa.Right - this.Width, wa.Bottom - this.Height);
                    break;
            }
            SaveCurrentBounds();
        }

        private void ResetPosition()
        {
            Rectangle wa = Screen.FromControl(this).WorkingArea;
            this.Size = new Size(fullWidth, fullHeight);
            this.Location = new Point(wa.Right - 380, wa.Top + 40);
            isMiniMode = false;
            SaveCurrentBounds();
        }

        private void ApplyMiniMode(bool mini)
        {
            isMiniMode = mini;
            if (mini)
            {
                this.Size = new Size(this.Width, miniHeight);
            }
            else
            {
                this.Size = new Size(this.Width, fullHeight);
            }
            SaveCurrentBounds();
        }

        private bool IsAutoStartEnabled()
        {
            try
            {
                using (RegistryKey key = Registry.CurrentUser.OpenSubKey(@"Software\Microsoft\Windows\CurrentVersion\Run", false))
                {
                    return key != null && key.GetValue("DesktopToDoWidget") != null;
                }
            }
            catch { return false; }
        }

        private void SetAutoStart(bool enable)
        {
            try
            {
                using (RegistryKey key = Registry.CurrentUser.OpenSubKey(@"Software\Microsoft\Windows\CurrentVersion\Run", true))
                {
                    if (key != null)
                    {
                        if (enable)
                        {
                            string exePath = Application.ExecutablePath;
                            key.SetValue("DesktopToDoWidget", "\"" + exePath + "\"");
                        }
                        else
                        {
                            key.DeleteValue("DesktopToDoWidget", false);
                        }
                    }
                }
            }
            catch { }
        }

        private async void InitWebView()
        {
            try
            {
                Log("InitWebView starting...");
                webView = new WebView2
                {
                    Dock = DockStyle.Fill,
                    DefaultBackgroundColor = Color.Black
                };
                this.Controls.Add(webView);

                string wvUserData = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData), "windows11-todo-widget", "webview2_data");
                var env = await CoreWebView2Environment.CreateAsync(null, wvUserData);
                Log("CoreWebView2Environment created");
                await webView.EnsureCoreWebView2Async(env);
                Log("EnsureCoreWebView2Async finished");

                webView.CoreWebView2.Settings.IsStatusBarEnabled = false;
                webView.CoreWebView2.Settings.AreDevToolsEnabled = false;
                webView.CoreWebView2.Settings.AreDefaultContextMenusEnabled = false;
                webView.CoreWebView2.Settings.IsZoomControlEnabled = false;

                webView.CoreWebView2.SetVirtualHostNameToFolderMapping("app.widget", appDirectory, CoreWebView2HostResourceAccessKind.Allow);
                Log("Virtual host mapped to " + appDirectory);

                string bridgeScript = @"
                (function() {
                    const pendingCalls = new Map();
                    let callIdCounter = 0;
                    const trayListeners = [];

                    window.chrome.webview.addEventListener('message', (event) => {
                        const msg = event.data;
                        if (!msg) return;
                        if (msg.type === 'response' && msg.id !== undefined) {
                            const resolver = pendingCalls.get(msg.id);
                            if (resolver) {
                                pendingCalls.delete(msg.id);
                                resolver(msg.result);
                            }
                        } else if (msg.type === 'tray-action') {
                            trayListeners.forEach(cb => {
                                try { cb(msg.action, msg.payload); } catch(e) {}
                            });
                        }
                    });

                    function callNative(action, payload) {
                        return new Promise((resolve) => {
                            const id = ++callIdCounter;
                            pendingCalls.set(id, resolve);
                            window.chrome.webview.postMessage({ id, action, payload });
                        });
                    }

                    window.widgetAPI = {
                        loadData: () => callNative('loadData'),
                        saveData: (data) => callNative('saveData', data),
                        minimizeWindow: () => callNative('minimizeWindow'),
                        closeWindow: () => callNative('closeWindow'),
                        toggleAlwaysOnTop: (flag) => callNative('toggleAlwaysOnTop', flag),
                        setMiniMode: (isMini) => callNative('setMiniMode', isMini),
                        setWindowOpacity: (opacity) => callNative('setWindowOpacity', opacity),
                        resetPosition: () => callNative('resetPosition'),
                        pinToCorner: (corner) => callNative('pinToCorner', corner),
                        getAutoStart: () => callNative('getAutoStart'),
                        setAutoStart: (enable) => callNative('setAutoStart', enable),
                        sendNotification: (title, body) => callNative('sendNotification', { title, body }),
                        startDrag: () => callNative('startDrag'),
                        onTrayAction: (callback) => {
                            trayListeners.push(callback);
                        }
                    };

                    document.addEventListener('mousedown', function(e) {
                        if (e.button !== 0) return;
                        const widget = document.getElementById('widget-container');
                        if (widget && widget.classList.contains('position-locked')) return;

                        let el = e.target;
                        while (el && el !== document.body) {
                            if (el.classList && (
                                el.classList.contains('no-drag') || 
                                el.classList.contains('task-card') || 
                                el.classList.contains('color-studio-container') || 
                                el.classList.contains('search-bar') ||
                                el.classList.contains('google-tasks-date-popover') ||
                                el.classList.contains('settings-drawer') ||
                                el.classList.contains('modal-box')
                            )) {
                                return;
                            }
                            const tag = (el.tagName || '').toLowerCase();
                            if (tag === 'button' || tag === 'input' || tag === 'select' || tag === 'textarea' || tag === 'label') {
                                return;
                            }
                            if (el.id === 'full-header' || el.id === 'collapsed-bar' || (el.classList && (el.classList.contains('widget-header') || el.classList.contains('collapsed-bar')))) {
                                window.chrome.webview.postMessage({ action: 'startDrag' });
                                return;
                            }
                            el = el.parentElement;
                        }
                    }, true);
                })();
                ";

                await webView.CoreWebView2.AddScriptToExecuteOnDocumentCreatedAsync(bridgeScript);
                webView.CoreWebView2.WebMessageReceived += OnWebMessageReceived;

                webView.CoreWebView2.Navigate("https://app.widget/src/index.html");
                Log("Navigated to https://app.widget/src/index.html");
            }
            catch (Exception ex)
            {
                Log("InitWebView Exception: " + ex.ToString());
            }
        }

        private void OnWebMessageReceived(object sender, CoreWebView2WebMessageReceivedEventArgs e)
        {
            try
            {
                string rawJson = e.WebMessageAsJson;
                Log("WebMessage received: " + rawJson);
                Dictionary<string, object> msg = jsonSerializer.Deserialize<Dictionary<string, object>>(rawJson);
                if (msg == null) return;

                int id = msg.ContainsKey("id") ? Convert.ToInt32(msg["id"]) : 0;
                string action = msg.ContainsKey("action") ? Convert.ToString(msg["action"]) : "";
                object payload = msg.ContainsKey("payload") ? msg["payload"] : null;

                object result = null;

                switch (action)
                {
                    case "startDrag":
                        if (!isPositionLocked)
                        {
                            ReleaseCapture();
                            SendMessage(this.Handle, WM_NCLBUTTONDOWN, (IntPtr)HTCAPTION, IntPtr.Zero);
                        }
                        break;

                    case "loadData":
                        result = LoadStoredDataDirect();
                        break;

                    case "saveData":
                        if (payload != null)
                        {
                            File.WriteAllText(dataFilePath, jsonSerializer.Serialize(payload), Encoding.UTF8);
                            result = true;
                        }
                        break;

                    case "setMiniMode":
                        bool mini = Convert.ToBoolean(payload);
                        ApplyMiniMode(mini);
                        result = mini;
                        break;

                    case "pinToCorner":
                        string corner = Convert.ToString(payload);
                        PinToCorner(corner);
                        result = true;
                        break;

                    case "resetPosition":
                        ResetPosition();
                        result = true;
                        break;

                    case "getAutoStart":
                        result = IsAutoStartEnabled();
                        break;

                    case "setAutoStart":
                        bool enable = Convert.ToBoolean(payload);
                        SetAutoStart(enable);
                        result = enable;
                        break;

                    case "closeWindow":
                        Application.Exit();
                        break;

                    case "minimizeWindow":
                        SendToWallpaperLayer();
                        result = true;
                        break;
                }

                if (id > 0)
                {
                    var response = new Dictionary<string, object>
                    {
                        { "type", "response" },
                        { "id", id },
                        { "result", result }
                    };
                    webView.CoreWebView2.PostWebMessageAsJson(jsonSerializer.Serialize(response));
                }
            }
            catch (Exception ex)
            {
                Log("OnWebMessageReceived Exception: " + ex.ToString());
            }
        }

        private void NotifyWeb(string type, string action, object payload)
        {
            if (webView == null || webView.CoreWebView2 == null) return;
            try
            {
                var msg = new Dictionary<string, object>
                {
                    { "type", type },
                    { "action", action },
                    { "payload", payload }
                };
                webView.CoreWebView2.PostWebMessageAsJson(jsonSerializer.Serialize(msg));
            }
            catch { }
        }
    }
}
