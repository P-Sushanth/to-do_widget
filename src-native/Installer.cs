using System;
using System.IO;
using System.IO.Compression;
using System.Reflection;
using System.Diagnostics;
using System.Windows.Forms;
using Microsoft.Win32;

namespace DesktopToDoWidget.Installer
{
    static class Program
    {
        [STAThread]
        static void Main(string[] args)
        {
            bool isSilent = false;
            foreach (var arg in args)
            {
                if (arg.Equals("/S", StringComparison.OrdinalIgnoreCase) || arg.Equals("-s", StringComparison.OrdinalIgnoreCase))
                {
                    isSilent = true;
                }
            }

            try
            {
                // 1. Terminate running instance if any
                foreach (var proc in Process.GetProcessesByName("DesktopToDoWidget"))
                {
                    try
                    {
                        proc.Kill();
                        proc.WaitForExit(1500);
                    }
                    catch { }
                }

                // 2. Determine target install directory
                string localAppData = Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData);
                string installDir = Path.Combine(localAppData, "Programs", "DesktopToDoWidget");

                if (!Directory.Exists(installDir))
                {
                    Directory.CreateDirectory(installDir);
                }

                // 3. Extract embedded payload
                var assembly = Assembly.GetExecutingAssembly();
                using (var stream = assembly.GetManifestResourceStream("payload.zip"))
                {
                    if (stream == null)
                    {
                        if (!isSilent)
                        {
                            MessageBox.Show("Installation failed: payload archive not found.", "Desktop To-Do Widget Setup", MessageBoxButtons.OK, MessageBoxIcon.Error);
                        }
                        return;
                    }

                    using (var archive = new ZipArchive(stream, ZipArchiveMode.Read))
                    {
                        foreach (var entry in archive.Entries)
                        {
                            string destPath = Path.Combine(installDir, entry.FullName);
                            string destFolder = Path.GetDirectoryName(destPath);
                            if (!string.IsNullOrEmpty(destFolder) && !Directory.Exists(destFolder))
                            {
                                Directory.CreateDirectory(destFolder);
                            }

                            if (!string.IsNullOrEmpty(entry.Name))
                            {
                                using (var inStream = entry.Open())
                                using (var outStream = new FileStream(destPath, FileMode.Create, FileAccess.Write, FileShare.None))
                                {
                                    inStream.CopyTo(outStream);
                                }
                            }
                        }
                    }
                }

                // 4. Create Desktop and Start Menu Shortcuts
                string exePath = Path.Combine(installDir, "DesktopToDoWidget.exe");
                string iconPath = Path.Combine(installDir, "assets", "icon.ico");

                try
                {
                    Type shellType = Type.GetTypeFromProgID("WScript.Shell");
                    if (shellType != null)
                    {
                        dynamic shell = Activator.CreateInstance(shellType);

                        // Desktop Shortcut
                        string desktopPath = Environment.GetFolderPath(Environment.SpecialFolder.DesktopDirectory);
                        dynamic deskShortcut = shell.CreateShortcut(Path.Combine(desktopPath, "To-Do Widget.lnk"));
                        deskShortcut.TargetPath = exePath;
                        deskShortcut.WorkingDirectory = installDir;
                        if (File.Exists(iconPath)) deskShortcut.IconLocation = iconPath + ",0";
                        deskShortcut.Description = "Desktop To-Do Widget";
                        deskShortcut.Save();

                        // Start Menu Shortcut
                        string startMenuPath = Environment.GetFolderPath(Environment.SpecialFolder.Programs);
                        dynamic startShortcut = shell.CreateShortcut(Path.Combine(startMenuPath, "To-Do Widget.lnk"));
                        startShortcut.TargetPath = exePath;
                        startShortcut.WorkingDirectory = installDir;
                        if (File.Exists(iconPath)) startShortcut.IconLocation = iconPath + ",0";
                        startShortcut.Description = "Desktop To-Do Widget";
                        startShortcut.Save();
                    }
                }
                catch { }

                // 5. Register in Windows Auto-Start
                try
                {
                    using (RegistryKey runKey = Registry.CurrentUser.OpenSubKey(@"Software\Microsoft\Windows\CurrentVersion\Run", true))
                    {
                        if (runKey != null)
                        {
                            runKey.SetValue("DesktopToDoWidget", "\"" + exePath + "\"");
                        }
                    }
                }
                catch { }

                // 6. Launch Widget
                if (File.Exists(exePath))
                {
                    Process.Start(new ProcessStartInfo
                    {
                        FileName = exePath,
                        WorkingDirectory = installDir
                    });
                }

                if (!isSilent)
                {
                    MessageBox.Show(
                        "Desktop To-Do Widget installed successfully!\n\n• Shortcut placed on your Desktop\n• Shortcut placed in Start Menu\n• Added to Windows startup\n\nThe widget is now running live on your desktop wallpaper.",
                        "Desktop To-Do Widget Setup",
                        MessageBoxButtons.OK,
                        MessageBoxIcon.Information
                    );
                }
            }
            catch (Exception ex)
            {
                if (!isSilent)
                {
                    MessageBox.Show("Installation failed:\n" + ex.Message, "Setup Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
                }
            }
        }
    }
}
