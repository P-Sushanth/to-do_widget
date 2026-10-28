const { app, BrowserWindow, ipcMain, Tray, Menu, nativeImage, screen, Notification, globalShortcut } = require('electron');
const path = require('path');
const fs = require('fs');

// Performance and high-DPI crisp text switches
app.commandLine.appendSwitch('disable-http-cache');
app.commandLine.appendSwitch('disable-component-update');
app.commandLine.appendSwitch('high-dpi-support', '1');
app.commandLine.appendSwitch('disable-gpu-vsync');       // reduce idle GPU wake-ups
app.commandLine.appendSwitch('disable-smooth-scrolling'); // lower compositor work for a small widget

let mainWindow = null;
let tray = null;
let isQuitting = false;

// In-memory cache to avoid redundant disk reads during window drag/resize
let cachedData = null;
let saveDirtyTimer = null;

// Determine storage path
const dataFilePath = path.join(app.getPath('userData'), 'todo-widget-data.json');

// Default initial state - strictly monochrome, Personal & Work, no priorities
const defaultData = {
    tasks: [
        {
            id: 'task-1',
            title: 'Welcome to your Desktop To-Do Widget',
            completed: false,
            category: 'Personal',
            dueDate: new Date().toISOString().split('T')[0],
            isDaily: true,
            createdAt: Date.now()
        },
        {
            id: 'task-2',
            title: 'Review project roadmap',
            completed: false,
            category: 'Work',
            dueDate: new Date().toISOString().split('T')[0],
            isDaily: false,
            createdAt: Date.now()
        },
        {
            id: 'task-3',
            title: 'Try clicking the checkbox to complete a task',
            completed: true,
            category: 'Personal',
            dueDate: '',
            isDaily: false,
            createdAt: Date.now()
        }
    ],
    categories: ['Personal', 'Work'],
    settings: {
        locked: false,
        opacity: 0.94,
        miniMode: false,
        autoStart: true,
        bounds: null,
        themeColor: '#000000'
    }
};

function loadStoredData() {
    if (cachedData) return cachedData;
    try {
        if (fs.existsSync(dataFilePath)) {
            const raw = fs.readFileSync(dataFilePath, 'utf8');
            const parsed = JSON.parse(raw);
            cachedData = {
                tasks: parsed.tasks || defaultData.tasks,
                categories: ['Personal', 'Work'], // strictly Personal and Work
                settings: { ...defaultData.settings, ...(parsed.settings || {}) }
            };
            return cachedData;
        }
    } catch (err) {
        console.error('Error loading data:', err);
    }
    cachedData = { ...defaultData };
    return cachedData;
}

function saveStoredData(data) {
    try {
        data.categories = ['Personal', 'Work'];
        cachedData = data;
        fs.writeFileSync(dataFilePath, JSON.stringify(data, null, 2), 'utf8');
    } catch (err) {
        console.error('Error saving data:', err);
    }
}

// Debounced save for high-frequency updates (window move/resize)
function saveStoredDataDebounced(data) {
    cachedData = data;
    clearTimeout(saveDirtyTimer);
    saveDirtyTimer = setTimeout(() => {
        saveStoredData(data);
    }, 300);
}

const logPath = path.join(__dirname, 'widget_runtime.log');
function logRuntime(msg) {
    try {
        const line = `[${new Date().toISOString()}] ${msg}\n`;
        fs.appendFileSync(logPath, line, 'utf8');
        console.log(msg);
    } catch(e) {}
}

let isElevated = false;

