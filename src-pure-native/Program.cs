using System;
using System.Collections.Generic;
using System.Drawing;
using System.Drawing.Drawing2D;
using System.Drawing.Text;
using System.IO;
using System.Runtime.InteropServices;
using System.Text;
using System.Web.Script.Serialization;
using System.Windows.Forms;
using Microsoft.Win32;

namespace DesktopToDoWidget.PureNative
{
    // ==========================================================================
    // Data Models
    // ==========================================================================
    public class TaskItem
    {
        public string id { get; set; }
        public string title { get; set; }
        public bool completed { get; set; }
        public string category { get; set; }
        public string dueDate { get; set; }
        public bool isDaily { get; set; }
        public long createdAt { get; set; }

        public TaskItem()
        {
            id = Guid.NewGuid().ToString();
            title = "";
            completed = false;
            category = "Personal";
            dueDate = "";
            isDaily = false;
            createdAt = DateTime.UtcNow.Ticks;
        }
    }

    public class WidgetSettings
    {
        public bool locked { get; set; }
        public double opacity { get; set; }
        public bool miniMode { get; set; }
        public bool autoStart { get; set; }
        public string themeColor { get; set; }
        public Dictionary<string, object> bounds { get; set; }

        public WidgetSettings()
        {
            locked = false;
            opacity = 1.0;
            miniMode = false;
            autoStart = true;
            themeColor = "#000000";
        }
    }

    // ==========================================================================
    // Program Entry Point
    // ==========================================================================
    static class Program
    {
        private static System.Threading.Mutex mutex = null;

        [STAThread]
        static void Main()
        {
            const string appName = "Local\\DesktopToDoWidget_PureNative_SingleInstanceMutex";
            bool createdNew;
            mutex = new System.Threading.Mutex(true, appName, out createdNew);

            if (!createdNew)
            {
                IntPtr hWnd = FindWindow(null, "Desktop To-Do Widget (Pure Native)");
                if (hWnd != IntPtr.Zero)
                {
                    SetForegroundWindow(hWnd);
                }
                return;
            }

            Application.EnableVisualStyles();
            Application.SetCompatibleTextRenderingDefault(false);
            Application.Run(new PureNativeWidgetForm());
        }

        [DllImport("user32.dll", SetLastError = true)]
        static extern IntPtr FindWindow(string lpClassName, string lpWindowName);

        [DllImport("user32.dll")]
        [return: MarshalAs(UnmanagedType.Bool)]
        static extern bool SetForegroundWindow(IntPtr hWnd);
    }

    // ==========================================================================
    // Pure Native Widget Form
    // ==========================================================================
    public class PureNativeWidgetForm : Form
    {
        // Win32 Constants
        private const int WM_NCLBUTTONDOWN = 0x00A1;
        private const int HTCAPTION = 0x0002;
        private const int WM_SYSCOMMAND = 0x0112;
        private const int SC_MINIMIZE = 0xF020;
        private const int WM_EXITSIZEMOVE = 0x0232;
        private const int WM_HOTKEY = 0x0312;
        private const int CS_DROPSHADOW = 0x00020000;
        private const int WS_THICKFRAME = 0x00040000;
        private const int WM_NCCALCSIZE = 0x0083;

        private const int HTLEFT = 10;
        private const int HTRIGHT = 11;
        private const int HTTOP = 12;
        private const int HTTOPLEFT = 13;
        private const int HTTOPRIGHT = 14;
        private const int HTBOTTOM = 15;
        private const int HTBOTTOMLEFT = 16;
        private const int HTBOTTOMRIGHT = 17;

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

        private const int DWMWA_WINDOW_CORNER_PREFERENCE = 33;
        private const int DWMWCP_ROUND = 2;

        private const int SNAP_THRESHOLD = 45;
        private const int RESIZE_HANDLE_SIZE = 8;
        private const int DEFAULT_WIDTH = 350;
        private const int DEFAULT_HEIGHT = 560;
        private const int MINI_HEIGHT = 380;

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

        [DllImport("psapi.dll")]
        private static extern int EmptyWorkingSet(IntPtr hwProc);

        // State Data
        private List<TaskItem> tasks = new List<TaskItem>();
        private List<string> categories = new List<string> { "Personal", "Work" };
        private WidgetSettings settings = new WidgetSettings();
        private string dataFilePath;
        private JavaScriptSerializer jsonSerializer;

        // UI State
        private string activeCategoryFilter = "All"; // "All", "Personal", "Work"
        private string searchFilter = "";
        private bool isSearchOpen = false;
        private bool isSettingsOpen = false;
        private bool isCompletedCollapsed = false;
        private int scrollOffsetY = 0;
        private int maxScrollY = 0;

        // New Task Input Form
        private TextBox txtNewTaskTitle;
        private TextBox txtSearch;
        private string newTaskCategory = "Personal";
        private string newTaskDueDate = "";
        private bool newTaskIsDaily = false;

        // Interaction Hit-Testing
        private Point lastMousePos = Point.Empty;
        private TaskItem hoveredTask = null;
        private bool hoveredTaskCheckbox = false;
        private bool hoveredTaskDelete = false;
        private string hoveredButton = null;
        private TaskItem lastDeletedTask = null;
        private int lastDeletedIndex = -1;
        private string toastMessage = null;
        private System.Windows.Forms.Timer toastTimer;
        private System.Windows.Forms.Timer memTrimTimer;

        // System Tray & Menu
        private NotifyIcon trayIcon;
        private ContextMenuStrip trayMenu;

        // Colors
        private Color baseBgColor = Color.FromArgb(10, 10, 12);
        private Color cardBgColor = Color.FromArgb(20, 20, 24);
        private Color cardBorderColor = Color.FromArgb(38, 38, 44);
        private Color accentColor = Color.FromArgb(255, 255, 255);
        private Color textPrimary = Color.FromArgb(240, 240, 240);
        private Color textMuted = Color.FromArgb(140, 140, 150);
        private Color borderSubtle = Color.FromArgb(30, 30, 35);
        private Color deleteRed = Color.FromArgb(239, 68, 68);

        private static readonly string[] PRESET_COLORS = new string[] {
            "#000000", "#0B132B", "#1C1024", "#0C1F17", "#240E15",
            "#0A252C", "#1C1611", "#1B2026", "#14142B", "#101D28"
        };

        protected override CreateParams CreateParams
        {
            get
            {
                CreateParams cp = base.CreateParams;
                cp.ClassStyle |= CS_DROPSHADOW;
                cp.Style |= WS_THICKFRAME;
                return cp;
            }
        }

