const { execSync } = require('child_process');
const fs = require('fs');
const path = require('path');

// 1. Launch widget via launch-widget.vbs
execSync('wscript.exe launch-widget.vbs');
console.log('Launched via launch-widget.vbs');

// 2. Wait 2 seconds, then inspect all windows
setTimeout(() => {
    const listProc = execSync('powershell "Get-Process electron -ErrorAction SilentlyContinue | Select-Object Id, ProcessName, Responding"').toString();
    console.log('Electron process:\n', listProc);
}, 2000);