function elevateWidget() {
    logRuntime(`elevateWidget called. isElevated=${isElevated}, windowExists=${!!mainWindow}`);
    if (!mainWindow || mainWindow.isDestroyed()) return;
    if (mainWindow.isMinimized()) mainWindow.restore();
    mainWindow.show();
    // Momentarily elevate to bring to foreground over active apps
    mainWindow.setAlwaysOnTop(true);
    mainWindow.moveTop();
    app.focus({ steal: true });
    mainWindow.focus();
    // Immediately release alwaysOnTop so the widget lives strictly on the wallpaper layer
    setTimeout(() => {
        if (mainWindow && !mainWindow.isDestroyed()) {
            mainWindow.setAlwaysOnTop(false);
            logRuntime('Widget returned to desktop wallpaper layer (alwaysOnTop=false).');
        }
    }, 150);
    isElevated = true;
    logRuntime(`elevateWidget finished. isAlwaysOnTop=${mainWindow.isAlwaysOnTop()}, isVisible=${mainWindow.isVisible()}`);
}

function lowerWidget() {
    logRuntime(`lowerWidget called. Setting isAlwaysOnTop=false`);
    if (!mainWindow || mainWindow.isDestroyed()) return;
    mainWindow.setAlwaysOnTop(false);
    isElevated = false;
}

function toggleSummon() {
    logRuntime(`toggleSummon called. isFocused=${mainWindow && mainWindow.isFocused()}`);
    if (!mainWindow || mainWindow.isDestroyed()) return;
    if (mainWindow.isFocused()) {
        mainWindow.blur();
        lowerWidget();
    } else {
        elevateWidget();
    }
}

// Single instance enforcement
const gotTheLock = app.requestSingleInstanceLock();
if (!gotTheLock) {
    logRuntime('Another instance is running. Quitting duplicate instance gracefully.');
    app.quit();
} else {
    app.on('second-instance', () => {
        logRuntime('Second instance launched. Showing widget on desktop wallpaper.');
        if (mainWindow && !mainWindow.isDestroyed()) {
            if (mainWindow.isMinimized()) mainWindow.restore();
            mainWindow.show();
            mainWindow.setAlwaysOnTop(false);
            mainWindow.focus();
        }
    });
}