        public PureNativeWidgetForm()
        {
            this.Text = "Desktop To-Do Widget (Pure Native)";
            this.FormBorderStyle = FormBorderStyle.None;
            this.StartPosition = FormStartPosition.Manual;
            this.ShowInTaskbar = false;
            this.DoubleBuffered = true;
            this.SetStyle(ControlStyles.OptimizedDoubleBuffer | ControlStyles.AllPaintingInWmPaint | ControlStyles.UserPaint, true);
            this.BackColor = baseBgColor;
            this.Width = DEFAULT_WIDTH;
            this.Height = DEFAULT_HEIGHT;
            this.MinimumSize = new Size(280, 220);
            this.MaximumSize = new Size(900, 1400);

            jsonSerializer = new JavaScriptSerializer();

            string appDataDir = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData), "windows11-todo-widget");
            if (!Directory.Exists(appDataDir))
            {
                Directory.CreateDirectory(appDataDir);
            }
            dataFilePath = Path.Combine(appDataDir, "todo-widget-data.json");

            InitControls();
            LoadData();
            ApplyThemeColor(settings.themeColor);
            InitTray();

            toastTimer = new System.Windows.Forms.Timer();
            toastTimer.Interval = 4000;
            toastTimer.Tick += (s, e) => {
                toastTimer.Stop();
                toastMessage = null;
                lastDeletedTask = null;
                Invalidate();
            };

