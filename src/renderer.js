// ==========================================================================
// Minimalist Monochrome Desktop Tasks Widget (Renderer Logic)
// ==========================================================================

let state = {
    tasks: [],
    categories: ['Personal', 'Work'],
    settings: {
        locked: false,
        opacity: 0.94,
        miniMode: false,
        autoStart: true,
        themeColor: '#000000'
    },
    filter: {
        category: 'All', // 'All', 'Personal', 'Work'
        status: 'all',   // 'all', 'pending', 'completed'
        search: '',
        sortBy: 'date-asc' // 'date-asc', 'date-desc', 'a-z', 'z-a'
    },
    // Form state for new tasks
    newCategory: 'Personal',
    newDueDate: '',
    newIsDaily: false,

    editingTaskId: null,
    lastDeletedTask: null,
    toastTimer: null,
    draggedTaskId: null
};

// DOM Elements
const widgetContainer = document.getElementById('widget-container');
const collapsedBar = document.getElementById('collapsed-bar');
const collapsedBadge = document.getElementById('collapsed-badge');
const btnUncollapse = document.getElementById('btn-uncollapse');

// Full Header Elements
const fullHeader = document.getElementById('full-header');
const btnSearchToggle = document.getElementById('btn-search-toggle');
const btnLockPos = document.getElementById('btn-lock-pos');
const iconUnlock = document.getElementById('icon-unlock');
const iconLock = document.getElementById('icon-lock');
const btnMiniMode = document.getElementById('btn-mini-mode');
const btnSettings = document.getElementById('btn-settings');

// Search Bar
const searchBar = document.getElementById('search-bar');
const searchInput = document.getElementById('search-input');
const btnSearchClear = document.getElementById('btn-search-clear');

// Category & Status Bars
const categoryBar = document.getElementById('category-bar');
const statusFilterBtns = document.querySelectorAll('.status-filter-btn');

// Task Lists
const activeTasksList = document.getElementById('active-tasks-list');
const completedGroupWrap = document.getElementById('completed-group-wrap');
const toggleCompletedBtn = document.getElementById('toggle-completed-btn');
const completedCountLabel = document.getElementById('completed-count-label');
const completedTasksList = document.getElementById('completed-tasks-list');
const emptyState = document.getElementById('empty-state');

// Quick Add Form
const addTaskForm = document.getElementById('add-task-form');
const taskInputTitle = document.getElementById('task-input-title');
const btnToggleCategory = document.getElementById('btn-toggle-category');
const categoryLabel = document.getElementById('category-label');

// Google Tasks Style Date & Repeat Popover Elements
const btnDueDatePicker = document.getElementById('btn-due-date-picker');
const dueDatePickerLabel = document.getElementById('due-date-picker-label');
const datePopover = document.getElementById('date-popover');
const popoverNativeDate = document.getElementById('popover-native-date');
const popoverCheckDaily = document.getElementById('popover-check-daily');
const btnClearDate = document.getElementById('btn-clear-date');
const btnApplyDate = document.getElementById('btn-apply-date');

const sortSelect = document.getElementById('sort-select');

// Settings Elements
const settingsDrawer = document.getElementById('settings-drawer');
const btnCloseSettings = document.getElementById('btn-close-settings');
const toggleAutostart = document.getElementById('toggle-autostart');
const btnResetPos = document.getElementById('btn-reset-pos');
const btnPinTopLeft = document.getElementById('btn-pin-top-left');
const btnPinTopRight = document.getElementById('btn-pin-top-right');

// Color Studio Elements
const colorPresetsRow = document.getElementById('color-presets-row');
const colorSvBox = document.getElementById('color-sv-box');
const colorSvCursor = document.getElementById('color-sv-cursor');
const colorHueSlider = document.getElementById('color-hue-slider');
const hueValLabel = document.getElementById('hue-val-label');
const colorLightnessSlider = document.getElementById('color-lightness-slider');
const lightnessValLabel = document.getElementById('lightness-val-label');
const colorActiveSwatch = document.getElementById('color-active-swatch');
const colorHexInput = document.getElementById('color-hex-input');
const nativeColorPicker = document.getElementById('native-color-picker');
const btnEyedropper = document.getElementById('btn-eyedropper');
const btnResetColor = document.getElementById('btn-reset-color');

// Edit Modal
const taskEditModal = document.getElementById('task-edit-modal');
const btnCloseEdit = document.getElementById('btn-close-edit');
const btnCancelEdit = document.getElementById('btn-cancel-edit');
const btnSaveEdit = document.getElementById('btn-save-edit');
const editTaskTitle = document.getElementById('edit-task-title');
const editTaskCategory = document.getElementById('edit-task-category');
const editTaskDate = document.getElementById('edit-task-date');
const editTaskDaily = document.getElementById('edit-task-daily');

// Toast
const toastBar = document.getElementById('toast-bar');
const toastMessage = document.getElementById('toast-message');
const btnToastUndo = document.getElementById('btn-toast-undo');

const defaultTasks = [
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
];

// ==========================================================================
// Initialization & Data Loading
// ==========================================================================
// ponytail: sync cache for instant 0ms first-paint, async file sync in background
function loadLocalCache() {
    try {
        const cachedTasks = localStorage.getItem('todo_widget_tasks');
        if (cachedTasks) {
            state.tasks = JSON.parse(cachedTasks);
        } else {
            state.tasks = [...defaultTasks];
        }
        const cachedSettings = localStorage.getItem('todo_widget_settings');
        if (cachedSettings) {
            state.settings = { ...state.settings, ...JSON.parse(cachedSettings) };
        }
        const cachedColor = localStorage.getItem('todo_widget_theme_color');
        if (cachedColor) {
            state.settings.themeColor = cachedColor;
        } else if (!state.settings.themeColor) {
            state.settings.themeColor = '#000000';
        }
        applyThemeColor(state.settings.themeColor, false, false);

        const cachedSort = localStorage.getItem('todo_widget_sort');
        if (cachedSort) {
            state.filter.sortBy = cachedSort;
        }
    } catch (e) {
        state.tasks = [...defaultTasks];
    }
}

