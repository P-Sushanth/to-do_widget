@echo off
setlocal
cd /d "%~dp0"

echo =======================================================
echo   Installing Windows 11 Desktop To-Do Widget
echo =======================================================
echo.

powershell -NoProfile -ExecutionPolicy Bypass -File "%~dp0install.ps1"

echo.
echo Launching widget now...
wscript.exe "%~dp0launch-widget.vbs"
echo.
echo Done! You can close this window.
pause
