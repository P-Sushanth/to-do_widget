@echo off
title Installing Native Desktop To-Do Widget
echo =========================================================
echo Installing Lightweight Native Desktop To-Do Widget (~2MB)
echo =========================================================
powershell.exe -NoProfile -ExecutionPolicy Bypass -File "%~dp0install-native.ps1"
pause