async function initWidget() {
    // 1. Instant synchronous first-paint (0ms)
    loadLocalCache();
    if (sortSelect) sortSelect.value = state.filter.sortBy;
    applyLockState();
    applyCollapseMode(state.settings.miniMode, false);
    renderTasks();
    initColorStudio();

    // 2. Non-blocking background sync with main process
    if (window.widgetAPI && window.widgetAPI.loadData) {
        window.widgetAPI.loadData().then(stored => {
            if (stored && stored.tasks && stored.tasks.length > 0) {
                state.tasks = stored.tasks.map(t => ({
                    ...t,
                    category: t.category === 'Work' ? 'Work' : 'Personal',
                    isDaily: !!t.isDaily
                }));
                state.settings = { ...state.settings, ...(stored.settings || {}) };
                if (stored.settings && stored.settings.themeColor) {
                    state.settings.themeColor = stored.settings.themeColor;
                    applyThemeColor(state.settings.themeColor, true, false);
                }
                applyOpacity(state.settings.opacity);
                applyLockState();
                renderTasks();
            }
        }).catch(() => {});

        try {
            window.widgetAPI.onTrayAction((action, payload) => {
                if (action === 'toggle-lock') {
                    state.settings.locked = payload;
                    applyLockState();
                } else if (action === 'toggle-mini') {
                    applyCollapseMode(!state.settings.miniMode);
                    persistState();
                }
            });
        } catch (e) {}
    }

    if (window.widgetAPI && window.widgetAPI.getAutoStart) {
        window.widgetAPI.getAutoStart().then(autoStart => {
            state.settings.autoStart = autoStart;
            toggleAutostart.checked = autoStart;
        }).catch(() => {});
    }
}

let _persistTimer = null;
function persistState() {
    // Instant sync save (zero-latency local mirror)
    try {
        localStorage.setItem('todo_widget_tasks', JSON.stringify(state.tasks));
        localStorage.setItem('todo_widget_settings', JSON.stringify(state.settings));
    } catch (e) {}

    // Debounced async file save (coalesce rapid edits)
    clearTimeout(_persistTimer);
    _persistTimer = setTimeout(() => {
        if (window.widgetAPI && window.widgetAPI.saveData) {
            window.widgetAPI.saveData({
                tasks: state.tasks,
                categories: ['Personal', 'Work'],
                settings: state.settings
            }).catch(() => {});
        }
    }, 150);
}

// ==========================================================================
// Settings & State Modifiers
// ==========================================================================
function applyOpacity(opacity) {
    state.settings.opacity = 1;
    widgetContainer.style.backgroundColor = '';
}

function applyLockState() {
    const isLocked = state.settings.locked;
    widgetContainer.classList.toggle('position-locked', isLocked);
    iconUnlock.classList.toggle('hidden', isLocked);
    iconLock.classList.toggle('hidden', !isLocked);
    btnLockPos.title = isLocked ? 'Position Locked (Click to Unlock)' : 'Lock Position';
}

// Requirement 11: When clicked on collapse to pill, show ONLY the tasks
function applyCollapseMode(isCollapsed, callIpc = true) {
    state.settings.miniMode = isCollapsed;
    widgetContainer.classList.toggle('collapsed-tasks-only', isCollapsed);

    if (callIpc && window.widgetAPI && window.widgetAPI.setMiniMode) {
        window.widgetAPI.setMiniMode(isCollapsed).catch(() => {});
    }
    updateCollapsedCount();
}

function updateCollapsedCount() {
    const pending = state.tasks.filter(t => !t.completed).length;
    collapsedBadge.textContent = pending;
}

// ==========================================================================
// Color Engine & Dynamic Theme Shades
// ==========================================================================

function hexToRgb(hex) {
    if (!hex) return { r: 0, g: 0, b: 0 };
    let c = hex.replace('#', '').trim();
    if (c.length === 3) {
        c = c.split('').map(ch => ch + ch).join('');
    }
    if (c.length !== 6) return { r: 0, g: 0, b: 0 };
    const num = parseInt(c, 16);
    if (isNaN(num)) return { r: 0, g: 0, b: 0 };
    return {
        r: (num >> 16) & 255,
        g: (num >> 8) & 255,
        b: num & 255
    };
}

function rgbToHex(r, g, b) {
    const toH = (n) => {
        const cl = Math.max(0, Math.min(255, Math.round(n)));
        return cl.toString(16).padStart(2, '0');
    };
    return `#${toH(r)}${toH(g)}${toH(b)}`.toLowerCase();
}

function rgbToHsl(r, g, b) {
    r /= 255;
    g /= 255;
    b /= 255;
    const max = Math.max(r, g, b);
    const min = Math.min(r, g, b);
    let h = 0, s = 0;
    const l = (max + min) / 2;

    if (max !== min) {
        const d = max - min;
        s = l > 0.5 ? d / (2 - max - min) : d / (max + min);
        switch (max) {
            case r: h = (g - b) / d + (g < b ? 6 : 0); break;
            case g: h = (b - r) / d + 2; break;
            case b: h = (r - g) / d + 4; break;
        }
        h /= 6;
    }

    return {
        h: Math.round(h * 360),
        s: Math.round(s * 100),
        l: Math.round(l * 100)
    };
}

function hslToHex(h, s, l) {
    h = (h % 360 + 360) % 360;
    s = Math.max(0, Math.min(100, s)) / 100;
    l = Math.max(0, Math.min(100, l)) / 100;

    const c = (1 - Math.abs(2 * l - 1)) * s;
    const x = c * (1 - Math.abs(((h / 60) % 2) - 1));
    const m = l - c / 2;

    let r = 0, g = 0, b = 0;
    if (h >= 0 && h < 60) {
        r = c; g = x; b = 0;
    } else if (h >= 60 && h < 120) {
        r = x; g = c; b = 0;
    } else if (h >= 120 && h < 180) {
        r = 0; g = c; b = x;
    } else if (h >= 180 && h < 240) {
        r = 0; g = x; b = c;
    } else if (h >= 240 && h < 300) {
        r = x; g = 0; b = c;
    } else {
        r = c; g = 0; b = x;
    }

    return rgbToHex((r + m) * 255, (g + m) * 255, (b + m) * 255);
}

