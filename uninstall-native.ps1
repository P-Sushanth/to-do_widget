# ==============================================================================
# Uninstaller for Native Windows WebView2 Desktop To-Do Widget
# ==============================================================================

$ErrorActionPreference = "SilentlyContinue"

Write-Host ">>> Stopping running instances..." -ForegroundColor Yellow
Stop-Process -Name "DesktopToDoWidget" -Force

$desktopShortcut = Join-Path ([Environment]::GetFolderPath("Desktop")) "To-Do Widget.lnk"
if (Test-Path $desktopShortcut) { Remove-Item $desktopShortcut -Force }

$startShortcut = Join-Path ([Environment]::GetFolderPath("Programs")) "To-Do Widget.lnk"
if (Test-Path $startShortcut) { Remove-Item $startShortcut -Force }

Remove-ItemProperty -Path "HKCU:\Software\Microsoft\Windows\CurrentVersion\Run" -Name "DesktopToDoWidget" -ErrorAction SilentlyContinue

$installDir = Join-Path $env:LOCALAPPDATA "Programs\DesktopToDoWidget"
if (Test-Path $installDir) {
    Remove-Item $installDir -Recurse -Force
}

Write-Host ">>> Native Desktop To-Do Widget successfully uninstalled." -ForegroundColor Green
