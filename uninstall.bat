@echo off
setlocal
echo Removing Windows 11 To-Do Widget shortcuts...

powershell -NoProfile -ExecutionPolicy Bypass -Command ^
  "Remove-Item (Join-Path ([Environment]::GetFolderPath('Desktop')) 'To-Do Widget.lnk') -ErrorAction SilentlyContinue; ^
   Remove-Item (Join-Path ([Environment]::GetFolderPath('Programs')) 'To-Do Widget.lnk') -ErrorAction SilentlyContinue; ^
   Remove-Item (Join-Path ([Environment]::GetFolderPath('Startup')) 'To-Do Widget.lnk') -ErrorAction SilentlyContinue;"

echo Shortcuts removed from Desktop, Start Menu, and Startup.
pause
