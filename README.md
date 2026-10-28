# Desktop To-Do Widget 📌

A lightweight, zero-latency desktop To-Do widget built specifically for Windows 11. Engineered with a pure pitch-black aesthetic, ClearType font rendering, magnetic flush edge/corner snapping, and desktop homescreen persistence that stays on your wallpaper **no matter what**.

---

## 🌟 Key Features

### 1. Persistent Desktop Homescreen Pinning
- **Native Desktop Layer**: Sits natively on your desktop wallpaper plane (`alwaysOnTop: false`) like an authentic desktop gadget.
- **Immune to `Win + D` (Show Desktop)**: Minimization commands are intercepted and restored across Windows DWM animation cycles, ensuring the widget never disappears when toggling the desktop.
- **No Taskbar Clutter**: Runs with `skipTaskbar: true` to prevent cluttering your taskbar.
- **Position Lock**: Click **🔒 Lock** in the header to prevent accidental window movement while interacting.
- **Position Memory**: Remembers its exact screen coordinates across reboots.

### 2. Flush Magnetic Edge & Corner Snapping
- **Flush Snapping (0px Offset)**: Dragging near the screen boundary magnetically snaps the widget flush against the display edge without awkward gaps.
- **4-Corner Snapping**: Dragging to the Top-Left, Top-Right, Bottom-Left, or Bottom-Right corner pins the widget directly into place.
- **One-Click Pinning**: Right-click the system tray icon or open Settings to snap to **Top-Left** or **Top-Right** with a single click.

### 3. Pure Black Minimalist Aesthetics
- **Pure Black Palette**: `#000000` pitch-black background with deep `#0a0a0a` cards and crisp `#1e1e1e` borders.
- **Zero Blur / No Translucency**: Solid, high-contrast theme eliminates text fuzziness and visual clutter.
- **ClearType Subpixel Rendering**: High-DPI font optimizations ensure razor-sharp typography on 125% and 150% Windows scaling.

### 4. Task Management & Productivity
- **Quick-Add Bar**: Add tasks with a single keystroke.
- **Personal & Work Categories**: Focused categorization with `All`, `Personal`, and `Work` filter pills.
- **Google Tasks-Style Date Popover**: Quick scheduling with **Today**, **Tomorrow**, native date picker, and **Show daily** recurrence.
- **Sorting Options**: Sort by `Date asc`, `Date dsc`, `a-z`, or `z-a`.
- **Collapsible Mini Mode**: Collapse the widget into a compact view displaying remaining pending tasks.
- **Undo Toast**: 5-second undo toast for accidental task deletion.

### 5. Quick Summon Hotkeys & System Tray
- **Global Hotkeys**: Press <kbd>F9</kbd> or <kbd>Ctrl</kbd> + <kbd>Alt</kbd> + <kbd>T</kbd> anywhere in Windows to temporarily bring the widget in front of active windows, and press again to return it to the desktop.
- **System Tray Menu**: Right-click for quick actions: lock position, pin to corners, toggle mini mode, or quit.
- **Windows Startup**: Automatically starts when Windows boots.

---

## 🚀 Installation & Running

### Method 1: Ultra-Lightweight Native Windows Edition (~2 MB) — Recommended ⭐

No Electron, no Node.js required! Uses the Windows built-in Microsoft Edge engine:
- **Total Disk Footprint:** **~2.2 MB** *(99.3% smaller than standard 300MB apps)*
- **Memory (RAM) Usage:** **~30–45 MB** *(vs ~180MB)*
- **1-Click Install:** Double-click `install-native.bat` to install shortcuts to your Desktop, Start Menu, and Windows Startup.
- **Portable Zip:** Extract `Desktop-To-Do-Widget-Native.zip` and run `DesktopToDoWidget.exe` directly from any folder.
- **Build from Source (Instant):** Run `powershell -ExecutionPolicy Bypass -File .\build_native.ps1` to recompile the native C# executable in ~2 seconds.

---

### Method 2: Download Electron Installers (.exe)

Choose either prebuilt installer from [GitHub Releases](https://github.com/P-Sushanth/to-do_widget/releases) or the `dist/` folder:

1. **Setup Installer** (`Desktop To-Do Widget Setup 1.0.0.exe`):
   - Double-click to install like any standard Windows application.
   - Automatically creates **Desktop** and **Start Menu** shortcuts.
   - Includes full Windows Add/Remove Programs uninstallation support.

2. **Portable Executable** (`Desktop-To-Do-Widget-Portable.exe`):
   - Single standalone `.exe`.
   - No installation needed — just double-click anywhere and it runs instantly.

---

### Method 3: Electron Developer Setup (Node.js)

#### Prerequisites
- **Windows 10 or Windows 11** (64-bit)
- **Node.js** (v18.0.0 or later) & **npm** — [Download Node.js](https://nodejs.org/)

#### Step-by-Step Developer Setup
1. **Clone the Repository**:
   ```powershell
   git clone https://github.com/P-Sushanth/to-do_widget.git
   cd to-do_widget
   ```
2. **Install Dependencies**:
   ```powershell
   npm install
   ```
3. **Run Locally**:
   ```powershell
   npm start
   ```
4. **Build Your Own `.exe` Installers**:
   ```powershell
   npm run build
   ```
   The compiled `.exe` files will be generated in the `dist/` directory.

5. **1-Click Script Installation (Optional Dev Shortcuts)**:
   - Double-click `install.bat` or run:
     ```powershell
     powershell -ExecutionPolicy Bypass -File .\install.ps1
     ```

---

## 🖥️ Running the Widget

Once installed, you can launch the widget using any of these methods:

1. **Desktop Shortcut**: Double-click the **To-Do Widget** shortcut on your Desktop.
2. **Silent Background Launch**: Double-click `launch-widget.vbs` or `start-widget.bat` (runs without leaving an open terminal window).
3. **Developer / Terminal Mode**:
   ```powershell
   npm start
   ```

---

## ⌨️ Keyboard Shortcuts

| Shortcut | Action |
| :--- | :--- |
| <kbd>F9</kbd> or <kbd>Ctrl</kbd> + <kbd>Alt</kbd> + <kbd>T</kbd> | Global summon / elevate widget over active windows |
| <kbd>Ctrl</kbd> + <kbd>Shift</kbd> + <kbd>T</kbd> | Alternative global summon hotkey |
| <kbd>Ctrl</kbd> + <kbd>N</kbd> | Focus task input field |
| <kbd>Ctrl</kbd> + <kbd>F</kbd> | Toggle task search bar |
| <kbd>Enter</kbd> | Submit new task or save edited task |
| <kbd>Escape</kbd> | Close popovers, search, or settings drawer |

---

## 💾 Data Storage

All your tasks and settings are persisted locally on your computer in an encrypted/safe JSON format at:
```text
%APPDATA%\windows11-todo-widget\todo-widget-data.json
```
Your data never leaves your device and does not require an internet connection.

---

## 🧹 Uninstallation

To remove the widget shortcuts and startup entry:
1. Double-click `uninstall.bat` (or manually delete the shortcut from `shell:startup`).
2. Delete the project folder if you no longer need it.

---

## 📖 Technical Architecture & Developer Notes

For deep-dive architectural specifications, IPC contracts, memory caching, and Windows 11 DWM handling, refer to [`ARCHITECTURE_AND_SPECS.md`](ARCHITECTURE_AND_SPECS.md).