function createWindow() {
    const data = loadStoredData();
    const primaryDisplay = screen.getPrimaryDisplay();
    const { width: screenWidth, height: screenHeight } = primaryDisplay.workAreaSize;

    // Default position: top right corner with margin
    let defaultX = screenWidth - 380;
    let defaultY = 40;
    let defaultW = 350;
    let defaultH = 560;

    if (data.settings.bounds) {
        defaultX = data.settings.bounds.x ?? defaultX;
        defaultY = data.settings.bounds.y ?? defaultY;
        defaultW = data.settings.bounds.width ?? defaultW;
        defaultH = data.settings.bounds.height ?? defaultH;

        // Only reset if completely offscreen outside the display
        if (defaultX < -100 || defaultX > screenWidth - 50) defaultX = screenWidth - 380;
        if (defaultY < -50 || defaultY > screenHeight - 50) defaultY = 40;
    }

    const iconPath = path.join(__dirname, 'assets', 'icon.png');

    mainWindow = new BrowserWindow({
        x: defaultX,
        y: defaultY,
        width: defaultW,
        height: defaultH,
        minWidth: 280,
        minHeight: 250,
        frame: false,
        transparent: true,
        backgroundColor: '#00000000',
        skipTaskbar: true, // Acts as desktop gadget, doesn't clutter taskbar
        hasShadow: false,
        resizable: true,
        minimizable: false, // Prevents OS from showing standard minimize behavior
        show: true,
        icon: iconPath,
        webPreferences: {
            preload: path.join(__dirname, 'preload.js'),
            contextIsolation: true,
            nodeIntegration: false
        }
    });

    mainWindow.once('ready-to-show', () => {
        if (mainWindow && !mainWindow.isDestroyed()) {
            mainWindow.show();
            mainWindow.setAlwaysOnTop(false);
            isElevated = false;
            logRuntime('Widget placed strictly on homescreen / desktop.');
        }
    });

    // Strictly Desktop pinned - alwaysOnTop is disabled by default
    mainWindow.setAlwaysOnTop(false);

    // Homescreen persistence: Ensure widget never minimizes or hides when Win+D (Show Desktop) is triggered
    mainWindow.on('minimize', (e) => {
        e.preventDefault();
        logRuntime('minimize intercepted: keeping widget on homescreen');
        [10, 50, 150, 300, 500].forEach(delay => {
            setTimeout(() => {
                if (mainWindow && !mainWindow.isDestroyed()) {
                    if (mainWindow.isMinimized()) mainWindow.restore();
                    mainWindow.show();
                    mainWindow.setAlwaysOnTop(false);
                }
            }, delay);
        });
    });

    // Guard against unintended hides
    mainWindow.on('hide', () => {
        if (!isQuitting) {
            logRuntime('hide intercepted: restoring widget to homescreen');
            setTimeout(() => {
                if (mainWindow && !mainWindow.isDestroyed() && !isQuitting) {
                    mainWindow.show();
                    mainWindow.setAlwaysOnTop(false);
                }
            }, 50);
        }
    });

    // When focus is lost (blur), strictly enforce alwaysOnTop=false so widget sits behind other windows
    mainWindow.on('blur', () => {
        if (mainWindow && !mainWindow.isDestroyed()) {
            mainWindow.setAlwaysOnTop(false);
            isElevated = false;
            logRuntime('Widget blurred: strictly kept on desktop wallpaper layer.');
        }
    });

    // Magnetic edge & corner pinning (flush against screen boundaries)
    function snapBounds(bounds) {
        const display = screen.getDisplayMatching(bounds);
        const { x: waX, y: waY, width: waW, height: waH } = display.workArea;

        const SNAP_THRESHOLD = 45; // Magnetically snaps flush within 45px of screen boundaries

        let newX = bounds.x;
        let newY = bounds.y;
        let snapped = false;

        // Left edge & corners: Pin flush against left screen edge (waX = 0)
        if (bounds.x <= waX + SNAP_THRESHOLD) {
            newX = waX;
            snapped = true;
        }

        // Right edge & corners: Pin flush against right screen edge
        const rightTarget = waX + waW - bounds.width;
        if (bounds.x >= rightTarget - SNAP_THRESHOLD) {
            newX = rightTarget;
            snapped = true;
        }

        // Top edge & corners: Pin flush against top screen edge (waY = 0)
        if (bounds.y <= waY + SNAP_THRESHOLD) {
            newY = waY;
            snapped = true;
        }

        // Bottom edge & corners: Pin flush against bottom screen edge (above taskbar)
        const bottomTarget = waY + waH - bounds.height;
        if (bounds.y >= bottomTarget - SNAP_THRESHOLD) {
            newY = bottomTarget;
            snapped = true;
        }

        return {
            snapped,
            x: Math.round(newX),
            y: Math.round(newY),
            width: bounds.width,
            height: bounds.height
        };
    }

    let snapDebounceTimer = null;
    const updateBounds = () => {
        if (!mainWindow || mainWindow.isDestroyed()) return;
        clearTimeout(snapDebounceTimer);
        snapDebounceTimer = setTimeout(() => {
            if (!mainWindow || mainWindow.isDestroyed()) return;
            const currentData = loadStoredData(); // reads from memory cache, not disk
            if (!currentData.settings.locked) {
                const curBounds = mainWindow.getBounds();
                const snapped = snapBounds(curBounds);
                if (snapped.snapped) {
                    mainWindow.setBounds({
                        x: snapped.x,
                        y: snapped.y,
                        width: snapped.width,
                        height: snapped.height
                    });
                }
            }
            currentData.settings.bounds = mainWindow.getBounds();
            saveStoredDataDebounced(currentData); // debounced disk write for drag performance
        }, 100);
    };

    mainWindow.on('moved', updateBounds);
    mainWindow.on('resized', updateBounds);

    mainWindow.loadFile(path.join(__dirname, 'src', 'index.html'));

    mainWindow.on('close', (e) => {
        if (!isQuitting) {
            e.preventDefault();
        }
    });

    createTray();
}

