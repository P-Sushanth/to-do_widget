const { contextBridge, ipcRenderer } = require('electron');

contextBridge.exposeInMainWorld('widgetAPI', {
    // Data persistence
    loadData: () => ipcRenderer.invoke('load-data'),
    saveData: (data) => ipcRenderer.invoke('save-data', data),

    // Window controls
    minimizeWindow: () => ipcRenderer.send('window-minimize'),
    closeWindow: () => ipcRenderer.send('window-close'),
    toggleAlwaysOnTop: (flag) => ipcRenderer.invoke('toggle-always-on-top', flag),
    setMiniMode: (isMini) => ipcRenderer.invoke('set-mini-mode', isMini),
    setWindowOpacity: (opacity) => ipcRenderer.invoke('set-window-opacity', opacity),
    resetPosition: () => ipcRenderer.invoke('reset-position'),
    pinToCorner: (corner) => ipcRenderer.invoke('pin-to-corner', corner),
    
    // Windows startup
    getAutoStart: () => ipcRenderer.invoke('get-auto-start'),
    setAutoStart: (enable) => ipcRenderer.invoke('set-auto-start', enable),

    // Notifications
    sendNotification: (title, body) => ipcRenderer.invoke('send-notification', { title, body }),

    // Listeners from Tray / Main
    onTrayAction: (callback) => {
        ipcRenderer.on('tray-action', (event, action, payload) => callback(action, payload));
    }
});