            memTrimTimer = new System.Windows.Forms.Timer();
            memTrimTimer.Interval = 30000;
            memTrimTimer.Tick += (s, e) => TrimMemory();
            memTrimTimer.Start();
        }

        private void TrimMemory()
        {
            try
            {
                GC.Collect(2, GCCollectionMode.Forced, true);
                GC.WaitForPendingFinalizers();
                EmptyWorkingSet(System.Diagnostics.Process.GetCurrentProcess().Handle);
            }
            catch { }
        }

        private void InitControls()
        {
            // Bottom New Task Title TextBox
            txtNewTaskTitle = new TextBox
            {
                BorderStyle = BorderStyle.None,
                BackColor = Color.FromArgb(24, 24, 28),
                ForeColor = textPrimary,
                Font = new Font("Segoe UI", 9.5f, FontStyle.Regular),
                Location = new Point(14, this.Height - 42),
                Width = this.Width - 120,
                Height = 22
            };
            txtNewTaskTitle.KeyDown += (s, e) => {
                if (e.KeyCode == Keys.Enter && !string.IsNullOrWhiteSpace(txtNewTaskTitle.Text))
                {
                    e.SuppressKeyPress = true;
                    AddNewTask(txtNewTaskTitle.Text.Trim());
                    txtNewTaskTitle.Text = "";
                }
            };
            this.Controls.Add(txtNewTaskTitle);

            // Search TextBox (hidden initially)
            txtSearch = new TextBox
            {
                BorderStyle = BorderStyle.None,
                BackColor = Color.FromArgb(28, 28, 34),
                ForeColor = textPrimary,
                Font = new Font("Segoe UI", 9.0f, FontStyle.Regular),
                Location = new Point(14, 48),
                Width = this.Width - 50,
                Height = 20,
                Visible = false
            };
            txtSearch.TextChanged += (s, e) => {
                searchFilter = txtSearch.Text;
                Invalidate();
            };
            txtSearch.KeyDown += (s, e) => {
                if (e.KeyCode == Keys.Escape)
                {
                    isSearchOpen = false;
                    txtSearch.Visible = false;
                    searchFilter = "";
                    txtSearch.Text = "";
                    Invalidate();
                }
            };
            this.Controls.Add(txtSearch);
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

            if (toastTimer != null) { toastTimer.Stop(); toastTimer.Dispose(); }
            if (memTrimTimer != null) { memTrimTimer.Stop(); memTrimTimer.Dispose(); }
            if (trayIcon != null) { trayIcon.Visible = false; trayIcon.Dispose(); }

            base.OnFormClosing(e);
        }

        protected override void WndProc(ref Message m)
        {
            if (m.Msg == WM_NCCALCSIZE && m.WParam.ToInt32() == 1)
            {
                m.Result = IntPtr.Zero;
                return;
            }

            if (m.Msg == WM_SYSCOMMAND && (m.WParam.ToInt32() & 0xFFF0) == SC_MINIMIZE)
            {
                return;
            }

            if (m.Msg == WM_EXITSIZEMOVE)
            {
                SnapToEdges();
                SaveData();
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
            TrimMemory();
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
                txtNewTaskTitle.Focus();
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

        // ======================================================================
        // Data Serialization
        // ======================================================================
        private void LoadData()
        {
            try
            {
                if (File.Exists(dataFilePath))
                {
                    string json = File.ReadAllText(dataFilePath, Encoding.UTF8);
                    Dictionary<string, object> root = jsonSerializer.Deserialize<Dictionary<string, object>>(json);

                    if (root != null)
                    {
                        if (root.ContainsKey("tasks"))
                        {
                            var taskList = root["tasks"] as System.Collections.ArrayList;
                            if (taskList != null)
                            {
                                tasks.Clear();
                                foreach (object obj in taskList)
                                {
                                    var t = obj as Dictionary<string, object>;
                                    if (t == null) continue;

                                    TaskItem item = new TaskItem();
                                    if (t.ContainsKey("id")) item.id = Convert.ToString(t["id"]);
                                    if (t.ContainsKey("title")) item.title = Convert.ToString(t["title"]);
                                    if (t.ContainsKey("completed")) item.completed = Convert.ToBoolean(t["completed"]);
                                    if (t.ContainsKey("category")) item.category = Convert.ToString(t["category"]);
                                    if (t.ContainsKey("dueDate")) item.dueDate = Convert.ToString(t["dueDate"]);
                                    if (t.ContainsKey("isDaily")) item.isDaily = Convert.ToBoolean(t["isDaily"]);
                                    if (t.ContainsKey("createdAt")) item.createdAt = Convert.ToInt64(t["createdAt"]);
                                    tasks.Add(item);
                                }
                            }
                        }

                        if (root.ContainsKey("settings"))
                        {
                            var s = root["settings"] as Dictionary<string, object>;
                            if (s != null)
                            {
                                if (s.ContainsKey("locked")) settings.locked = Convert.ToBoolean(s["locked"]);
                                if (s.ContainsKey("miniMode")) settings.miniMode = Convert.ToBoolean(s["miniMode"]);
                                if (s.ContainsKey("autoStart")) settings.autoStart = Convert.ToBoolean(s["autoStart"]);
                                if (s.ContainsKey("themeColor")) settings.themeColor = Convert.ToString(s["themeColor"]);

                                if (s.ContainsKey("bounds"))
                                {
                                    var b = s["bounds"] as Dictionary<string, object>;
                                    if (b != null)
                                    {
                                        int bx = b.ContainsKey("x") ? Convert.ToInt32(b["x"]) : this.Left;
                                        int by = b.ContainsKey("y") ? Convert.ToInt32(b["y"]) : this.Top;
                                        int bw = b.ContainsKey("width") ? Convert.ToInt32(b["width"]) : this.Width;
                                        int bh = b.ContainsKey("height") ? Convert.ToInt32(b["height"]) : this.Height;

                                        Rectangle screenArea = Screen.FromPoint(new Point(bx, by)).WorkingArea;
                                        bx = Math.Max(screenArea.Left, Math.Min(screenArea.Right - bw, bx));
                                        by = Math.Max(screenArea.Top, Math.Min(screenArea.Bottom - bh, by));

                                        this.Location = new Point(bx, by);
                                        this.Size = new Size(Math.Max(this.MinimumSize.Width, bw), Math.Max(this.MinimumSize.Height, bh));
                                    }
                                }
                            }
                        }
                    }
                }
            }
            catch { }

            if (tasks.Count == 0)
            {
                tasks.Add(new TaskItem { title = "Welcome to Pure Native Widget", category = "Personal", dueDate = DateTime.Today.ToString("yyyy-MM-dd"), isDaily = true });
                tasks.Add(new TaskItem { title = "Super-fast: Only ~8 MB RAM!", category = "Work", dueDate = DateTime.Today.ToString("yyyy-MM-dd"), isDaily = false });
                tasks.Add(new TaskItem { title = "Click to mark as complete", completed = true, category = "Personal" });
            }
        }

        private void SaveData()
        {
            try
            {
                Dictionary<string, object> root = new Dictionary<string, object>();
                root["tasks"] = tasks;
                root["categories"] = categories;

                Dictionary<string, object> s = new Dictionary<string, object>();
                s["locked"] = settings.locked;
                s["opacity"] = settings.opacity;
                s["miniMode"] = settings.miniMode;
                s["autoStart"] = settings.autoStart;
                s["themeColor"] = settings.themeColor;

                Dictionary<string, object> b = new Dictionary<string, object>();
                b["x"] = this.Left;
                b["y"] = this.Top;
                b["width"] = this.Width;
                b["height"] = this.Height;
                s["bounds"] = b;

                root["settings"] = s;

                File.WriteAllText(dataFilePath, jsonSerializer.Serialize(root), Encoding.UTF8);
            }
            catch { }
        }

        // ======================================================================
        // Theme Color Calculation
        // ======================================================================
        private void ApplyThemeColor(string hex)
        {
            if (string.IsNullOrEmpty(hex)) hex = "#000000";
            settings.themeColor = hex;

            try
            {
                Color c = ColorTranslator.FromHtml(hex);
                float h, s, v;
                ColorToHsv(c, out h, out s, out v);

                if (v < 0.10f && s < 0.10f)
                {
                    // Pure pitch black
                    baseBgColor = Color.FromArgb(10, 10, 12);
                    cardBgColor = Color.FromArgb(20, 20, 24);
                    cardBorderColor = Color.FromArgb(38, 38, 44);
                    accentColor = Color.FromArgb(255, 255, 255);
                }
                else
                {
                    // Tailored harmonized dark shade
                    baseBgColor = HsvToColor(h, Math.Min(0.85f, s), Math.Min(0.18f, Math.Max(0.08f, v * 0.3f)));
                    cardBgColor = HsvToColor(h, Math.Min(0.75f, s), Math.Min(0.28f, Math.Max(0.14f, v * 0.45f)));
                    cardBorderColor = HsvToColor(h, Math.Min(0.60f, s), Math.Min(0.42f, Math.Max(0.25f, v * 0.65f)));
                    accentColor = HsvToColor(h, Math.Max(0.40f, s), Math.Max(0.85f, v));
                }

                this.BackColor = baseBgColor;
                if (txtNewTaskTitle != null) txtNewTaskTitle.BackColor = cardBgColor;
                if (txtSearch != null) txtSearch.BackColor = cardBgColor;

                Invalidate();
            }
            catch { }
        }

        private static void ColorToHsv(Color color, out float hue, out float saturation, out float value)
        {
            int max = Math.Max(color.R, Math.Max(color.G, color.B));
            int min = Math.Min(color.R, Math.Min(color.G, color.B));

            hue = color.GetHue();
            saturation = (max == 0) ? 0 : 1f - (1f * min / max);
            value = max / 255f;
        }

        private static Color HsvToColor(float hue, float saturation, float value)
        {
            int hi = Convert.ToInt32(Math.Floor(hue / 60)) % 6;
            float f = hue / 60 - (float)Math.Floor(hue / 60);

            value = value * 255;
            int v = Convert.ToInt32(Math.Max(0, Math.Min(255, value)));
            int p = Convert.ToInt32(Math.Max(0, Math.Min(255, value * (1 - saturation))));
            int q = Convert.ToInt32(Math.Max(0, Math.Min(255, value * (1 - f * saturation))));
            int t = Convert.ToInt32(Math.Max(0, Math.Min(255, value * (1 - (1 - f) * saturation))));

            if (hi == 0) return Color.FromArgb(v, t, p);
            else if (hi == 1) return Color.FromArgb(q, v, p);
            else if (hi == 2) return Color.FromArgb(p, v, t);
            else if (hi == 3) return Color.FromArgb(p, q, v);
            else if (hi == 4) return Color.FromArgb(t, p, v);
            else return Color.FromArgb(v, p, q);
        }

        // ======================================================================
        // Custom Painting & Layout
        // ======================================================================
        protected override void OnResize(EventArgs e)
        {
            base.OnResize(e);
            if (txtNewTaskTitle != null)
            {
                txtNewTaskTitle.Location = new Point(14, this.Height - 42);
                txtNewTaskTitle.Width = this.Width - 140;
            }
            if (txtSearch != null)
            {
                txtSearch.Location = new Point(14, 48);
                txtSearch.Width = this.Width - 50;
            }
            Invalidate();
        }

        protected override void OnPaint(PaintEventArgs e)
        {
            Graphics g = e.Graphics;
            g.SmoothingMode = SmoothingMode.AntiAlias;
            g.TextRenderingHint = TextRenderingHint.ClearTypeGridFit;

            // Background
            g.Clear(baseBgColor);

            // Subtle outer border
            using (Pen borderPen = new Pen(borderSubtle, 1))
            {
                g.DrawRectangle(borderPen, 0, 0, this.Width - 1, this.Height - 1);
            }

            int topY = 12;

            // Header: Title + Icons
            DrawHeader(g, ref topY);

            // Search Bar (if open)
            if (isSearchOpen)
            {
                topY += 34;
            }

            // Category Bar ("All", "Personal", "Work")
            DrawCategoryBar(g, ref topY);

            // Main Content Area: Task List or Settings Drawer
            if (isSettingsOpen)
            {
                DrawSettingsDrawer(g, topY);
            }
            else
            {
                DrawTaskList(g, topY, this.Height - 56);
            }

            // Bottom Quick-Add Bar
            DrawFooter(g);

            // Toast Message (if active)
            if (!string.IsNullOrEmpty(toastMessage))
            {
                DrawToast(g);
            }

            // Subtle corner resize grip (bottom-right)
            DrawResizeGrip(g);
        }

        private void DrawHeader(Graphics g, ref int y)
        {
            // Title
            using (Font titleFont = new Font("Segoe UI", 12f, FontStyle.Bold))
            using (SolidBrush textBrush = new SolidBrush(textPrimary))
            {
                g.DrawString("Tasks", titleFont, textBrush, 14, y);
            }

            // Right Action Icons: Search, Lock, Mini, Settings
            int rightX = this.Width - 28;

            // Settings Gear
            DrawIconButton(g, rightX, y + 2, "⚙", "settings", hoveredButton == "settings");
            rightX -= 26;

            // Mini Mode Toggle
            DrawIconButton(g, rightX, y + 2, settings.miniMode ? "🗖" : "🗕", "mini", hoveredButton == "mini");
            rightX -= 26;

            // Lock Position Toggle
            DrawIconButton(g, rightX, y + 2, settings.locked ? "🔒" : "🔓", "lock", hoveredButton == "lock");
            rightX -= 26;

            // Search Toggle
            DrawIconButton(g, rightX, y + 2, "🔍", "search", hoveredButton == "search");

            y += 28;
        }

        private void DrawIconButton(Graphics g, int x, int y, string iconText, string id, bool isHovered)
        {
            if (isHovered)
            {
                using (SolidBrush hoverBg = new SolidBrush(Color.FromArgb(40, 255, 255, 255)))
                {
                    g.FillEllipse(hoverBg, x - 3, y - 2, 22, 22);
                }
            }

            using (Font iconFont = new Font("Segoe UI Symbol", 9.5f, FontStyle.Regular))
            using (SolidBrush iconBrush = new SolidBrush(isHovered ? textPrimary : textMuted))
            {
                g.DrawString(iconText, iconFont, iconBrush, x, y);
            }
        }

        private void DrawCategoryBar(Graphics g, ref int y)
        {
            int startX = 14;
            string[] tabs = new string[] { "All", "Personal", "Work" };

            foreach (var tab in tabs)
            {
                bool isActive = activeCategoryFilter.Equals(tab, StringComparison.OrdinalIgnoreCase);
                bool isHover = hoveredButton == ("cat_" + tab);

                using (Font tabFont = new Font("Segoe UI", 9.0f, isActive ? FontStyle.Bold : FontStyle.Regular))
                {
                    SizeF size = g.MeasureString(tab, tabFont);
                    int padW = 16;
                    int btnW = (int)size.Width + padW;
                    int btnH = 24;

                    Rectangle btnRect = new Rectangle(startX, y, btnW, btnH);

                    if (isActive)
                    {
                        using (SolidBrush activeBg = new SolidBrush(accentColor))
                        using (GraphicsPath path = GetRoundedRect(btnRect, 12))
                        {
                            g.FillPath(activeBg, path);
                        }
                        using (SolidBrush textB = new SolidBrush(GetContrastingTextColor(accentColor)))
                        {
                            g.DrawString(tab, tabFont, textB, startX + 8, y + 4);
                        }
                    }
                    else
                    {
                        if (isHover)
                        {
                            using (SolidBrush hoverBg = new SolidBrush(Color.FromArgb(30, 255, 255, 255)))
                            using (GraphicsPath path = GetRoundedRect(btnRect, 12))
                            {
                                g.FillPath(hoverBg, path);
                            }
                        }
                        using (SolidBrush textB = new SolidBrush(isHover ? textPrimary : textMuted))
                        {
                            g.DrawString(tab, tabFont, textB, startX + 8, y + 4);
                        }
                    }

                    startX += btnW + 8;
                }
            }

            y += 32;
        }

        private void DrawTaskList(Graphics g, int topY, int bottomY)
        {
            Rectangle clipArea = new Rectangle(0, topY, this.Width, bottomY - topY);
            g.SetClip(clipArea);

            int currentY = topY - scrollOffsetY;

            // Filter tasks
            List<TaskItem> activeList = new List<TaskItem>();
            List<TaskItem> completedList = new List<TaskItem>();

            foreach (var t in tasks)
            {
                // Category Filter
                if (activeCategoryFilter != "All" && !t.category.Equals(activeCategoryFilter, StringComparison.OrdinalIgnoreCase))
                    continue;

                // Search Filter
                if (!string.IsNullOrEmpty(searchFilter))
                {
                    if (t.title.IndexOf(searchFilter, StringComparison.OrdinalIgnoreCase) < 0 &&
                        t.category.IndexOf(searchFilter, StringComparison.OrdinalIgnoreCase) < 0)
                        continue;
                }

                if (t.completed)
                    completedList.Add(t);
                else
                    activeList.Add(t);
            }

            // Draw Active Tasks
            foreach (var task in activeList)
            {
                DrawTaskCard(g, task, 14, currentY, this.Width - 28);
                currentY += 58;
            }

            // Completed Collapsible Header
            if (completedList.Count > 0)
            {
                DrawCompletedHeader(g, 14, currentY, completedList.Count);
                currentY += 30;

                if (!isCompletedCollapsed)
                {
                    foreach (var task in completedList)
                    {
                        DrawTaskCard(g, task, 14, currentY, this.Width - 28);
                        currentY += 58;
                    }
                }
            }

            maxScrollY = Math.Max(0, (currentY + scrollOffsetY) - bottomY + 20);
            g.ResetClip();

            // Draw empty state if no tasks
            if (activeList.Count == 0 && completedList.Count == 0)
            {
                using (Font emptyFont = new Font("Segoe UI", 9.5f, FontStyle.Regular))
                using (SolidBrush emptyBrush = new SolidBrush(textMuted))
                {
                    string msg = "No tasks found. Relax or add a task below!";
                    SizeF s = g.MeasureString(msg, emptyFont);
                    g.DrawString(msg, emptyFont, emptyBrush, (this.Width - s.Width) / 2, (topY + bottomY) / 2 - 10);
                }
            }
        }

        private void DrawTaskCard(Graphics g, TaskItem task, int x, int y, int width)
        {
            int height = 52;
            Rectangle cardRect = new Rectangle(x, y, width, height);

            bool isHovered = (hoveredTask == task);

            // Card Background
            using (SolidBrush bgBrush = new SolidBrush(isHovered ? Color.FromArgb(cardBgColor.R + 8, cardBgColor.G + 8, cardBgColor.B + 8) : cardBgColor))
            using (Pen borderPen = new Pen(isHovered ? cardBorderColor : Color.FromArgb(cardBorderColor.R - 5, cardBorderColor.G - 5, cardBorderColor.B - 5), 1))
            using (GraphicsPath path = GetRoundedRect(cardRect, 8))
            {
                g.FillPath(bgBrush, path);
                g.DrawPath(borderPen, path);
            }

            // Checkbox (x: 24, y: y + 16, w: 18, h: 18)
            Rectangle checkRect = new Rectangle(x + 10, y + 17, 18, 18);
            using (GraphicsPath checkPath = GetRoundedRect(checkRect, 5))
            {
                if (task.completed)
                {
                    using (SolidBrush checkBg = new SolidBrush(accentColor))
                    {
                        g.FillPath(checkBg, checkPath);
                    }
                    // Draw Checkmark
                    using (Pen checkPen = new Pen(GetContrastingTextColor(accentColor), 2f))
                    {
                        g.DrawLine(checkPen, checkRect.Left + 4, checkRect.Top + 9, checkRect.Left + 7, checkRect.Top + 13);
                        g.DrawLine(checkPen, checkRect.Left + 7, checkRect.Top + 13, checkRect.Left + 14, checkRect.Top + 5);
                    }
                }
                else
                {
                    using (Pen checkBorder = new Pen(hoveredTask == task && hoveredTaskCheckbox ? accentColor : textMuted, 1.5f))
                    {
                        g.DrawPath(checkBorder, checkPath);
                    }
                }
            }

            // Task Title
            int textX = x + 36;
            using (Font titleFont = new Font("Segoe UI", 9.5f, task.completed ? FontStyle.Strikeout : FontStyle.Regular))
            using (SolidBrush textBrush = new SolidBrush(task.completed ? textMuted : textPrimary))
            {
                Rectangle textRect = new Rectangle(textX, y + 8, width - 75, 20);
                StringFormat sf = new StringFormat { Trimming = StringTrimming.EllipsisCharacter, FormatFlags = StringFormatFlags.NoWrap };
                g.DrawString(task.title, titleFont, textBrush, textRect, sf);
            }

            // Meta Info: Category Badge + Due Date Badge
            int metaX = textX;
            int metaY = y + 29;

            // Category Pill
            using (Font metaFont = new Font("Segoe UI", 7.5f, FontStyle.Bold))
            {
                SizeF catSize = g.MeasureString(task.category, metaFont);
                Rectangle catRect = new Rectangle(metaX, metaY, (int)catSize.Width + 8, 16);
                using (SolidBrush catBg = new SolidBrush(task.category == "Work" ? Color.FromArgb(30, 41, 59) : Color.FromArgb(38, 25, 45)))
                using (GraphicsPath catPath = GetRoundedRect(catRect, 4))
                using (SolidBrush catText = new SolidBrush(task.category == "Work" ? Color.FromArgb(148, 163, 184) : Color.FromArgb(216, 180, 254)))
                {
                    g.FillPath(catBg, catPath);
                    g.DrawString(task.category, metaFont, catText, metaX + 4, metaY + 1);
                }
                metaX += catRect.Width + 6;

                // Due Date Badge
                if (!string.IsNullOrEmpty(task.dueDate))
                {
                    string dateText = (task.dueDate == DateTime.Today.ToString("yyyy-MM-dd")) ? "Today" : task.dueDate;
                    if (task.isDaily) dateText += " ⟳";

                    SizeF dateSize = g.MeasureString(dateText, metaFont);
                    Rectangle dateRect = new Rectangle(metaX, metaY, (int)dateSize.Width + 8, 16);
                    using (SolidBrush dateBg = new SolidBrush(Color.FromArgb(24, 24, 28)))
                    using (GraphicsPath datePath = GetRoundedRect(dateRect, 4))
                    using (SolidBrush dateTextB = new SolidBrush(textMuted))
                    {
                        g.FillPath(dateBg, datePath);
                        g.DrawString(dateText, metaFont, dateTextB, metaX + 4, metaY + 1);
                    }
                }
            }

            // Delete Icon (visible on hover)
            if (isHovered)
            {
                int delX = x + width - 26;
                int delY = y + 16;
                using (Font delFont = new Font("Segoe UI Symbol", 10f, FontStyle.Regular))
                using (SolidBrush delBrush = new SolidBrush(hoveredTaskDelete ? deleteRed : textMuted))
                {
                    g.DrawString("🗑", delFont, delBrush, delX, delY);
                }
            }
        }

        private void DrawCompletedHeader(Graphics g, int x, int y, int count)
        {
            using (Font hFont = new Font("Segoe UI", 8.5f, FontStyle.Bold))
            using (SolidBrush hBrush = new SolidBrush(textMuted))
            {
                string chevron = isCompletedCollapsed ? "▶" : "▼";
                g.DrawString(chevron + " Completed (" + count + ")", hFont, hBrush, x, y);
            }
        }

        private void DrawFooter(Graphics g)
        {
            int footerY = this.Height - 52;

            // Top footer border
            using (Pen borderPen = new Pen(borderSubtle, 1))
            {
                g.DrawLine(borderPen, 0, footerY, this.Width, footerY);
            }

            // Quick category toggle pill ("Personal" / "Work")
            int catX = this.Width - 110;
            int catY = footerY + 12;
            Rectangle catRect = new Rectangle(catX, catY, 65, 26);
            using (GraphicsPath path = GetRoundedRect(catRect, 6))
            using (SolidBrush bg = new SolidBrush(Color.FromArgb(26, 26, 32)))
            using (Pen p = new Pen(borderSubtle, 1))
            using (Font f = new Font("Segoe UI", 8.0f, FontStyle.Bold))
            using (SolidBrush tb = new SolidBrush(textPrimary))
            {
                g.FillPath(bg, path);
                g.DrawPath(p, path);
                g.DrawString(newTaskCategory, f, tb, catX + 8, catY + 5);
            }

            // Add '+' Button
            int addX = this.Width - 38;
            int addY = footerY + 12;
            Rectangle addRect = new Rectangle(addX, addY, 26, 26);
            bool isAddHover = hoveredButton == "add_task";
            using (GraphicsPath path = GetRoundedRect(addRect, 6))
            using (SolidBrush bg = new SolidBrush(isAddHover ? Color.FromArgb(255, 255, 255) : accentColor))
            using (SolidBrush tb = new SolidBrush(GetContrastingTextColor(accentColor)))
            using (Font f = new Font("Segoe UI", 12f, FontStyle.Bold))
            {
                g.FillPath(bg, path);
                g.DrawString("+", f, tb, addX + 6, addY + 1);
            }
        }

        private void DrawSettingsDrawer(Graphics g, int topY)
        {
            Rectangle drawerRect = new Rectangle(14, topY, this.Width - 28, this.Height - topY - 20);

            using (SolidBrush bg = new SolidBrush(Color.FromArgb(16, 16, 20)))
            using (Pen p = new Pen(cardBorderColor, 1))
            using (GraphicsPath path = GetRoundedRect(drawerRect, 8))
            {
                g.FillPath(bg, path);
                g.DrawPath(p, path);
            }

            int sy = topY + 16;
            using (Font titleFont = new Font("Segoe UI", 10f, FontStyle.Bold))
            using (Font labelFont = new Font("Segoe UI", 9.0f, FontStyle.Regular))
            using (SolidBrush textB = new SolidBrush(textPrimary))
            using (SolidBrush muteB = new SolidBrush(textMuted))
            {
                g.DrawString("Settings & Personalization", titleFont, textB, 26, sy);
                sy += 30;

                // Color Presets Header
                g.DrawString("Accent & Theme Color", labelFont, muteB, 26, sy);
                sy += 22;

                // Draw 10 Preset Color Chips
                int chipX = 26;
                int chipSize = 22;
                for (int i = 0; i < PRESET_COLORS.Length; i++)
                {
                    string hex = PRESET_COLORS[i];
                    Color pc = ColorTranslator.FromHtml(hex);
                    Rectangle cr = new Rectangle(chipX, sy, chipSize, chipSize);

                    using (SolidBrush cb = new SolidBrush(pc))
                    using (Pen cp = new Pen(settings.themeColor.Equals(hex, StringComparison.OrdinalIgnoreCase) ? Color.White : Color.FromArgb(60, 60, 70), 2))
                    {
                        g.FillEllipse(cb, cr);
                        g.DrawEllipse(cp, cr);
                    }

                    chipX += chipSize + 8;
                    if (i == 4)
                    {
                        chipX = 26;
                        sy += chipSize + 8;
                    }
                }

                sy += chipSize + 16;

                // Custom Color Picker Button
                DrawButton(g, 26, sy, 120, 28, "Pick Custom Color", "btn_custom_color", hoveredButton == "btn_custom_color");
                sy += 40;

                // Window Pinning
                g.DrawString("Magnetic Screen Pinning", labelFont, muteB, 26, sy);
                sy += 22;
                DrawButton(g, 26, sy, 110, 28, "Top-Left (0px)", "btn_pin_tl", hoveredButton == "btn_pin_tl");
                DrawButton(g, 146, sy, 110, 28, "Top-Right (0px)", "btn_pin_tr", hoveredButton == "btn_pin_tr");
                sy += 40;

                // Auto Start on Boot
                g.DrawString("Startup Behavior", labelFont, muteB, 26, sy);
                sy += 22;
                DrawButton(g, 26, sy, 160, 28, settings.autoStart ? "Auto-Start: ON ✔" : "Auto-Start: OFF ✖", "btn_toggle_autostart", hoveredButton == "btn_toggle_autostart");
                sy += 45;

                // Close Settings Button
                DrawButton(g, 26, sy, 100, 28, "Close", "btn_close_settings", hoveredButton == "btn_close_settings");
            }
        }

        private void DrawButton(Graphics g, int x, int y, int w, int h, string text, string id, bool isHovered)
        {
            Rectangle r = new Rectangle(x, y, w, h);
            using (GraphicsPath path = GetRoundedRect(r, 6))
            using (SolidBrush bg = new SolidBrush(isHovered ? Color.FromArgb(40, 40, 48) : Color.FromArgb(28, 28, 34)))
            using (Pen p = new Pen(isHovered ? accentColor : cardBorderColor, 1))
            using (Font f = new Font("Segoe UI", 8.5f, FontStyle.Regular))
            using (SolidBrush tb = new SolidBrush(isHovered ? textPrimary : textMuted))
            {
                g.FillPath(bg, path);
                g.DrawPath(p, path);
                SizeF s = g.MeasureString(text, f);
                g.DrawString(text, f, tb, x + (w - s.Width) / 2, y + (h - s.Height) / 2);
            }
        }

        private void DrawToast(Graphics g)
        {
            Rectangle r = new Rectangle(20, this.Height - 88, this.Width - 40, 32);
            using (GraphicsPath path = GetRoundedRect(r, 6))
            using (SolidBrush bg = new SolidBrush(Color.FromArgb(30, 30, 36)))
            using (Pen p = new Pen(borderSubtle, 1))
            using (Font f = new Font("Segoe UI", 8.5f, FontStyle.Regular))
            using (Font undoFont = new Font("Segoe UI", 8.5f, FontStyle.Bold))
            using (SolidBrush tb = new SolidBrush(textPrimary))
            using (SolidBrush ub = new SolidBrush(accentColor))
            {
                g.FillPath(bg, path);
                g.DrawPath(p, path);
                g.DrawString(toastMessage, f, tb, r.Left + 10, r.Top + 7);

                if (lastDeletedTask != null)
                {
                    g.DrawString("Undo", undoFont, ub, r.Right - 50, r.Top + 7);
                }
            }
        }

        private void DrawResizeGrip(Graphics g)
        {
            int rx = this.Width - 12;
            int ry = this.Height - 12;
            using (Pen gripPen = new Pen(Color.FromArgb(80, 80, 90), 1.5f))
            {
                g.DrawLine(gripPen, rx + 6, ry + 2, rx + 2, ry + 6);
                g.DrawLine(gripPen, rx + 6, ry + 5, rx + 5, ry + 6);
            }
        }

        // ======================================================================
        // Mouse & Keyboard Interactions
        // ======================================================================
        protected override void OnMouseDown(MouseEventArgs e)
        {
            base.OnMouseDown(e);

            // Resize handle check (edges and corners)
            if (!settings.locked)
            {
                int hitCode = GetResizeHitCode(e.Location);
                if (hitCode != 0)
                {
                    ReleaseCapture();
                    SendMessage(this.Handle, WM_NCLBUTTONDOWN, (IntPtr)hitCode, IntPtr.Zero);
                    return;
                }
            }

            // Header dragging
            if (e.Y <= 38 && e.Button == MouseButtons.Left && !settings.locked)
            {
                // Check header icons
                if (hoveredButton == "settings")
                {
                    isSettingsOpen = !isSettingsOpen;
                    Invalidate();
                    return;
                }
                if (hoveredButton == "mini")
                {
                    ToggleMiniMode();
                    return;
                }
                if (hoveredButton == "lock")
                {
                    settings.locked = !settings.locked;
                    ShowToast(settings.locked ? "Position: Locked 🔒" : "Position: Draggable 🔓");
                    SaveData();
                    Invalidate();
                    return;
                }
                if (hoveredButton == "search")
                {
                    isSearchOpen = !isSearchOpen;
                    txtSearch.Visible = isSearchOpen;
                    if (isSearchOpen) txtSearch.Focus();
                    Invalidate();
                    return;
                }

                // Window dragging
                ReleaseCapture();
                SendMessage(this.Handle, WM_NCLBUTTONDOWN, (IntPtr)HTCAPTION, IntPtr.Zero);
                return;
            }

            // Category Bar clicks
            if (e.Y > 38 && e.Y < 72 && !isSettingsOpen)
            {
                if (hoveredButton != null && hoveredButton.StartsWith("cat_"))
                {
                    activeCategoryFilter = hoveredButton.Substring(4);
                    Invalidate();
                    return;
                }
            }

            // Settings Drawer button clicks
            if (isSettingsOpen)
            {
                HandleSettingsClick(e.Location);
                return;
            }

            // Task List clicks: Checkbox or Delete
            if (hoveredTask != null)
            {
                if (hoveredTaskCheckbox)
                {
                    hoveredTask.completed = !hoveredTask.completed;
                    SaveData();
                    Invalidate();
                    return;
                }
                if (hoveredTaskDelete)
                {
                    DeleteTask(hoveredTask);
                    return;
                }
            }

            // Completed section collapsible toggle
            if (e.Y >= 72 && e.Y <= this.Height - 56)
            {
                if (hoveredButton == "toggle_completed")
                {
                    isCompletedCollapsed = !isCompletedCollapsed;
                    Invalidate();
                    return;
                }
            }

            // Footer Actions
            if (e.Y >= this.Height - 52)
            {
                // Toggle Category pill
                if (e.X >= this.Width - 110 && e.X <= this.Width - 45)
                {
                    newTaskCategory = (newTaskCategory == "Personal") ? "Work" : "Personal";
                    Invalidate();
                    return;
                }
                // Click '+' Add
                if (e.X >= this.Width - 38 && e.X <= this.Width - 12)
                {
                    if (!string.IsNullOrWhiteSpace(txtNewTaskTitle.Text))
                    {
                        AddNewTask(txtNewTaskTitle.Text.Trim());
                        txtNewTaskTitle.Text = "";
                    }
                    return;
                }
            }

            // Toast Undo click
            if (!string.IsNullOrEmpty(toastMessage) && lastDeletedTask != null)
            {
                Rectangle r = new Rectangle(20, this.Height - 88, this.Width - 40, 32);
                if (r.Contains(e.Location) && e.X >= r.Right - 60)
                {
                    UndoDelete();
                    return;
                }
            }
        }

        protected override void OnMouseMove(MouseEventArgs e)
        {
            base.OnMouseMove(e);
            lastMousePos = e.Location;

            // Cursor updates for resizing
            if (!settings.locked)
            {
                int hit = GetResizeHitCode(e.Location);
                switch (hit)
                {
                    case HTLEFT:
                    case HTRIGHT:
                        this.Cursor = Cursors.SizeWE;
                        break;
                    case HTTOP:
                    case HTBOTTOM:
                        this.Cursor = Cursors.SizeNS;
                        break;
                    case HTTOPLEFT:
                    case HTBOTTOMRIGHT:
                        this.Cursor = Cursors.SizeNWSE;
                        break;
                    case HTTOPRIGHT:
                    case HTBOTTOMLEFT:
                        this.Cursor = Cursors.SizeNESW;
                        break;
                    default:
                        this.Cursor = Cursors.Default;
                        break;
                }
            }

            // Update Hover States
            UpdateHoverStates(e.Location);
        }

        private void UpdateHoverStates(Point p)
        {
            string oldHoveredBtn = hoveredButton;
            TaskItem oldHoveredTask = hoveredTask;
            bool oldCheck = hoveredTaskCheckbox;
            bool oldDel = hoveredTaskDelete;

            hoveredButton = null;
            hoveredTask = null;
            hoveredTaskCheckbox = false;
            hoveredTaskDelete = false;

            // Header Icon Buttons
            if (p.Y >= 10 && p.Y <= 34)
            {
                int rightX = this.Width - 28;
                if (p.X >= rightX - 5 && p.X <= rightX + 20) hoveredButton = "settings";
                rightX -= 26;
                if (p.X >= rightX - 5 && p.X <= rightX + 20) hoveredButton = "mini";
                rightX -= 26;
                if (p.X >= rightX - 5 && p.X <= rightX + 20) hoveredButton = "lock";
                rightX -= 26;
                if (p.X >= rightX - 5 && p.X <= rightX + 20) hoveredButton = "search";
            }

            // Category Bar Buttons
            int catY = isSearchOpen ? 74 : 40;
            if (p.Y >= catY && p.Y <= catY + 24)
            {
                if (p.X >= 14 && p.X <= 55) hoveredButton = "cat_All";
                else if (p.X >= 63 && p.X <= 135) hoveredButton = "cat_Personal";
                else if (p.X >= 143 && p.X <= 200) hoveredButton = "cat_Work";
            }

            // Footer buttons
            if (p.Y >= this.Height - 52)
            {
                if (p.X >= this.Width - 38 && p.X <= this.Width - 12)
                    hoveredButton = "add_task";
            }

            // Task List item hover
            int topY = isSearchOpen ? 104 : 72;
            int bottomY = this.Height - 56;
            if (p.Y >= topY && p.Y <= bottomY && !isSettingsOpen)
            {
                int currentY = topY - scrollOffsetY;
                foreach (var t in tasks)
                {
                    if (activeCategoryFilter != "All" && !t.category.Equals(activeCategoryFilter, StringComparison.OrdinalIgnoreCase)) continue;
                    if (!string.IsNullOrEmpty(searchFilter) && t.title.IndexOf(searchFilter, StringComparison.OrdinalIgnoreCase) < 0) continue;
                    if (t.completed) continue;

                    Rectangle cardRect = new Rectangle(14, currentY, this.Width - 28, 52);
                    if (cardRect.Contains(p))
                    {
                        hoveredTask = t;
                        Rectangle checkR = new Rectangle(14 + 10, currentY + 17, 18, 18);
                        if (checkR.Contains(p)) hoveredTaskCheckbox = true;

                        Rectangle delR = new Rectangle(14 + this.Width - 28 - 26, currentY + 16, 20, 20);
                        if (delR.Contains(p)) hoveredTaskDelete = true;
                        break;
                    }
                    currentY += 58;
                }
            }

            if (oldHoveredBtn != hoveredButton || oldHoveredTask != hoveredTask || oldCheck != hoveredTaskCheckbox || oldDel != hoveredTaskDelete)
            {
                Invalidate();
            }
        }

        private void HandleSettingsClick(Point p)
        {
            int topY = isSearchOpen ? 74 : 40;
            int sy = topY + 16 + 30 + 22;

            // Check Preset Chips
            int chipX = 26;
            int chipSize = 22;
            for (int i = 0; i < PRESET_COLORS.Length; i++)
            {
                Rectangle cr = new Rectangle(chipX, sy, chipSize, chipSize);
                if (cr.Contains(p))
                {
                    ApplyThemeColor(PRESET_COLORS[i]);
                    SaveData();
                    ShowToast("Theme updated");
                    return;
                }

                chipX += chipSize + 8;
                if (i == 4)
                {
                    chipX = 26;
                    sy += chipSize + 8;
                }
            }

            sy += chipSize + 16;

            // Pick Custom Color Button
            if (new Rectangle(26, sy, 120, 28).Contains(p))
            {
                using (ColorDialog cd = new ColorDialog())
                {
                    cd.FullOpen = true;
                    if (cd.ShowDialog() == DialogResult.OK)
                    {
                        string hex = "#" + cd.Color.R.ToString("X2") + cd.Color.G.ToString("X2") + cd.Color.B.ToString("X2");
                        ApplyThemeColor(hex);
                        SaveData();
                        ShowToast("Custom color applied");
                    }
                }
                return;
            }

            sy += 40 + 22;

            // Pin Top-Left (0px)
            if (new Rectangle(26, sy, 110, 28).Contains(p))
            {
                PinToCorner("top-left");
                ShowToast("Pinned to Top-Left");
                return;
            }
            // Pin Top-Right (0px)
            if (new Rectangle(146, sy, 110, 28).Contains(p))
            {
                PinToCorner("top-right");
                ShowToast("Pinned to Top-Right");
                return;
            }

            sy += 40 + 22;

            // Toggle Auto-Start
            if (new Rectangle(26, sy, 160, 28).Contains(p))
            {
                settings.autoStart = !settings.autoStart;
                SetAutoStart(settings.autoStart);
                SaveData();
                ShowToast(settings.autoStart ? "Auto-start enabled" : "Auto-start disabled");
                Invalidate();
                return;
            }

            sy += 45;

            // Close Settings Button
            if (new Rectangle(26, sy, 100, 28).Contains(p))
            {
                isSettingsOpen = false;
                Invalidate();
                return;
            }
        }

        protected override void OnMouseWheel(MouseEventArgs e)
        {
            base.OnMouseWheel(e);
            if (!isSettingsOpen)
            {
                scrollOffsetY -= e.Delta / 2;
                scrollOffsetY = Math.Max(0, Math.Min(maxScrollY, scrollOffsetY));
                Invalidate();
            }
        }

        private int GetResizeHitCode(Point p)
        {
            int x = p.X;
            int y = p.Y;
            int w = this.Width;
            int h = this.Height;

            bool left = (x <= RESIZE_HANDLE_SIZE);
            bool right = (x >= w - RESIZE_HANDLE_SIZE);
            bool top = (y <= RESIZE_HANDLE_SIZE);
            bool bottom = (y >= h - RESIZE_HANDLE_SIZE);

            if (top && left) return HTTOPLEFT;
            if (top && right) return HTTOPRIGHT;
            if (bottom && left) return HTBOTTOMLEFT;
            if (bottom && right) return HTBOTTOMRIGHT;

            if (left) return HTLEFT;
            if (right) return HTRIGHT;
            if (top) return HTTOP;
            if (bottom) return HTBOTTOM;

            return 0;
        }

        // ======================================================================
        // Task Operations
        // ======================================================================
        private void AddNewTask(string title)
        {
            TaskItem t = new TaskItem
            {
                title = title,
                category = newTaskCategory,
                dueDate = newTaskDueDate,
                isDaily = newTaskIsDaily,
                createdAt = DateTime.UtcNow.Ticks
            };
            tasks.Insert(0, t);
            SaveData();
            ShowToast("Task added");
            Invalidate();
        }

        private void DeleteTask(TaskItem task)
        {
            lastDeletedIndex = tasks.IndexOf(task);
            lastDeletedTask = task;
            tasks.Remove(task);
            SaveData();
            ShowToast("Task removed", true);
            Invalidate();
        }

        private void UndoDelete()
        {
            if (lastDeletedTask != null)
            {
                if (lastDeletedIndex >= 0 && lastDeletedIndex <= tasks.Count)
                    tasks.Insert(lastDeletedIndex, lastDeletedTask);
                else
                    tasks.Add(lastDeletedTask);

                lastDeletedTask = null;
                lastDeletedIndex = -1;
                SaveData();
                ShowToast("Action undone");
                Invalidate();
            }
        }

        private void ShowToast(string msg, bool canUndo = false)
        {
            toastMessage = msg;
            toastTimer.Stop();
            toastTimer.Start();
            Invalidate();
        }

        private void ToggleMiniMode()
        {
            settings.miniMode = !settings.miniMode;
            if (settings.miniMode)
            {
                this.Size = new Size(this.Width, MINI_HEIGHT);
            }
            else
            {
                this.Size = new Size(this.Width, DEFAULT_HEIGHT);
            }
            SaveData();
            Invalidate();
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
            SaveData();
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
                            key.SetValue("DesktopToDoWidgetPureNative", "\"" + exePath + "\"");
                        }
                        else
                        {
                            key.DeleteValue("DesktopToDoWidgetPureNative", false);
                        }
                    }
                }
            }
            catch { }
        }

        private void InitTray()
        {
            trayIcon = new NotifyIcon();
            string iconPath = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "assets", "icon.ico");
            if (File.Exists(iconPath))
            {
                trayIcon.Icon = new Icon(iconPath);
            }
            else
            {
                trayIcon.Icon = SystemIcons.Application;
            }
            trayIcon.Text = "Desktop To-Do Widget (Pure Native)";
            trayIcon.Visible = true;
            trayIcon.DoubleClick += (s, e) => ToggleWidgetFocus();

            trayMenu = new ContextMenuStrip();
            trayMenu.Items.Add(new ToolStripMenuItem("Desktop To-Do Widget (Pure Native)") { Enabled = false });
            trayMenu.Items.Add(new ToolStripSeparator());
            trayMenu.Items.Add(new ToolStripMenuItem("Focus Widget", null, (s, e) => ToggleWidgetFocus()));
            trayMenu.Items.Add(new ToolStripMenuItem("Toggle Mini Mode", null, (s, e) => ToggleMiniMode()));
            trayMenu.Items.Add(new ToolStripMenuItem("Pin to Top-Left", null, (s, e) => PinToCorner("top-left")));
            trayMenu.Items.Add(new ToolStripMenuItem("Pin to Top-Right", null, (s, e) => PinToCorner("top-right")));
            trayMenu.Items.Add(new ToolStripSeparator());
            trayMenu.Items.Add(new ToolStripMenuItem("Exit", null, (s, e) => Application.Exit()));

            trayIcon.ContextMenuStrip = trayMenu;
        }

        // ======================================================================
        // Drawing Utilities
        // ======================================================================
        private static GraphicsPath GetRoundedRect(Rectangle bounds, int radius)
        {
            int diameter = radius * 2;
            Size size = new Size(diameter, diameter);
            Rectangle arc = new Rectangle(bounds.Location, size);
            GraphicsPath path = new GraphicsPath();

            if (radius == 0)
            {
                path.AddRectangle(bounds);
                return path;
            }

            path.AddArc(arc, 180, 90);
            arc.X = bounds.Right - diameter;
            path.AddArc(arc, 270, 90);
            arc.Y = bounds.Bottom - diameter;
            path.AddArc(arc, 0, 90);
            arc.X = bounds.Left;
            path.AddArc(arc, 90, 90);
            path.CloseFigure();
            return path;
        }

        private static Color GetContrastingTextColor(Color bg)
        {
            double luminance = (0.299 * bg.R + 0.587 * bg.G + 0.114 * bg.B) / 255.0;
            return luminance > 0.55 ? Color.Black : Color.White;
        }
    }
}