function rgbToHsv(r, g, b) {
    r /= 255;
    g /= 255;
    b /= 255;
    const max = Math.max(r, g, b);
    const min = Math.min(r, g, b);
    const d = max - min;
    let h = 0;
    const s = max === 0 ? 0 : d / max;
    const v = max;

    if (max !== min) {
        switch (max) {
            case r: h = (g - b) / d + (g < b ? 6 : 0); break;
            case g: h = (b - r) / d + 2; break;
            case b: h = (r - g) / d + 4; break;
        }
        h /= 6;
    }

    return {
        h: Math.round(h * 360),
        s: s,
        v: v
    };
}

function hsvToRgb(h, s, v) {
    h = (h % 360 + 360) % 360;
    s = Math.max(0, Math.min(1, s));
    v = Math.max(0, Math.min(1, v));

    const c = v * s;
    const x = c * (1 - Math.abs(((h / 60) % 2) - 1));
    const m = v - c;

    let r = 0, g = 0, b = 0;
    if (h >= 0 && h < 60) {
        r = c; g = x; b = 0;
    } else if (h >= 60 && h < 120) {
        r = x; g = c; b = 0;
    } else if (h >= 120 && h < 180) {
        r = 0; g = c; b = x;
    } else if (h >= 180 && h < 240) {
        r = 0; g = x; b = c;
    } else if (h >= 240 && h < 300) {
        r = x; g = 0; b = c;
    } else {
        r = c; g = 0; b = x;
    }

    return {
        r: Math.round((r + m) * 255),
        g: Math.round((g + m) * 255),
        b: Math.round((b + m) * 255)
    };
}

function computeThemeShades(hex) {
    const cleanHex = (hex && hex.startsWith('#') ? hex : '#' + (hex || '000000')).toLowerCase();
    const rgb = hexToRgb(cleanHex);
    const hsl = rgbToHsl(rgb.r, rgb.g, rgb.b);
    const { h, s, l } = hsl;

    // Special case: Default pitch black / pure dark monochrome
    if (cleanHex === '#000000' || (s <= 5 && l <= 3)) {
        return {
            bgWindow: '#000000',
            bgSurface: '#0a0a0a',
            bgSurfaceHover: '#141414',
            bgSurfaceActive: '#1c1c1c',
            bgPopover: '#0a0a0a',
            borderWindow: '#1e1e1e',
            borderSubtle: '#141414',
            borderHover: '#2d2d2d',
            textPrimary: '#ffffff',
            textSecondary: '#a1a1aa',
            textMuted: '#71717a',
            textDisabled: '#52525b',
            accentSolid: '#ffffff',
            accentHover: '#e4e4e7',
            accentSubtle: 'rgba(255, 255, 255, 0.08)',
            accentText: '#ffffff',
            accentContrast: '#000000',
            shadowWindow: '0 12px 32px rgba(0, 0, 0, 0.9), 0 0 0 1px #1e1e1e'
        };
    }

    // Dark & Medium Backgrounds (L <= 55)
    if (l <= 55) {
        const surfSat = Math.min(100, Math.round(s * 0.9 + 4));
        const surfL = Math.min(32, Math.round(l + 6));
        const surfHoverL = Math.min(40, Math.round(l + 12));
        const surfActiveL = Math.min(48, Math.round(l + 18));
        const popoverL = Math.min(34, Math.round(l + 7));

        const borderSat = Math.min(100, Math.round(s * 0.8 + 6));
        const borderWinL = Math.min(45, Math.round(l + 14));
        const borderSubtleL = Math.min(35, Math.round(l + 8));
        const borderHoverL = Math.min(55, Math.round(l + 24));

        let accentSolid, accentHover, accentSubtle, accentText, accentContrast;
        if (s > 6) {
            // Punchy, vibrant shade of the selected color hue for buttons & icons!
            const accentSat = Math.max(76, Math.min(100, s));
            const accentL = Math.max(52, Math.min(68, l + 35));
            accentSolid = hslToHex(h, accentSat, accentL);
            accentHover = hslToHex(h, accentSat, Math.min(78, accentL + 8));
            accentSubtle = `hsla(${h}, ${Math.max(70, s)}%, 55%, 0.18)`;
            accentText = hslToHex(h, Math.max(75, s), 78);
            const accRgb = hexToRgb(accentSolid);
            const accLum = 0.299 * accRgb.r + 0.587 * accRgb.g + 0.114 * accRgb.b;
            accentContrast = accLum > 140 ? '#000000' : '#ffffff';
        } else {
            accentSolid = '#ffffff';
            accentHover = '#e4e4e7';
            accentSubtle = 'rgba(255, 255, 255, 0.12)';
            accentText = '#ffffff';
            accentContrast = '#000000';
        }

        const borderWinHex = hslToHex(h, borderSat, borderWinL);

        return {
            bgWindow: cleanHex,
            bgSurface: hslToHex(h, surfSat, surfL),
            bgSurfaceHover: hslToHex(h, surfSat, surfHoverL),
            bgSurfaceActive: hslToHex(h, surfSat, surfActiveL),
            bgPopover: hslToHex(h, surfSat, popoverL),
            borderWindow: borderWinHex,
            borderSubtle: hslToHex(h, borderSat, borderSubtleL),
            borderHover: hslToHex(h, borderSat, borderHoverL),
            textPrimary: '#ffffff',
            textSecondary: hslToHex(h, Math.min(25, Math.round(s * 0.3)), 74),
            textMuted: hslToHex(h, Math.min(20, Math.round(s * 0.25)), 54),
            textDisabled: hslToHex(h, Math.min(15, Math.round(s * 0.2)), 38),
            accentSolid,
            accentHover,
            accentSubtle,
            accentText,
            accentContrast,
            shadowWindow: `0 12px 32px rgba(0, 0, 0, 0.85), 0 0 0 1px ${borderWinHex}`
        };
    }

    // Light Backgrounds (L > 55)
    const surfSat = Math.max(10, Math.round(s * 0.8));
    const surfL = Math.max(82, Math.round(l - 6));
    const surfHoverL = Math.max(74, Math.round(l - 12));
    const surfActiveL = Math.max(68, Math.round(l - 18));
    const popoverL = Math.max(86, Math.round(l - 4));

    const borderSat = Math.max(12, Math.round(s * 0.7));
    const borderWinL = Math.max(50, Math.round(l - 24));
    const borderSubtleL = Math.max(70, Math.round(l - 14));
    const borderHoverL = Math.max(40, Math.round(l - 32));

    const accentSat = Math.max(75, s);
    const accentL = Math.min(42, Math.max(22, Math.round(l - 30)));
    const accentSolid = hslToHex(h, accentSat, accentL);
    const accentHover = hslToHex(h, accentSat, Math.max(16, accentL - 8));
    const accentSubtle = `hsla(${h}, ${Math.max(65, s)}%, 40%, 0.15)`;
    const accentText = hslToHex(h, Math.max(80, s), 28);
    const borderWinHex = hslToHex(h, borderSat, borderWinL);

    return {
        bgWindow: cleanHex,
        bgSurface: hslToHex(h, surfSat, surfL),
        bgSurfaceHover: hslToHex(h, surfSat, surfHoverL),
        bgSurfaceActive: hslToHex(h, surfSat, surfActiveL),
        bgPopover: hslToHex(h, surfSat, popoverL),
        borderWindow: borderWinHex,
        borderSubtle: hslToHex(h, borderSat, borderSubtleL),
        borderHover: hslToHex(h, borderSat, borderHoverL),
        textPrimary: '#09090b',
        textSecondary: hslToHex(h, Math.min(30, Math.round(s * 0.3)), 26),
        textMuted: hslToHex(h, Math.min(25, Math.round(s * 0.25)), 44),
        textDisabled: hslToHex(h, Math.min(20, Math.round(s * 0.2)), 62),
        accentSolid,
        accentHover,
        accentSubtle,
        accentText,
        accentContrast: '#ffffff',
        shadowWindow: `0 12px 32px rgba(0, 0, 0, 0.25), 0 0 0 1px ${borderWinHex}`
    };
}