function createTray() {
    if (tray) return;

    const trayIconPath = path.join(__dirname, 'assets', 'tray.png');
    let trayIcon;
    if (fs.existsSync(trayIconPath)) {
        trayIcon = nativeImage.createFromPath(trayIconPath);
    } else {
        trayIcon = nativeImage.createEmpty();
    }

    tray = new Tray(trayIcon);
    tray.setToolTip('Desktop To-Do Widget');

    const updateTrayMenu = () => {
        const data = loadStoredData();
        const isLocked = data.settings.locked;
        const autoStart = app.getLoginItemSettings().openAtLogin;

        const contextMenu = Menu.buildFromTemplate([
            {
                label: 'Desktop To-Do Widget',
                enabled: false,
                icon: nativeImage.createFromPath(path.join(__dirname, 'assets', 'icon_64.png')).resize({ width: 16, height: 16 })
            },
            { type: 'separator' },
            {
                label: mainWindow && mainWindow.isVisible() ? 'Hide Widget' : 'Show Widget',
                click: () => {
                    if (mainWindow.isVisible()) {
                        mainWindow.hide();
                    } else {
                        mainWindow.show();
                        mainWindow.focus();
                    }
                    updateTrayMenu();
                }
            },
            {
                label: isLocked ? 'Position: Locked 🔒' : 'Position: Draggable 🔓',
                click: () => {
                    data.settings.locked = !isLocked;
                    saveStoredData(data);
                    if (mainWindow) {
                        mainWindow.webContents.send('tray-action', 'toggle-lock', data.settings.locked);
                    }
                    updateTrayMenu();
                }
            },
            {
                label: 'Pin to Top-Left Corner 📌',
                click: () => pinWindowToCorner('top-left')
            },
            {
                label: 'Pin to Top-Right Corner 📌',
                click: () => pinWindowToCorner('top-right')
            },
            {
                label: 'Reset Position to Top-Right',
                click: () => {
                    resetWindowPosition();
                }
            },
            {
                label: 'Start with Windows',
                type: 'checkbox',
                checked: autoStart,
                click: (item) => {
                    setAutoStartVbs(item.checked);
                    data.settings.autoStart = item.checked;
                    saveStoredData(data);
                }
            },
            { type: 'separator' },
            {
                label: 'Quit Widget',
                click: () => {
                    isQuitting = true;
                    app.quit();
                }
            }
        ]);

        tray.setContextMenu(contextMenu);
    };

    updateTrayMenu();

    tray.on('click', () => {
        toggleSummon();
        updateTrayMenu();
    });
}

function pinWindowToCorner(corner = 'top-left') {
    if (!mainWindow || mainWindow.isDestroyed()) return;
    const bounds = mainWindow.getBounds();
    const display = screen.getDisplayMatching(bounds);
    const { x: waX, y: waY, width: waW, height: waH } = display.workArea;

    let targetX = waX;
    let targetY = waY;

    if (corner === 'top-right') {
        targetX = waX + waW - bounds.width;
        targetY = waY;
    } else if (corner === 'bottom-left') {
        targetX = waX;
        targetY = waY + waH - bounds.height;
    } else if (corner === 'bottom-right') {
        targetX = waX + waW - bounds.width;
        targetY = waY + waH - bounds.height;
    }

    mainWindow.setBounds({
        x: Math.round(targetX),
        y: Math.round(targetY),
        width: bounds.width,
        height: bounds.height
    });

    const currentData = loadStoredData();
    currentData.settings.bounds = mainWindow.getBounds();
    saveStoredData(currentData);
}

