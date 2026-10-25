# Desktop To-Do Widget: Architecture, Design Specs & Developer Guide

> **Notice for Future AI Models & Sessions**:  
> Read this document thoroughly before proposing or making changes. It documents the exact architecture, user requirements, established design patterns, and critical Windows 11/Electron gotchas for this repository.

---

## 1. Project Overview & Philosophy

**Desktop To-Do Widget** is a lightweight, zero-latency desktop widget built with **Electron (v45.0.0)**, pure **HTML/CSS**, and **Vanilla JavaScript**. It is designed specifically for Windows 11 homescreen productivity.

### Core Philosophy
1. **Always on Homescreen**: The widget lives natively on the Windows desktop wallpaper plane (`alwaysOnTop: false`), immune to being hidden or minimized by <kbd>Win</kbd> + <kbd>D</kbd> (*Show Desktop*).
2. **Pure Black Aesthetics**: Pure pitch-black (`#000000`) background, deep `#0a0a0a` task cards, `#1e1e1e` borders, and high-contrast typography. **No blur, no acrylic/mica translucency, and no frosted glass**.
3. **Crisp Rendering**: ClearType and subpixel text rendering optimizations prevent blurriness on Windows high-DPI displays.
4. **Magnetic Flush Snapping**: Windows-style edge and corner snapping that pins cleanly flush (`0px` offset) to the work area boundaries.
5. **Simplicity First**: Minimalist task management with strictly two categories (`Personal` and `Work`), 4 sorting modes, and Google Tasks-style date recurrence.

---

## 2. Inviolable Design Rules & User Preferences

When modifying this codebase, **strictly preserve** the following user requirements:

| Feature | Requirement / Constraint | Why |
| :--- | :--- | :--- |
| **Color Palette** | Pure black (`#000000`) background, deep `#0a0a0a` cards, `#1e1e1e` borders. | User explicitly removed all acrylic/mica/frosted glass effects. |
| **Transparency** | Removed entirely. Opacity is locked to 1.0. | Translucency caused text fuzziness and visual clutter over busy wallpapers. |
| **Font Rendering** | Segment/subpixel anti-aliasing via Segoe UI Variable and ClearType flags. | Eliminates text blurring on Windows 11 scaling factors (125%, 150%). |
| **Categories** | Strictly **`Personal`** and **`Work`** only (+ `All` filter tab). | User rejected complex tagging, priority tags, and custom categories. |
| **Priorities** | Removed completely. No urgent/high/low priority flags or color dots. | Keeps tasks focused purely on content, date, and completion. |
| **Date Picker** | Google Tasks-style popover with quick buttons (*Today*, *Tomorrow*), native date input, and **"Show daily"** recurrence checkbox. | Streamlined, familiar date scheduling. |
| **Sorting** | Exactly 4 options in the sort dropdown: `Date asc`, `Date dsc`, `a-z`, `z-a`. | Clean, predictable sorting. |
| **Window State** | `minimizable: false`, `alwaysOnTop: false` by default, `skipTaskbar: true`. | Sits permanently on desktop homescreen like a true desktop fixture. |
| **Edge Snapping** | Snaps **flush (`0px`)** to screen edges and corners when moved within 45px. | Eliminates awkward gaps when user wants the widget in a corner. |

---

## 3. Architecture & File Layout

```
TO_DO_Widget/
├── assets/
│   ├── icon.ico             # Windows native shortcut icon
│   ├── icon.png             # Window icon (PNG)
│   ├── icon_64.png          # Start menu & tray icon
│   └── tray.png             # System tray icon
├── src/
│   ├── index.html           # Widget DOM structure
│   ├── renderer.js          # Client UI logic & instant localStorage state
│   └── styles.css           # Pure black theme & drag region styles
├── main.js                  # Electron main process (lifecycle, IPC, snapping)
├── preload.js               # Context-isolated IPC bridge (`window.widgetAPI`)
├── package.json             # Electron v45.0.0 dependency & scripts
├── install.ps1              # Native shortcut installer (Desktop, Start, Startup)
├── install.bat              # 1-click wrapper for install.ps1
├── launch-widget.vbs        # Silent background launcher (no CMD prompt popup)
├── start-widget.bat         # Direct binary startup script
├── uninstall.bat            # Clean uninstaller for shortcuts and startup entries
├── widget_runtime.log       # Persistent main process event & diagnostic log
└── ARCHITECTURE_AND_SPECS.md# This architectural reference document
```

### Data Storage & Paths
- **User Data File**: `%APPDATA%\windows11-todo-widget\todo-widget-data.json`
- **Instant Client Cache**: Browser `localStorage` (`todo_widget_tasks`, `todo_widget_settings`, `todo_widget_sort`) ensures zero-latency synchronous boot before IPC completes.
- **Diagnostics Log**: `widget_runtime.log` (in workspace root).