let _currentThemeH = 0;
let _currentThemeS = 0;
let _currentThemeV = 0;

function applyThemeColor(hex, syncControls = true, persist = true) {
    if (!hex) hex = '#000000';
    let cleanHex = hex.trim();
    if (!cleanHex.startsWith('#')) cleanHex = '#' + cleanHex;
    if (!/^#[0-9a-fA-F]{6}$/i.test(cleanHex)) {
        if (/^#[0-9a-fA-F]{3}$/i.test(cleanHex)) {
            cleanHex = '#' + cleanHex[1] + cleanHex[1] + cleanHex[2] + cleanHex[2] + cleanHex[3] + cleanHex[3];
        } else {
            cleanHex = '#000000';
        }
    }
    cleanHex = cleanHex.toLowerCase();
    state.settings.themeColor = cleanHex;

    const shades = computeThemeShades(cleanHex);
    const rootStyle = document.documentElement.style;

    rootStyle.setProperty('--bg-window', shades.bgWindow);
    rootStyle.setProperty('--bg-surface', shades.bgSurface);
    rootStyle.setProperty('--bg-surface-hover', shades.bgSurfaceHover);
    rootStyle.setProperty('--bg-surface-active', shades.bgSurfaceActive);
    rootStyle.setProperty('--bg-popover', shades.bgPopover);
    rootStyle.setProperty('--border-window', shades.borderWindow);
    rootStyle.setProperty('--border-subtle', shades.borderSubtle);
    rootStyle.setProperty('--border-hover', shades.borderHover);
    rootStyle.setProperty('--text-primary', shades.textPrimary);
    rootStyle.setProperty('--text-secondary', shades.textSecondary);
    rootStyle.setProperty('--text-muted', shades.textMuted);
    rootStyle.setProperty('--text-disabled', shades.textDisabled);
    rootStyle.setProperty('--accent-solid', shades.accentSolid);
    rootStyle.setProperty('--accent-hover', shades.accentHover);
    rootStyle.setProperty('--accent-subtle', shades.accentSubtle);
    rootStyle.setProperty('--accent-text', shades.accentText);
    rootStyle.setProperty('--accent-contrast', shades.accentContrast);
    rootStyle.setProperty('--shadow-window', shades.shadowWindow);

    // Sync UI elements
    if (colorActiveSwatch) {
        colorActiveSwatch.style.backgroundColor = cleanHex;
    }
    if (colorHexInput && document.activeElement !== colorHexInput) {
        colorHexInput.value = cleanHex.replace('#', '').toUpperCase();
    }
    if (nativeColorPicker) {
        nativeColorPicker.value = cleanHex;
    }

    // Preset chips highlight
    if (colorPresetsRow) {
        colorPresetsRow.querySelectorAll('.preset-chip').forEach(chip => {
            const chipColor = (chip.dataset.color || '').toLowerCase();
            chip.classList.toggle('active', chipColor === cleanHex);
        });
    }

    const rgb = hexToRgb(cleanHex);
    const hsv = rgbToHsv(rgb.r, rgb.g, rgb.b);
    const hsl = rgbToHsl(rgb.r, rgb.g, rgb.b);

    if (syncControls) {
        if (hsl.s > 4) {
            _currentThemeH = hsv.h;
        }
        _currentThemeS = hsv.s;
        _currentThemeV = hsv.v;

        if (colorHueSlider) {
            colorHueSlider.value = _currentThemeH;
        }
        if (hueValLabel) {
            hueValLabel.textContent = _currentThemeH + '°';
        }
        if (colorLightnessSlider) {
            colorLightnessSlider.value = hsl.l;
            colorLightnessSlider.style.background = `linear-gradient(to right, #000000, hsl(${_currentThemeH}, 100%, 50%), #ffffff)`;
        }
        if (lightnessValLabel) {
            lightnessValLabel.textContent = hsl.l + '%';
        }
        if (colorSvBox) {
            colorSvBox.style.backgroundColor = `hsl(${_currentThemeH}, 100%, 50%)`;
        }
        if (colorSvCursor) {
            colorSvCursor.style.left = (_currentThemeS * 100) + '%';
            colorSvCursor.style.top = ((1 - _currentThemeV) * 100) + '%';
        }
    }

    if (persist) {
        try {
            localStorage.setItem('todo_widget_theme_color', cleanHex);
        } catch (e) {}
        persistState();
    }
}

