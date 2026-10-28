# Desktop To-Do Widget 📌

[![Download Windows Installer](https://img.shields.io/badge/Download_Installer-.exe%20(1.6%20MB)-2ea44f?style=for-the-badge&logo=windows&logoColor=white)](https://github.com/P-Sushanth/to-do_widget/releases/latest)

[![Windows 11](https://img.shields.io/badge/Windows-11%20%7C%2010-0078D4?logo=windows&logoColor=white)](https://github.com/P-Sushanth/to-do_widget)
[![Size](https://img.shields.io/badge/Size-~2.2%20MB-brightgreen)](#-key-features)
[![RAM](https://img.shields.io/badge/RAM-~35%20MB-success)](#-key-features)
[![No Electron](https://img.shields.io/badge/Runtime-Native%20WebView2-blue)](#-key-features)
[![Latest Release](https://img.shields.io/github/v/release/P-Sushanth/to-do_widget?color=orange&label=Latest%20Release)](https://github.com/P-Sushanth/to-do_widget/releases/latest)

A sleek, ultra-lightweight desktop To-Do widget built specifically for Windows 10 & 11. Designed to live permanently on your desktop wallpaper like a real gadget, with **flush magnetic snapping**, **real-time edge/corner resizing**, a **full-spectrum color picker**, and **zero-latency persistence** that stays on your screen even when you press <kbd>Win</kbd> + <kbd>D</kbd> (*Show Desktop*).

**Direct .exe installer — no repo cloning, no terminal commands, no Node.js required!** Simply download `Desktop-To-Do-Widget-Setup.exe` (~1.6 MB), double-click to install, and you're done.

---

## 🌟 Key Features

### 1. ⚡ Ultra-Small App Size (~2 MB) & Featherweight RAM (~35 MB)
- **99.3% Smaller Disk Footprint**: Traditional Electron utilities consume 250–350+ MB for simple desktop widgets. The native edition requires only **~2.2 MB total disk space**, with a single standalone executable of just **~140 KB**.
- **75% Lower RAM Consumption**: Runs at a featherweight **~35–45 MB RAM** (compared to ~180 MB across 4 separate background Chromium/Node.js helper processes in Electron).
- **Windows Native WebView2 Engine**: Leverages the Microsoft Edge WebView2 runtime already built into Windows 10 & 11. No bundled Chromium binaries, zero bloat, and lightning-fast cold boot in under 300 ms.
- **Hardware-Accelerated Win32 Interop**: DirectComposition GPU acceleration with native Windows subpixel ClearType font rendering for crisp text on high-DPI displays.

### 2. 🎨 Full-Spectrum Color & Dynamic Theme Studio
- **Unlimited Palette Freedom**: An integrated color studio in Settings (`⚙`) offering complete control over your desktop aesthetic:
  - **2D Saturation & Value Canvas Box**: Smooth 2D touch/mouse surface for pinpoint color intensity and brightness control.
  - **360° Rainbow Hue Slider**: Full chromatic sweep across every hue in the color spectrum.
  - **Dedicated Shade / Lightness Slider**: Seamlessly tune darkness from pitch black to deep shadows and vibrant tints.
  - **Hex Color Input**: Type or paste any custom hex code (`#ff4500`, `#3b82f6`, `#8b5cf6`, etc.).
  - **System Eyedropper**: Sample any pixel color directly from your desktop wallpaper or open apps.
  - **10 Curated Dark & Vibrant Presets**: Instant 1-click palettes (Pitch Black, Obsidian Navy, Deep Plum, Forest Emerald, Velvet Wine, Cyberpunk Teal, Espresso, Titanium Slate, Midnight Indigo, Steel Blue).
  - **Instant Reset**: One-click **Default** button to instantly restore Pure Pitch Black (`#000000`).
- **Dynamic Harmonized Shade Calculation**: Selecting a color doesn't just change the background — the widget automatically computes complementary and high-contrast shades of that exact hue for all buttons (`+` Add, "Done", "Save", checkboxes), active category tabs, filter pills, and hover states!

### 3. 🧲 Magnetic Flush Edge & Corner Snapping
- **True 0px Flush Screen Snapping**: Dragging near any display border (left, right, top, or bottom) magnetically snaps the widget with a **0px flush offset** directly against your monitor boundary — zero gaps, zero overhang.
- **4-Corner Snap Locking**: Pulling the widget toward any screen corner automatically locks it into the corner quadrant with a smooth 45px snap threshold.
- **One-Click Corner Pinning**: Open the Settings drawer or right-click the system tray icon to snap instantly to **Top-Left** or **Top-Right** with a single click.
- **Position Lock (🔒)**: Toggle the lock icon in the header to freeze coordinates and prevent accidental dragging or resizing while working.

### 4. 🪟 Desktop Homescreen Persistence & Real-Time Resizing
- **True Wallpaper Plane**: Lives permanently on your desktop background layer (`alwaysOnTop: false`), allowing all normal productivity windows to glide smoothly over it.
- **Immune to `Win + D` (*Show Desktop*)**: Intercepts Windows desktop toggle commands to ensure the widget never minimizes or vanishes.
- **8-Directional Edge & Corner Resizing**: Freely drag from the **left**, **right**, **top**, **bottom**, or any of the **4 corners** in real-time, backed by native Windows `WM_NCLBUTTONDOWN` hit-testing.
- **Visual Corner Grip**: Subtle diagonal grip indicator at the bottom-right corner for quick diagonal resizing.
- **No Taskbar Clutter**: Stays hidden from the taskbar, accessible anytime via the desktop or system tray.

### 5. ⚡ Task Management & Google Tasks Recurrence
- **Quick-Add**: Type task titles and press <kbd>Enter</kbd> instantly.
- **Strictly Two Categories**: Organized under `Personal` and `Work` tabs with an `All` view.
- **Google Tasks-Style Date Popover**: Quick scheduling with **Today**, **Tomorrow**, native date input, and **Show daily** recurrence checkbox.
- **4 Predictable Sort Modes**: `Date asc`, `Date dsc`, `a-z`, `z-a`.
- **Collapsible Mini Mode**: Collapse into an ultra-compact pending-only list to save desktop space.
- **5-Second Undo Toast**: Restore accidentally deleted tasks with one click.

### 6. ⌨️ Global Hotkeys & System Tray
- Press <kbd>F9</kbd> or <kbd>Ctrl</kbd> + <kbd>Alt</kbd> + <kbd>T</kbd> anywhere in Windows to summon the widget in front of active windows, and press again to return it to the wallpaper.
- System Tray icon for quick actions: Lock position, mini mode, pin to corners, reset position, or exit.

---

## 🚀 Installation Guide

```
┌─────────────────────────────────────────────────────────────────────────────┐
│ 1-Click Windows Executable (.exe)                                           │
│  ✔ Standalone Setup installer: only ~1.6 MB (no 300MB Electron bloat)       │
│  ✔ ~35–45 MB RAM usage (75% savings vs Chromium apps)                       │
│  ✔ Zero setup: No git, no npm, no command prompt required                   │
│  ✔ Automatically creates Desktop & Start Menu shortcuts                     │
└─────────────────────────────────────────────────────────────────────────────┘
```

### 📥 Option 1: Direct 1-Click Installer (.exe) — Recommended ⭐

**No repository cloning, no terminal commands, no zip extraction required.**

1. Download **[`Desktop-To-Do-Widget-Setup.exe`](https://github.com/P-Sushanth/to-do_widget/releases/latest)** (~1.6 MB).
2. Double-click the downloaded **`Desktop-To-Do-Widget-Setup.exe`**.
3. **Done!** The installer automatically:
   - Installs the widget to `%LOCALAPPDATA%\Programs\DesktopToDoWidget\`
   - Places a **Desktop shortcut** (`To-Do Widget`)
   - Places a **Start Menu shortcut** (`To-Do Widget`)
   - Configures **Windows Startup** so it automatically starts when you log in
   - Instantly launches the widget living live on your desktop wallpaper!

---

### 📦 Option 2: Portable No-Install Zip (.zip)

If you don't want to install anything and just want a folder you can run from a USB drive or your Documents folder:

1. Download **[`Desktop-To-Do-Widget-Native.zip`](https://github.com/P-Sushanth/to-do_widget/releases/latest)** (~1.5 MB).
2. Extract the zip file anywhere on your computer.
3. Double-click **`DesktopToDoWidget.exe`** to run immediately!
4. *(Optional)* Right-click `DesktopToDoWidget.exe` and select **Pin to Start** or **Show more options > Send to > Desktop (create shortcut)**.

---

### 🛠️ Option 3: Developer & Source Build (Optional)

If you want to modify or compile from source:

#### Native Windows Edition:
Recompile the native C# executable and setup installer in ~2 seconds using the Windows built-in compiler:
```powershell
powershell -ExecutionPolicy Bypass -File .\build_native.ps1
```
Outputs generated:
- `dist-native\DesktopToDoWidget.exe` (Standalone executable, ~140 KB)
- `Desktop-To-Do-Widget-Setup.exe` (1-click installer, ~1.6 MB)

#### Electron Toolchain (Legacy / Node.js):
```powershell
git clone https://github.com/P-Sushanth/to-do_widget.git
cd to-do_widget
npm install
npm start
```

---

## ⌨️ Keyboard Shortcuts

| Shortcut | Action |
| :--- | :--- |
| <kbd>F9</kbd> | Global summon / elevate widget over active windows (press again to return to wallpaper) |
| <kbd>Ctrl</kbd> + <kbd>Alt</kbd> + <kbd>T</kbd> | Alternative global summon hotkey |
| <kbd>Ctrl</kbd> + <kbd>N</kbd> | Focus task input field |
| <kbd>Ctrl</kbd> + <kbd>F</kbd> | Open / close search bar |
| <kbd>Enter</kbd> | Submit new task or save edited task |
| <kbd>Escape</kbd> | Dismiss popovers, date picker, search bar, or Settings drawer |

---

## 💾 Local Data Storage

All your tasks, categories, window coordinates, and theme preferences are saved locally on your computer at:
```text
%APPDATA%\windows11-todo-widget\todo-widget-data.json
```
- Completely private and offline: **No data ever leaves your computer**.
- Zero cloud account required.

---

## 🧹 Uninstallation Guide

### Uninstalling the Native Edition (~2 MB)
1. Double-click **`uninstall-native.bat`** in the project folder (or right-click `uninstall-native.ps1` > *Run with PowerShell*).
2. The script will:
   - Close the running widget
   - Remove Desktop and Start Menu shortcuts
   - Remove the Windows Startup registry entry
   - Delete `%LOCALAPPDATA%\Programs\DesktopToDoWidget\`

### Uninstalling the Electron Edition
- If installed via `Setup 1.0.0.exe`: Open **Windows Settings > Apps > Installed apps**, search for **Desktop To-Do Widget**, and click **Uninstall**.
- Or double-click `uninstall.bat` in the repository root.

---

## 📄 Technical Architecture

For complete architectural details, IPC message contracts, magnetic snapping algorithms, ClearType subpixel rendering flags, and Windows DWM window management, see [`ARCHITECTURE_AND_SPECS.md`](ARCHITECTURE_AND_SPECS.md).
