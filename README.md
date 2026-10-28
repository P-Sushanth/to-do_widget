# Desktop To-Do Widget 📌

[![Windows 11](https://img.shields.io/badge/Windows-11%20%7C%2010-0078D4?logo=windows&logoColor=white)](https://github.com/P-Sushanth/to-do_widget)
[![Size](https://img.shields.io/badge/Size-~2.2%20MB-brightgreen)](#-installation-guide)
[![RAM](https://img.shields.io/badge/RAM-~35%20MB-success)](#-key-features)
[![No Electron](https://img.shields.io/badge/Runtime-Native%20WebView2-blue)](#method-1-ultra-lightweight-native-windows-edition-2-mb--recommended-)

A sleek, ultra-lightweight desktop To-Do widget built specifically for Windows 10 & 11. Designed to live permanently on your desktop wallpaper like a real gadget, with **flush magnetic snapping**, **real-time edge/corner resizing**, a **full-spectrum color picker**, and **zero-latency persistence** that stays on your screen even when you press <kbd>Win</kbd> + <kbd>D</kbd> (*Show Desktop*).

Now available in an **Ultra-Lightweight Native Windows Edition (~2 MB)** powered by the Windows built-in Microsoft Edge engine — eliminating the 300 MB bloat of traditional Electron apps while cutting RAM usage by 75%!

---

## 🌟 Key Features

### 1. 🪟 True Desktop Homescreen Persistence
- **Native Wallpaper Plane**: Sits on the desktop background layer (`alwaysOnTop: false`). Normal windows naturally sit over it when you work.
- **Immune to `Win + D`**: Minimization calls from Windows are intercepted and restored, ensuring the widget never disappears when toggling the desktop.
- **No Taskbar Clutter**: Runs without cluttering your taskbar.
- **Position Lock (🔒)**: Click the lock icon in the header to freeze the widget in place and avoid accidental movement or resizing.
- **Position & Size Memory**: Automatically remembers its exact screen position and custom dimensions across reboots.

### 2. 🧲 Magnetic Flush Edge & Corner Snapping
- **0px Flush Snapping**: Dragging near any screen boundary automatically snaps the widget flush (0px offset) against the display edge without gaps.
- **4-Corner Snap**: Dragging near any corner cleanly pins the widget into place.
- **One-Click Pinning**: Open the Settings drawer or right-click the system tray icon to snap to **Top-Left** or **Top-Right** with one click.

### 3. 📐 Real-Time Edge & Corner Resizing
- **Every Edge & Corner**: Drag from the **left**, **right**, **top**, **bottom**, or any of the **4 corners** to resize the widget to your liking.
- **Visual Corner Grip**: A subtle diagonal grip indicator at the bottom-right corner provides an intuitive handle for diagonal resizing.
- **Minimum Size Safeguard**: Enforces a minimum size of `280 × 220` so content always remains clear and readable.

### 4. 🎨 Full-Spectrum Color & Theme Studio
- **Full Spectrum Color Studio**: Accessible directly via Settings (`⚙`).
  - **2D Canvas Box**: Interactive saturation and brightness grid.
  - **360° Hue Slider**: Full rainbow spectrum to choose any hue.
  - **Shade / Lightness Slider**: Fine-tune depth from deep pitch black to bright tints.
  - **Hex Input**: Type or paste any hex code (`#ff4500`, `#3b82f6`, etc.).
  - **System Eyedropper**: Sample colors directly from your wallpaper or screen.
  - **10 Curated Presets**: Pitch Black, Obsidian Navy, Deep Plum, Forest Emerald, Velvet Wine, Cyberpunk Teal, Espresso, Titanium Slate, Midnight Indigo, and Steel Blue.
  - **Default Button**: One click to restore the original Pure Pitch Black (`#000000`).
- **Dynamic Button & Accent Shades**: Selecting a color doesn't just change the background — all buttons (`+` Add, "Done", "Save", checkboxes), active category tabs, and status filter pills dynamically adopt matching shades of that color with high contrast!

### 5. ⚡ Task Management & Google Tasks Date Recurrence
- **Quick-Add**: Type your task title and hit <kbd>Enter</kbd>.
- **Strictly Two Categories**: `Personal` and `Work` tabs (+ `All` view).
- **Google Tasks-Style Date Popover**: Quick scheduling with **Today**, **Tomorrow**, native date input, and **Show daily** recurrence checkbox.
- **4 Predictable Sort Modes**: `Date asc`, `Date dsc`, `a-z`, `z-a`.
- **Collapsible Mini Mode**: Collapse the widget into a compact view showing only pending tasks.
- **Undo Toast**: 5-second popup banner to instantly restore accidentally deleted tasks.

### 6. ⌨️ Global Hotkeys & System Tray
- Press <kbd>F9</kbd> or <kbd>Ctrl</kbd> + <kbd>Alt</kbd> + <kbd>T</kbd> anywhere in Windows to summon the widget in front of other apps, and press again to send it back to the wallpaper.
- System Tray icon for quick actions: Lock position, mini mode, pin to corners, reset position, or exit.

---

## 🚀 Installation Guide

Choose the installation method that best fits your workflow:

```
┌─────────────────────────────────────────────────────────────────────────────┐
│ Recommended: Native Windows WebView2 Edition                                │
│  ✔ ~2.2 MB total disk size (99.3% smaller than standard 300MB apps)         │
│  ✔ ~35–45 MB RAM usage (vs ~180MB)                                          │
│  ✔ Zero dependencies: Uses Windows built-in Microsoft Edge WebView2 engine  │
│  ✔ No Node.js or npm required!                                              │
└─────────────────────────────────────────────────────────────────────────────┘
```

---

### Option 1: 1-Click Installer (Native Windows Edition) — Recommended ⭐

1. Clone or download this repository:
   ```powershell
   git clone https://github.com/P-Sushanth/to-do_widget.git
   ```
2. Open the folder in File Explorer.
3. Double-click **`install-native.bat`** (or right-click `install-native.ps1` and select *Run with PowerShell*).
4. The installer will:
   - Install the widget to `%LOCALAPPDATA%\Programs\DesktopToDoWidget\`
   - Create a **Desktop shortcut** (`To-Do Widget`)
   - Create a **Start Menu shortcut** (`To-Do Widget`)
   - Register the widget in **Windows Startup** so it opens when you log in
   - Launch the widget immediately on your desktop homescreen!

---

### Option 2: Portable No-Install Zip (Native Windows Edition)

If you don't want to install anything and just want a folder you can run from a USB drive or your Documents folder:

1. Download or locate **`Desktop-To-Do-Widget-Native.zip`** (**1.56 MB**).
2. Extract the zip file anywhere on your computer.
3. Double-click **`DesktopToDoWidget.exe`** to run!
4. *(Optional)* Right-click `DesktopToDoWidget.exe` and select **Pin to Start** or **Show more options > Send to > Desktop (create shortcut)**.

---

### Option 3: Compile the Native Executable from Source

You can recompile the C# native binary directly on your machine in ~2 seconds using the Windows built-in compiler:

```powershell
powershell -ExecutionPolicy Bypass -File .\build_native.ps1
```

The compiled application will be generated in `dist-native\DesktopToDoWidget.exe`.

---

### Option 4: Electron Edition (For Developers using Node.js)

If you prefer running via the standard Electron toolchain:

#### Prerequisites
- **Windows 10 or 11** (64-bit)
- **Node.js** (v18 or newer) — [Download Node.js](https://nodejs.org/)

#### Steps
1. Clone the repository and install npm packages:
   ```powershell
   git clone https://github.com/P-Sushanth/to-do_widget.git
   cd to-do_widget
   npm install
   ```
2. Start the widget in dev mode:
   ```powershell
   npm start
   ```
3. To build standalone Electron `.exe` installers:
   ```powershell
   npm run build
   ```
   Outputs will be created in `dist/`:
   - `dist/Desktop To-Do Widget Setup 1.0.0.exe` (NSIS Installer)
   - `dist/Desktop-To-Do-Widget-Portable.exe` (Portable Binary)

---

## 🖥️ How to Use the Widget

### Moving & Resizing
- **Move**: Click and drag from the header bar (where the title "Tasks" is located) to move the widget. Drag near any screen edge or corner to snap flush.
- **Resize**: Hover your cursor over any border (left, right, top, bottom) or corner. The cursor will change to a resize arrow (`↔`, `↕`, `⤡`). Click and drag to adjust width and height.
- **Lock**: Click the **🔓** lock icon in the header. When locked (**🔒**), dragging and resizing are disabled to prevent accidental movement.

### Managing Tasks
- **Add Task**: Type your task title in the bottom field and press <kbd>Enter</kbd>.
- **Categories**: Click `Personal` or `Work` in the quick-add footer to tag a category before adding. Filter tasks using the `All`, `Personal`, and `Work` tabs at the top.
- **Schedule Due Dates**: Click the **📅 Date** button in the footer to choose **Today**, **Tomorrow**, a specific date, or check **Show daily** for recurring routines.
- **Edit Task**: Click any task card to edit its title, date, category, or recurrence.
- **Complete Task**: Click the checkbox next to any task. Completed tasks move to the bottom collapsible group.
- **Delete Task**: Hover over a task card and click the **🗑** trash button. An undo banner will appear for 5 seconds.

### Changing Theme Colors
1. Click the **⚙** gear icon in the header to open the Settings drawer.
2. Under **Widget Color & Theme**:
   - Click any of the **10 preset color chips** for instant dark/vibrant themes.
   - Or drag the cursor in the **2D Color Box** and use the **Hue** / **Shade** sliders to mix any color.
   - Or click the **Eyedropper** button to pick any color on your screen.
   - Click **Default** anytime to revert back to Pure Pitch Black (`#000000`).

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
