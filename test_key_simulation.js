const { app, globalShortcut, BrowserWindow } = require('electron');
const { execSync } = require('child_process');

let fired = {
    ctrlAltT: false,
    ctrlShiftT: false
};

app.whenReady().then(() => {
    const win = new BrowserWindow({ width: 200, height: 200, show: false });

    globalShortcut.register('CommandOrControl+Alt+T', () => {
        console.log('[TEST LOG] Ctrl+Alt+T received!');
        fired.ctrlAltT = true;
    });

    globalShortcut.register('CommandOrControl+Shift+T', () => {
        console.log('[TEST LOG] Ctrl+Shift+T received!');
        fired.ctrlShiftT = true;
    });

    console.log('Shortcuts registered. Now simulating keypresses via PowerShell SendKeys...');

    setTimeout(() => {
        try {
            // PowerShell SendKeys: ^ = Ctrl, % = Alt, + = Shift
            // Send Ctrl+Alt+T
            execSync('powershell -Command "[System.Reflection.Assembly]::LoadWithPartialName(\\\"System.Windows.Forms\\\") | Out-Null; [System.Windows.Forms.SendKeys]::SendWait(\\\"^%t\\\")"');
            console.log('Sent ^%t (Ctrl+Alt+T)');
        } catch(e) {
            console.error('Error sending Ctrl+Alt+T:', e);
        }

        setTimeout(() => {
            try {
                // Send Ctrl+Shift+T
                execSync('powershell -Command "[System.Reflection.Assembly]::LoadWithPartialName(\\\"System.Windows.Forms\\\") | Out-Null; [System.Windows.Forms.SendKeys]::SendWait(\\\"^+t\\\")"');
                console.log('Sent ^+t (Ctrl+Shift+T)');
            } catch(e) {
                console.error('Error sending Ctrl+Shift+T:', e);
            }

            setTimeout(() => {
                console.log('Final Test Results:', JSON.stringify(fired));
                globalShortcut.unregisterAll();
                app.quit();
            }, 600);
        }, 600);
    }, 600);
});
