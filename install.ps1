# Windows 11 To-Do Widget Installer Script
$ErrorActionPreference = "Stop"

$ProjectDir = $PSScriptRoot
if (-not $ProjectDir) {
    $ProjectDir = Split-Path -Parent $MyInvocation.MyCommand.Path
}

$VbsPath = Join-Path $ProjectDir "launch-widget.vbs"
$ElectronExe = Join-Path $ProjectDir "node_modules\electron\dist\electron.exe"
$IconPath = Join-Path $ProjectDir "assets\icon.ico"

$WshShell = New-Object -ComObject WScript.Shell

# 1. Desktop Shortcut
$DesktopPath = [Environment]::GetFolderPath("Desktop")
$DesktopShortcut = $WshShell.CreateShortcut((Join-Path $DesktopPath "To-Do Widget.lnk"))
$DesktopShortcut.TargetPath = $ElectronExe
$DesktopShortcut.Arguments = "."
$DesktopShortcut.WorkingDirectory = $ProjectDir
$DesktopShortcut.IconLocation = "$IconPath,0"
$DesktopShortcut.Description = "Windows 11 Desktop To-Do Task Widget"
$DesktopShortcut.Save()
Write-Host " [OK] Desktop shortcut updated." -ForegroundColor Green

# 2. Start Menu Shortcut
$StartMenuPath = [Environment]::GetFolderPath("Programs")
$StartMenuShortcut = $WshShell.CreateShortcut((Join-Path $StartMenuPath "To-Do Widget.lnk"))
$StartMenuShortcut.TargetPath = $ElectronExe
$StartMenuShortcut.Arguments = "."
$StartMenuShortcut.WorkingDirectory = $ProjectDir
$StartMenuShortcut.IconLocation = "$IconPath,0"
$StartMenuShortcut.Description = "Windows 11 Desktop To-Do Task Widget"
$StartMenuShortcut.Save()
Write-Host " [OK] Start Menu shortcut updated." -ForegroundColor Green

# 3. Windows Startup Shortcut (auto-starts on boot via silent VBS launcher)
$StartupPath = [Environment]::GetFolderPath("Startup")
$StartupShortcut = $WshShell.CreateShortcut((Join-Path $StartupPath "To-Do Widget.lnk"))
$StartupShortcut.TargetPath = "wscript.exe"
$StartupShortcut.Arguments = "`"$VbsPath`""
$StartupShortcut.WorkingDirectory = $ProjectDir
$StartupShortcut.IconLocation = "$IconPath,0"
$StartupShortcut.Description = "Windows 11 Desktop To-Do Task Widget (Startup)"
$StartupShortcut.Save()
Write-Host " [OK] Windows Startup shortcut updated (silent VBS launcher)." -ForegroundColor Green

# 4. Clean up broken registry auto-start entry (electron.exe without args)
try {
    $regPath = "HKCU:\Software\Microsoft\Windows\CurrentVersion\Run"
    $regEntry = Get-ItemProperty -Path $regPath -Name "electron.app.Electron" -ErrorAction SilentlyContinue
    if ($regEntry) {
        Remove-ItemProperty -Path $regPath -Name "electron.app.Electron" -ErrorAction SilentlyContinue
        Write-Host " [OK] Removed broken registry auto-start entry." -ForegroundColor Yellow
    }
} catch {}

Write-Host "`n Installation complete! The widget is now set up on your Desktop, Start Menu, and will automatically start when Windows boots." -ForegroundColor Cyan
