@echo off
title Uninstalling Native Desktop To-Do Widget
powershell.exe -NoProfile -ExecutionPolicy Bypass -File "%~dp0uninstall-native.ps1"
echo Done.
pause
