# ==============================================================================
# Installer for Native Windows WebView2 Desktop To-Do Widget (~2.2 MB Total)
# ==============================================================================

$ErrorActionPreference = "Stop"

$scriptDir = Split-Path -Parent $MyInvocation.MyCommand.Path
Set-Location $scriptDir

# Ensure native build exists
$distNative = Join-Path $scriptDir "dist-native"
if (-not (Test-Path (Join-Path $distNative "DesktopToDoWidget.exe"))) {
    Write-Host ">>> Building native widget first..." -ForegroundColor Cyan
    & powershell -ExecutionPolicy Bypass -File (Join-Path $scriptDir "build_native.ps1")
}

$installDir = Join-Path $env:LOCALAPPDATA "Programs\DesktopToDoWidget"
Write-Host ">>> Installing to $installDir..." -ForegroundColor Cyan

# Stop existing running instance
Stop-Process -Name "DesktopToDoWidget" -Force -ErrorAction SilentlyContinue
Start-Sleep -Milliseconds 600

# Copy files
if (-not (Test-Path $installDir)) {
    New-Item -ItemType Directory -Path $installDir -Force | Out-Null
}
Copy-Item (Join-Path $distNative "*") -Destination $installDir -Recurse -Force

# Create Desktop and Start Menu Shortcuts
$exePath = Join-Path $installDir "DesktopToDoWidget.exe"
$iconPath = Join-Path $installDir "assets\icon.ico"
$wsh = New-Object -ComObject WScript.Shell

# Desktop Shortcut
$desktopPath = [Environment]::GetFolderPath("Desktop")
$desktopShortcut = $wsh.CreateShortcut((Join-Path $desktopPath "To-Do Widget.lnk"))
$desktopShortcut.TargetPath = $exePath
$desktopShortcut.WorkingDirectory = $installDir
$desktopShortcut.IconLocation = "$iconPath,0"
$desktopShortcut.Description = "Desktop To-Do Widget"
$desktopShortcut.Save()
Write-Host ">>> Created Desktop shortcut" -ForegroundColor Green

# Start Menu Shortcut
$startMenuPath = [Environment]::GetFolderPath("Programs")
$startShortcut = $wsh.CreateShortcut((Join-Path $startMenuPath "To-Do Widget.lnk"))
$startShortcut.TargetPath = $exePath
$startShortcut.WorkingDirectory = $installDir
$startShortcut.IconLocation = "$iconPath,0"
$startShortcut.Description = "Desktop To-Do Widget"
$startShortcut.Save()
Write-Host ">>> Created Start Menu shortcut" -ForegroundColor Green

# Set Windows Auto-Start
Set-ItemProperty -Path "HKCU:\Software\Microsoft\Windows\CurrentVersion\Run" -Name "DesktopToDoWidget" -Value "`"$exePath`""
Write-Host ">>> Configured Windows Auto-Start" -ForegroundColor Green

# Launch Widget Detached
Invoke-CimMethod -ClassName Win32_Process -MethodName Create -Arguments @{CommandLine = "`"$exePath`""} | Out-Null
Write-Host "`n>>> Installation Complete! The widget is now running on your desktop homescreen." -ForegroundColor Cyan