function resetWindowPosition() {
    if (!mainWindow) return;
    const primaryDisplay = screen.getPrimaryDisplay();
    const { width: screenWidth } = primaryDisplay.workAreaSize;
    mainWindow.setBounds({
        x: screenWidth - 380,
        y: 40,
        width: 350,
        height: 560
    });
    const currentData = loadStoredData();
    currentData.settings.bounds = mainWindow.getBounds();
    currentData.settings.miniMode = false;
    saveStoredData(currentData);
}

// IPC Handlers
ipcMain.handle('pin-to-corner', (event, corner) => {
    pinWindowToCorner(corner);
    return true;
});
ipcMain.handle('load-data', () => {
    return loadStoredData();
});

ipcMain.handle('save-data', (event, data) => {
    saveStoredData(data);
    return true;
});

ipcMain.handle('set-mini-mode', (event, isMini) => {
    if (!mainWindow) return;
    const currentBounds = mainWindow.getBounds();
    const data = loadStoredData();
    data.settings.miniMode = isMini;

    if (isMini) {
        data.settings.savedBounds = { width: currentBounds.width, height: currentBounds.height };
        // In collapsed mode, resize to compact width & height focused on tasks
        mainWindow.setMinimumSize(280, 200);
        mainWindow.setSize(currentBounds.width, Math.min(currentBounds.height, 380));
    } else {
        const saved = data.settings.savedBounds || { width: 350, height: 560 };
        mainWindow.setMinimumSize(280, 250);
        mainWindow.setSize(saved.width || 350, saved.height || 560);
    }
    saveStoredData(data);
    return isMini;
});

ipcMain.handle('set-window-opacity', (event, opacity) => {
    if (mainWindow) {
        mainWindow.setOpacity(Math.max(0.2, Math.min(1.0, opacity)));
    }
    return true;
});

ipcMain.handle('reset-position', () => {
    resetWindowPosition();
    return true;
});

ipcMain.handle('get-auto-start', () => {
    return isAutoStartEnabled();
});

ipcMain.handle('set-auto-start', (event, enable) => {
    setAutoStartVbs(enable);
    const data = loadStoredData();
    data.settings.autoStart = enable;
    saveStoredData(data);
    return enable;
});

ipcMain.handle('send-notification', (event, { title, body }) => {
    if (Notification.isSupported()) {
        new Notification({
            title: title || 'To-Do Widget',
            body: body || '',
            icon: path.join(__dirname, 'assets', 'icon.png')
        }).show();
    }
});

ipcMain.on('window-minimize', () => {
    // Desktop homescreen widget should not be banished to tray; toggle collapse mode instead
    if (mainWindow && !mainWindow.isDestroyed()) {
        mainWindow.webContents.send('tray-action', 'toggle-mini');
    }
});

ipcMain.on('window-close', () => {
    isQuitting = true;
    app.quit();
});

// ==========================================================================
// Auto-Start via VBS Launcher (silent, no CMD popup on boot)
// ==========================================================================
function getStartupShortcutPath() {
    return path.join(app.getPath('appData'), 'Microsoft', 'Windows', 'Start Menu', 'Programs', 'Startup', 'To-Do Widget.lnk');
}

function isAutoStartEnabled() {
    if (app.isPackaged) {
        return app.getLoginItemSettings().openAtLogin;
    }
    const startupExists = fs.existsSync(getStartupShortcutPath());
    const registryAutoStart = app.getLoginItemSettings().openAtLogin;
    return startupExists || registryAutoStart;
}