---

## 4. Key Subsystems & Implementations

### 4.1. Homescreen Persistence (<kbd>Win</kbd> + <kbd>D</kbd> Protection)
In Windows 11, pressing <kbd>Win</kbd> + <kbd>D</kbd> minimizes all top-level application windows. To make the widget permanently present on the desktop homescreen:
1. `minimizable: false` is configured on `BrowserWindow`.
2. `mainWindow.on('minimize', ...)` intercepts the event, cancels default minimization, and executes a multi-stage restore cycle (`10ms`, `50ms`, `150ms`, `300ms`, `500ms`) to counter Windows Explorer's asynchronous desktop toggle animation.
3. `mainWindow.on('hide', ...)` automatically restores visibility if an external call attempts to hide the window.
4. Single "Collapse to Tasks Only" button collapses the widget to mini mode instead of redundant duplicate buttons.

### 4.2. Magnetic Edge & Corner Pinning
Located in `main.js` under `snapBounds(bounds)`:
- Work area is calculated via `screen.getDisplayMatching(bounds).workArea` (respects taskbar height/position).
- Snap threshold is **45px**.
- When dragged within 45px of any boundary:
  - **Left**: `newX = waX` (`0`)
  - **Top**: `newY = waY` (`0`)
  - **Right**: `newX = waX + waW - width`
  - **Bottom**: `newY = waY + waH - height`
- Both axes evaluate independently, enabling automatic flush corner pinning for Top-Left `(0, 0)`, Top-Right, Bottom-Left, and Bottom-Right.
- Debounced by `100ms` on `moved` and `resized` to ensure native mouse dragging remains buttery smooth.

### 4.3. Pure Desktop Wallpaper Layer & Quick Summon Hotkeys
The widget sits strictly on the desktop wallpaper plane (`alwaysOnTop: false`):
- All normal applications (browsers, editors, file manager) naturally sit on top of the widget.
- When focused or summoned via <kbd>F9</kbd> / <kbd>Ctrl</kbd> + <kbd>Alt</kbd> + <kbd>T</kbd>, it focuses to receive input and immediately ensures `alwaysOnTop: false` (with blur-based auto-lowering).
- Clicking any other window or pressing Alt+Tab blurs the widget, instantly ensuring it stays beneath active apps on the wallpaper.
- Launching the Desktop shortcut while the app is already running activates the widget without forcing permanent top-most elevation.

---

## 5. Critical Windows 11 & Electron Gotchas

### ⚠️ Gotcha 1: DirectComposition Transparent Void Bug
- **Bug**: On Windows 11, setting `transparent: false` or removing `backgroundColor: '#00000000'` on frameless windows can cause Chromium's DirectComposition compositor to render an unresponsive black void or artifacted margins.
- **Rule**: ALWAYS keep `transparent: true` and `backgroundColor: '#00000000'` in `new BrowserWindow({...})`. The solid pitch-black `#000000` background is provided by `#widget-container` in [styles.css](file:///c:/Users/popur/Documents/Projects/TO_DO_Widget/src/styles.css).

### ⚠️ Gotcha 2: Drag Regions Detaching Clicks
- **Bug**: Any child element inside a `-webkit-app-region: drag` container will NOT register mouse clicks or keyboard focus unless explicitly protected.
- **Rule**: Every button, input, dropdown, or interactive control inside the header or drag bars MUST have the `.no-drag` class (`-webkit-app-region: no-drag !important;`).

### ⚠️ Gotcha 3: Bounds Clamping at Screen Edges
- **Bug**: Clamping window bounds with `if (x < 20) x = defaultX` prevents users from placing windows flush at `(0, 0)`.
- **Rule**: `0` and small positive coordinates are completely valid snapped positions. Only reset coordinates if the window is completely offscreen outside the display work area (`x < -100 || x > screenWidth - 50`).

---

## 6. Development & Operational Commands

### Start Widget (Clean Native Binary)
```powershell
# Fast background launch
Start-Process "node_modules\electron\dist\electron.exe" -ArgumentList "." -WorkingDirectory (Get-Location)
```

### Inspect Processes & Windows
```powershell
Get-Process electron -ErrorAction SilentlyContinue | Select-Object Id, ProcessName
```

### Tail Diagnostic Runtime Log
```powershell
Get-Content .\widget_runtime.log -Tail 20
```

### Inspect Stored User Data
```powershell
Get-Content "$env:APPDATA\windows11-todo-widget\todo-widget-data.json"
```

### Reinstall Shortcuts (Desktop, Start Menu, Startup)
```powershell
powershell -ExecutionPolicy Bypass -File .\install.ps1
```

### Stop Running Widget
```powershell
Stop-Process -Name electron -Force -ErrorAction SilentlyContinue
```