function initColorStudio() {
    if (!colorSvBox || !colorHueSlider) return;

    // 1. Presets Chips
    if (colorPresetsRow) {
        colorPresetsRow.querySelectorAll('.preset-chip').forEach(chip => {
            chip.addEventListener('click', () => {
                const col = chip.dataset.color;
                if (col) {
                    applyThemeColor(col, true, true);
                }
            });
        });
    }

    // 2. 2D SV Box Dragging
    let isDragging = false;

    function handleSvMove(clientX, clientY) {
        const rect = colorSvBox.getBoundingClientRect();
        if (!rect.width || !rect.height) return;

        const x = Math.max(0, Math.min(rect.width, clientX - rect.left));
        const y = Math.max(0, Math.min(rect.height, clientY - rect.top));

        _currentThemeS = x / rect.width;
        _currentThemeV = 1 - (y / rect.height);

        colorSvCursor.style.left = (_currentThemeS * 100) + '%';
        colorSvCursor.style.top = ((1 - _currentThemeV) * 100) + '%';

        const rgb = hsvToRgb(_currentThemeH, _currentThemeS, _currentThemeV);
        const hex = rgbToHex(rgb.r, rgb.g, rgb.b);

        applyThemeColor(hex, false, true);

        const hsl = rgbToHsl(rgb.r, rgb.g, rgb.b);
        if (colorLightnessSlider) colorLightnessSlider.value = hsl.l;
        if (lightnessValLabel) lightnessValLabel.textContent = hsl.l + '%';
    }

    colorSvBox.addEventListener('pointerdown', (e) => {
        isDragging = true;
        colorSvBox.setPointerCapture(e.pointerId);
        handleSvMove(e.clientX, e.clientY);
    });

    colorSvBox.addEventListener('pointermove', (e) => {
        if (isDragging) {
            handleSvMove(e.clientX, e.clientY);
        }
    });

    const stopDragging = (e) => {
        if (isDragging) {
            isDragging = false;
            try { colorSvBox.releasePointerCapture(e.pointerId); } catch (err) {}
        }
    };
    colorSvBox.addEventListener('pointerup', stopDragging);
    colorSvBox.addEventListener('pointercancel', stopDragging);

    // 3. Hue Slider (0 - 360)
    colorHueSlider.addEventListener('input', () => {
        _currentThemeH = parseInt(colorHueSlider.value, 10);
        if (hueValLabel) hueValLabel.textContent = _currentThemeH + '°';

        colorSvBox.style.backgroundColor = `hsl(${_currentThemeH}, 100%, 50%)`;
        colorLightnessSlider.style.background = `linear-gradient(to right, #000000, hsl(${_currentThemeH}, 100%, 50%), #ffffff)`;

        if (_currentThemeS < 0.05 && _currentThemeV < 0.05) {
            _currentThemeS = 0.85;
            _currentThemeV = 0.25;
            colorSvCursor.style.left = (_currentThemeS * 100) + '%';
            colorSvCursor.style.top = ((1 - _currentThemeV) * 100) + '%';
        }

        const rgb = hsvToRgb(_currentThemeH, _currentThemeS, _currentThemeV);
        const hex = rgbToHex(rgb.r, rgb.g, rgb.b);

        applyThemeColor(hex, false, true);

        const hsl = rgbToHsl(rgb.r, rgb.g, rgb.b);
        if (colorLightnessSlider) colorLightnessSlider.value = hsl.l;
        if (lightnessValLabel) lightnessValLabel.textContent = hsl.l + '%';
    });

    // 4. Lightness / Shade Slider (0 - 100)
    colorLightnessSlider.addEventListener('input', () => {
        const targetL = parseInt(colorLightnessSlider.value, 10);
        if (lightnessValLabel) lightnessValLabel.textContent = targetL + '%';

        const curRgb = hexToRgb(state.settings.themeColor || '#000000');
        const curHsl = rgbToHsl(curRgb.r, curRgb.g, curRgb.b);
        const effectiveHue = curHsl.s > 4 ? curHsl.h : _currentThemeH;
        const effectiveSat = curHsl.s > 4 ? curHsl.s : (_currentThemeS > 0.05 ? Math.round(_currentThemeS * 100) : 80);

        const newHex = hslToHex(effectiveHue, effectiveSat, targetL);
        applyThemeColor(newHex, true, true);
    });

    // 5. Hex Input
    colorHexInput.addEventListener('input', () => {
        let val = colorHexInput.value.replace(/[^0-9a-fA-F]/g, '');
        if (val.length === 6) {
            applyThemeColor('#' + val, true, true);
        }
    });

    colorHexInput.addEventListener('blur', () => {
        let val = colorHexInput.value.replace(/[^0-9a-fA-F]/g, '');
        if (val.length === 3) {
            val = val.split('').map(c => c + c).join('');
            applyThemeColor('#' + val, true, true);
        } else if (val.length !== 6) {
            colorHexInput.value = (state.settings.themeColor || '#000000').replace('#', '').toUpperCase();
        }
    });

    colorHexInput.addEventListener('keydown', (e) => {
        if (e.key === 'Enter') {
            colorHexInput.blur();
        }
    });

    // 6. Eyedropper / System Picker
    if (btnEyedropper) {
        btnEyedropper.addEventListener('click', async () => {
            if (window.EyeDropper) {
                try {
                    const eyeDropper = new window.EyeDropper();
                    const result = await eyeDropper.open();
                    if (result && result.sRGBHex) {
                        applyThemeColor(result.sRGBHex, true, true);
                        showToast('Color picked');
                    }
                } catch (e) {
                    // canceled
                }
            } else if (nativeColorPicker) {
                nativeColorPicker.click();
            }
        });
    }

    if (nativeColorPicker) {
        nativeColorPicker.addEventListener('input', () => {
            applyThemeColor(nativeColorPicker.value, true, true);
        });
    }

    // 7. Reset to Default Pure Black
    if (btnResetColor) {
        btnResetColor.addEventListener('click', () => {
            applyThemeColor('#000000', true, true);
            showToast('Reset to Pitch Black');
        });
    }

    // Initial controls sync
    applyThemeColor(state.settings.themeColor || '#000000', true, false);
}