function setAutoStartVbs(enable) {
    if (app.isPackaged) {
        try {
            app.setLoginItemSettings({
                openAtLogin: enable,
                path: process.execPath
            });
            logRuntime(`Packaged auto-start set to: ${enable}`);
        } catch (err) {
            logRuntime(`Packaged auto-start error: ${err.message}`);
        }
        return;
    }

    const startupPath = getStartupShortcutPath();

    // Clear old registry-based auto-start (electron.exe without args = broken)
    try {
        app.setLoginItemSettings({ openAtLogin: false });
    } catch (e) {}

    if (enable) {
        // Create startup shortcut pointing to the VBS silent launcher
        try {
            const vbsLauncherPath = path.join(__dirname, 'launch-widget.vbs');
            // Use PowerShell to create a proper .lnk shortcut
            const psScript = `
                $WshShell = New-Object -ComObject WScript.Shell;
                $shortcut = $WshShell.CreateShortcut('${startupPath.replace(/'/g, "''")}');
                $shortcut.TargetPath = 'wscript.exe';
                $shortcut.Arguments = '"${vbsLauncherPath.replace(/'/g, "''")}"';
                $shortcut.WorkingDirectory = '${__dirname.replace(/'/g, "''")}'; 
                $iconPath = '${path.join(__dirname, 'assets', 'icon.ico').replace(/'/g, "''")}'; 
                if (Test-Path $iconPath) { $shortcut.IconLocation = "$iconPath,0" };
                $shortcut.Description = 'Desktop To-Do Widget (Startup)';
                $shortcut.Save();
            `.trim();
            require('child_process').execSync(`powershell -NoProfile -ExecutionPolicy Bypass -Command "${psScript.replace(/"/g, '\"').replace(/\n/g, ' ')}"`, { stdio: 'ignore' });
            logRuntime('Auto-start shortcut (VBS) created in Startup folder.');
        } catch (err) {
            logRuntime(`Failed to create startup shortcut: ${err.message}`);
            // Fallback: use Electron's native login item as backup
            app.setLoginItemSettings({ openAtLogin: true, path: process.execPath, args: ['.'] });
        }
    } else {
        // Remove startup shortcut
        try {
            if (fs.existsSync(startupPath)) fs.unlinkSync(startupPath);
            logRuntime('Auto-start shortcut removed from Startup folder.');
        } catch (err) {
            logRuntime(`Failed to remove startup shortcut: ${err.message}`);
        }
    }
}

// App Lifecycle
app.whenReady().then(() => {
    if (!gotTheLock) return;
    createWindow();

    // Ensure auto-start shortcut uses VBS (fix old broken registry entries)
    const data = loadStoredData();
    if (data.settings.autoStart) {
        setAutoStartVbs(true);
    }

    const shortcuts = [
        'F9',
        'CommandOrControl+Alt+T',
        'CommandOrControl+Shift+T',
        'Alt+Shift+T'
    ];

    shortcuts.forEach(sc => {
        try {
            const ok = globalShortcut.register(sc, () => {
                logRuntime(`Shortcut [${sc}] triggered by user!`);
                toggleSummon();
            });
            logRuntime(`Register shortcut [${sc}]: success=${ok}`);
        } catch (err) {
            logRuntime(`Failed to register shortcut [${sc}]: ${err.message}`);
        }
    });

    app.on('activate', () => {
        if (BrowserWindow.getAllWindows().length === 0) createWindow();
    });
});

app.on('before-quit', (e) => {
    isQuitting = true; // Ensure close handlers let the window go
    logRuntime(`app 'before-quit' event fired. isQuitting=${isQuitting}`);
    // Flush any pending debounced saves to disk
    if (saveDirtyTimer) {
        clearTimeout(saveDirtyTimer);
        if (cachedData) saveStoredData(cachedData);
    }
});

app.on('will-quit', () => {
    logRuntime(`app 'will-quit' event fired.`);
    globalShortcut.unregisterAll();
});

app.on('window-all-closed', () => {
    logRuntime(`app 'window-all-closed' event fired.`);
    // Do NOT quit when window is hidden/closed; it's a desktop widget that lives in tray
});

process.on('uncaughtException', (err) => {
    logRuntime(`UNCAUGHT EXCEPTION: ${err.stack || err}`);
});

process.on('unhandledRejection', (reason) => {
    logRuntime(`UNHANDLED REJECTION: ${reason}`);
});

process.on('exit', (code) => {
    logRuntime(`Process exiting with code ${code}`);
});