// ==========================================================================
// Rendering Tasks
// ==========================================================================
function renderTasks() {
    activeTasksList.innerHTML = '';
    completedTasksList.innerHTML = '';

    let filtered = state.tasks.filter(task => {
        // Category filter: 'All', 'Personal', 'Work'
        if (state.filter.category !== 'All' && task.category !== state.filter.category) {
            return false;
        }
        // Status filter
        if (state.filter.status === 'pending' && task.completed) return false;
        if (state.filter.status === 'completed' && !task.completed) return false;
        // Search filter
        if (state.filter.search.trim()) {
            const query = state.filter.search.toLowerCase();
            const matchTitle = task.title.toLowerCase().includes(query);
            const matchCategory = task.category.toLowerCase().includes(query);
            if (!matchTitle && !matchCategory) return false;
        }
        return true;
    });

    // Sort tasks based on state.filter.sortBy ('date-asc', 'date-desc', 'a-z', 'z-a')
    filtered.sort((a, b) => {
        if (state.filter.sortBy === 'date-asc') {
            if (!a.dueDate && !b.dueDate) return 0;
            if (!a.dueDate) return 1;
            if (!b.dueDate) return -1;
            return a.dueDate.localeCompare(b.dueDate);
        }
        if (state.filter.sortBy === 'date-desc') {
            if (!a.dueDate && !b.dueDate) return 0;
            if (!a.dueDate) return 1;
            if (!b.dueDate) return -1;
            return b.dueDate.localeCompare(a.dueDate);
        }
        if (state.filter.sortBy === 'a-z') {
            return a.title.localeCompare(b.title, undefined, { sensitivity: 'base' });
        }
        if (state.filter.sortBy === 'z-a') {
            return b.title.localeCompare(a.title, undefined, { sensitivity: 'base' });
        }
        return 0;
    });

    const activeFrag = document.createDocumentFragment();
    const completedFrag = document.createDocumentFragment();
    let activeCount = 0;
    let completedCount = 0;

    for (let i = 0; i < filtered.length; i++) {
        const task = filtered[i];
        if (task.completed) {
            completedCount++;
            if (state.filter.status !== 'pending') {
                completedFrag.appendChild(createTaskCard(task));
            }
        } else {
            activeCount++;
            activeFrag.appendChild(createTaskCard(task));
        }
    }

    activeTasksList.appendChild(activeFrag);

    if (completedCount > 0 && state.filter.status !== 'pending') {
        completedGroupWrap.classList.remove('hidden');
        completedCountLabel.textContent = completedCount;
        completedTasksList.appendChild(completedFrag);
    } else {
        completedGroupWrap.classList.add('hidden');
    }

    const hasVisible = activeCount > 0 || (completedCount > 0 && state.filter.status !== 'pending');
    emptyState.classList.toggle('hidden', hasVisible);

    updateCollapsedCount();
}

function createTaskCard(task) {
    const card = document.createElement('div');
    card.className = `task-card ${task.completed ? 'completed' : ''}`;
    card.draggable = !state.settings.locked;
    card.dataset.id = task.id;

    // Drag & drop
    card.addEventListener('dragstart', (e) => {
        state.draggedTaskId = task.id;
        card.classList.add('dragging');
        e.dataTransfer.effectAllowed = 'move';
    });

    card.addEventListener('dragend', () => {
        card.classList.remove('dragging');
        state.draggedTaskId = null;
    });

    card.addEventListener('dragover', (e) => {
        e.preventDefault();
        e.dataTransfer.dropEffect = 'move';
    });

    card.addEventListener('drop', (e) => {
        e.preventDefault();
        if (state.draggedTaskId && state.draggedTaskId !== task.id) {
            const fromIndex = state.tasks.findIndex(t => t.id === state.draggedTaskId);
            const toIndex = state.tasks.findIndex(t => t.id === task.id);
            if (fromIndex !== -1 && toIndex !== -1) {
                const [movedTask] = state.tasks.splice(fromIndex, 1);
                state.tasks.splice(toIndex, 0, movedTask);
                persistState();
                renderTasks();
            }
        }
    });

    // Date / Repeat display
    let dateBadgeHtml = '';
    if (task.dueDate) {
        const due = new Date(task.dueDate + 'T00:00:00');
        const now = new Date();
        now.setHours(0,0,0,0);
        const diffDays = Math.round((due - now) / (1000 * 60 * 60 * 24));

        let label = '';
        let isOverdue = false;
        if (diffDays < 0 && !task.completed) {
            label = 'Overdue';
            isOverdue = true;
        } else if (diffDays === 0) {
            label = 'Today';
        } else if (diffDays === 1) {
            label = 'Tomorrow';
        } else {
            label = due.toLocaleDateString(undefined, { month: 'short', day: 'numeric' });
        }

        dateBadgeHtml = `
            <span class="meta-due-date ${isOverdue ? 'overdue' : ''}">
                <svg width="10" height="10" viewBox="0 0 24 24" fill="none" stroke="currentColor" stroke-width="2"><rect x="3" y="4" width="18" height="18" rx="2" ry="2"></rect><line x1="16" y1="2" x2="16" y2="6"></line><line x1="8" y1="2" x2="8" y2="6"></line><line x1="3" y1="10" x2="21" y2="10"></line></svg>
                ${label}
            </span>
        `;
    }

    let dailyBadgeHtml = '';
    if (task.isDaily) {
        dailyBadgeHtml = `
            <span class="meta-daily-badge" title="Repeats daily">
                <svg width="10" height="10" viewBox="0 0 24 24" fill="none" stroke="currentColor" stroke-width="2.5"><polyline points="23 4 23 10 17 10"></polyline><path d="M20.49 15a9 9 0 1 1-2.12-9.36L23 10"></path></svg>
                Daily
            </span>
        `;
    }

    card.innerHTML = `
        <div class="task-checkbox-wrap">
            <input type="checkbox" class="task-checkbox" ${task.completed ? 'checked' : ''} aria-label="Complete task">
            <div class="custom-checkmark">
                <svg width="11" height="11" viewBox="0 0 24 24" fill="none" stroke="currentColor" stroke-width="3.5" stroke-linecap="round" stroke-linejoin="round"><polyline points="20 6 9 17 4 12"></polyline></svg>
            </div>
        </div>
        <div class="task-main">
            <span class="task-title">${escapeHtml(task.title)}</span>
            <div class="task-meta">
                <span class="meta-category">${task.category}</span>
                ${dateBadgeHtml}
                ${dailyBadgeHtml}
            </div>
        </div>
        <div class="task-actions">
            <button class="task-action-btn btn-edit" title="Edit">
                <svg width="11" height="11" viewBox="0 0 24 24" fill="none" stroke="currentColor" stroke-width="2"><path d="M11 4H4a2 2 0 0 0-2 2v14a2 2 0 0 0 2 2h14a2 2 0 0 0 2-2v-7"></path><path d="M18.5 2.5a2.121 2.121 0 0 1 3 3L12 15l-4 1 1-4 9.5-9.5z"></path></svg>
            </button>
            <button class="task-action-btn btn-delete" title="Delete">
                <svg width="11" height="11" viewBox="0 0 24 24" fill="none" stroke="currentColor" stroke-width="2"><polyline points="3 6 5 6 21 6"></polyline><path d="M19 6v14a2 2 0 0 1-2 2H7a2 2 0 0 1-2-2V6m3 0V4a2 2 0 0 1 2-2h4a2 2 0 0 1 2 2v2"></path></svg>
            </button>
        </div>
    `;

    // Checkbox toggle
    card.querySelector('.task-checkbox').addEventListener('change', () => {
        toggleTaskCompletion(task.id);
    });

    // Double-click or edit button
    card.querySelector('.task-title').addEventListener('dblclick', () => openEditModal(task));
    card.querySelector('.btn-edit').addEventListener('click', () => openEditModal(task));
    card.querySelector('.btn-delete').addEventListener('click', () => deleteTask(task.id));

    return card;
}

function escapeHtml(str) {
    return str.replace(/&/g, '&amp;').replace(/</g, '&lt;').replace(/>/g, '&gt;').replace(/"/g, '&quot;');
}

// ==========================================================================
// Task Operations
// ==========================================================================
function addTask(title, category, dueDate, isDaily) {
    if (!title.trim()) return;

    const newTask = {
        id: 'task-' + Date.now(),
        title: title.trim(),
        completed: false,
        category: category === 'Work' ? 'Work' : 'Personal',
        dueDate: dueDate || '',
        isDaily: !!isDaily,
        createdAt: Date.now()
    };

    state.tasks.unshift(newTask);
    persistState();
    renderTasks();

    taskInputTitle.value = '';
    taskInputTitle.focus();
}

function toggleTaskCompletion(taskId) {
    const task = state.tasks.find(t => t.id === taskId);
    if (!task) return;

    task.completed = !task.completed;
    persistState();
    renderTasks();
}

function deleteTask(taskId) {
    const idx = state.tasks.findIndex(t => t.id === taskId);
    if (idx === -1) return;

    state.lastDeletedTask = {
        task: state.tasks[idx],
        index: idx
    };

    state.tasks.splice(idx, 1);
    persistState();
    renderTasks();

    showToast('Task removed', true);
}

function undoDelete() {
    if (!state.lastDeletedTask) return;
    const { task, index } = state.lastDeletedTask;
    state.tasks.splice(Math.min(index, state.tasks.length), 0, task);
    state.lastDeletedTask = null;
    toastBar.classList.add('hidden');
    persistState();
    renderTasks();
}

function showToast(msg, canUndo = false) {
    toastMessage.textContent = msg;
    btnToastUndo.classList.toggle('hidden', !canUndo);
    toastBar.classList.remove('hidden');

    if (state.toastTimer) clearTimeout(state.toastTimer);
    state.toastTimer = setTimeout(() => {
        toastBar.classList.add('hidden');
        state.lastDeletedTask = null;
        if (typeof window.gc === 'function') {
            try { window.gc(); } catch (e) {}
        }
    }, 4000);
}

function openEditModal(task) {
    state.editingTaskId = task.id;
    editTaskTitle.value = task.title;
    editTaskCategory.value = task.category;
    editTaskDate.value = task.dueDate || '';
    editTaskDaily.checked = !!task.isDaily;
    taskEditModal.classList.remove('hidden');
    editTaskTitle.focus();
}

function saveEditedTask() {
    if (!state.editingTaskId) return;
    const task = state.tasks.find(t => t.id === state.editingTaskId);
    if (task && editTaskTitle.value.trim()) {
        task.title = editTaskTitle.value.trim();
        task.category = editTaskCategory.value;
        task.dueDate = editTaskDate.value;
        task.isDaily = editTaskDaily.checked;
        persistState();
        renderTasks();
    }
    taskEditModal.classList.add('hidden');
    state.editingTaskId = null;
}

// ==========================================================================
// Event Listeners
// ==========================================================================

// Add task submit
addTaskForm.addEventListener('submit', (e) => {
    e.preventDefault();
    addTask(taskInputTitle.value, state.newCategory, state.newDueDate, state.newIsDaily);
});

// Category toggle between Personal and Work
btnToggleCategory.addEventListener('click', () => {
    state.newCategory = state.newCategory === 'Personal' ? 'Work' : 'Personal';
    categoryLabel.textContent = state.newCategory;
});

// Category Filter Tabs (All, Personal, Work)
categoryBar.querySelectorAll('.cat-pill').forEach(btn => {
    btn.addEventListener('click', () => {
        categoryBar.querySelectorAll('.cat-pill').forEach(b => b.classList.remove('active'));
        btn.classList.add('active');
        state.filter.category = btn.dataset.cat;
        renderTasks();
    });
});

// Google Tasks Style Date Picker Popover
btnDueDatePicker.addEventListener('click', (e) => {
    e.stopPropagation();
    datePopover.classList.toggle('hidden');
});

// Popover quick buttons
datePopover.querySelectorAll('.quick-date-btn').forEach(btn => {
    btn.addEventListener('click', () => {
        const now = new Date();
        if (btn.dataset.date === 'tomorrow') {
            now.setDate(now.getDate() + 1);
        }
        const yyyy = now.getFullYear();
        const mm = String(now.getMonth() + 1).padStart(2, '0');
        const dd = String(now.getDate()).padStart(2, '0');
        popoverNativeDate.value = `${yyyy}-${mm}-${dd}`;
    });
});

btnClearDate.addEventListener('click', () => {
    popoverNativeDate.value = '';
    popoverCheckDaily.checked = false;
    state.newDueDate = '';
    state.newIsDaily = false;
    dueDatePickerLabel.textContent = 'Date';
    datePopover.classList.add('hidden');
});

btnApplyDate.addEventListener('click', () => {
    state.newDueDate = popoverNativeDate.value;
    state.newIsDaily = popoverCheckDaily.checked;

    if (state.newIsDaily && !state.newDueDate) {
        dueDatePickerLabel.textContent = 'Daily';
    } else if (state.newDueDate) {
        const d = new Date(state.newDueDate + 'T00:00:00');
        const today = new Date();
        today.setHours(0,0,0,0);
        const diff = Math.round((d - today) / (1000 * 60 * 60 * 24));
        let text = diff === 0 ? 'Today' : (diff === 1 ? 'Tomorrow' : d.toLocaleDateString(undefined, { month: 'short', day: 'numeric' }));
        if (state.newIsDaily) text += ' · Daily';
        dueDatePickerLabel.textContent = text;
    } else {
        dueDatePickerLabel.textContent = 'Date';
    }

    datePopover.classList.add('hidden');
});

// Close popover on click outside
document.addEventListener('click', (e) => {
    if (!datePopover.contains(e.target) && e.target !== btnDueDatePicker && !btnDueDatePicker.contains(e.target)) {
        datePopover.classList.add('hidden');
    }
});

// Collapse to Tasks Only (Requirement 11)
btnMiniMode.addEventListener('click', () => {
    applyCollapseMode(true);
    persistState();
});

btnUncollapse.addEventListener('click', () => {
    applyCollapseMode(false);
    persistState();
});

// Header actions
btnLockPos.addEventListener('click', () => {
    state.settings.locked = !state.settings.locked;
    applyLockState();
    persistState();
});

btnSearchToggle.addEventListener('click', () => {
    searchBar.classList.toggle('hidden');
    if (!searchBar.classList.contains('hidden')) {
        searchInput.focus();
    } else {
        state.filter.search = '';
        searchInput.value = '';
        renderTasks();
    }
});

searchInput.addEventListener('input', () => {
    state.filter.search = searchInput.value;
    btnSearchClear.classList.toggle('hidden', !searchInput.value);
    renderTasks();
});

btnSearchClear.addEventListener('click', () => {
    searchInput.value = '';
    state.filter.search = '';
    btnSearchClear.classList.add('hidden');
    renderTasks();
    searchInput.focus();
});

btnSettings.addEventListener('click', () => {
    settingsDrawer.classList.remove('hidden');
    setTimeout(() => {
        applyThemeColor(state.settings.themeColor || '#000000', true, false);
    }, 40);
});

btnCloseSettings.addEventListener('click', () => {
    settingsDrawer.classList.add('hidden');
    if (typeof window.gc === 'function') {
        try { window.gc(); } catch (e) {}
    }
});

// Status filter buttons
statusFilterBtns.forEach(btn => {
    btn.addEventListener('click', () => {
        statusFilterBtns.forEach(b => b.classList.remove('active'));
        btn.classList.add('active');
        state.filter.status = btn.dataset.filter;
        renderTasks();
    });
});

// Sort select change event
if (sortSelect) {
    sortSelect.value = state.filter.sortBy;
    sortSelect.addEventListener('change', () => {
        state.filter.sortBy = sortSelect.value;
        try {
            localStorage.setItem('todo_widget_sort', state.filter.sortBy);
        } catch (e) {}
        renderTasks();
    });
}

// Completed section toggle
if (toggleCompletedBtn) {
    toggleCompletedBtn.addEventListener('click', () => {
        toggleCompletedBtn.classList.toggle('collapsed');
    });
}

// Settings events
toggleAutostart.addEventListener('change', async () => {
    if (window.widgetAPI && window.widgetAPI.setAutoStart) {
        const enabled = await window.widgetAPI.setAutoStart(toggleAutostart.checked);
        state.settings.autoStart = enabled;
        persistState();
    }
});

btnResetPos.addEventListener('click', async () => {
    if (window.widgetAPI && window.widgetAPI.resetPosition) {
        await window.widgetAPI.resetPosition();
        showToast('Position reset');
    }
});

if (btnPinTopLeft) {
    btnPinTopLeft.addEventListener('click', async () => {
        if (window.widgetAPI && window.widgetAPI.pinToCorner) {
            await window.widgetAPI.pinToCorner('top-left');
            showToast('Pinned to Top-Left');
        }
    });
}

if (btnPinTopRight) {
    btnPinTopRight.addEventListener('click', async () => {
        if (window.widgetAPI && window.widgetAPI.pinToCorner) {
            await window.widgetAPI.pinToCorner('top-right');
            showToast('Pinned to Top-Right');
        }
    });
}

// Edit modal actions
btnCloseEdit.addEventListener('click', () => taskEditModal.classList.add('hidden'));
btnCancelEdit.addEventListener('click', () => taskEditModal.classList.add('hidden'));
btnSaveEdit.addEventListener('click', saveEditedTask);
editTaskTitle.addEventListener('keydown', (e) => {
    if (e.key === 'Enter') saveEditedTask();
});

// Undo
btnToastUndo.addEventListener('click', undoDelete);

// Keyboard Shortcuts
document.addEventListener('keydown', (e) => {
    if (e.key === 'Escape') {
        settingsDrawer.classList.add('hidden');
        taskEditModal.classList.add('hidden');
        searchBar.classList.add('hidden');
        datePopover.classList.add('hidden');
    }
    if ((e.ctrlKey || e.metaKey) && e.key.toLowerCase() === 'f') {
        e.preventDefault();
        btnSearchToggle.click();
    }
    if ((e.ctrlKey || e.metaKey) && e.key.toLowerCase() === 'n') {
        e.preventDefault();
        taskInputTitle.focus();
    }
});

// Run
initWidget();
