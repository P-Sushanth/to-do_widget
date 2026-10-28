# Context Log & AI Knowledge Base 🧠

> **Purpose of this file**: This document contains the full architectural history, decisions, code evolution, technical gotchas, and current state of the **Desktop To-Do Widget** repository. Future AI agents should read this file first to obtain complete project context with minimal token usage.

---

## 📌 Project Overview & Core Requirements

- **Repository**: `https://github.com/P-Sushanth/to-do_widget`
- **Application**: A sleek, pitch-black desktop To-Do widget for Windows 10 & 11.
- **Core Requirements**:
  1. **Wallpaper Plane Pinning**: Lives natively on the desktop background layer (`HWND_BOTTOM`, `alwaysOnTop: false`), allowing other productivity apps to glide over it.
  2. **Immunity to `Win + D` (*Show Desktop*)**: Intercepts minimization commands (`WM_SYSCOMMAND` + `SC_MINIMIZE`) so the widget never vanishes when toggling desktop.
  3. **0px Magnetic Flush Snapping**: Automatically snaps flush against monitor boundaries (top, bottom, left, right) and 4 corners with 0px offset and a 45px threshold.
  4. **8-Directional Real-Time Resizing**: Resizes from any of the 4 borders and 4 corners even though it is a frameless, borderless window.
  5. **Full-Spectrum Color Studio**: Settings drawer with 2D Saturation/Value canvas box, 360° rainbow hue slider, shade/lightness slider, eyedropper, hex input, presets, and dynamic contrast calculation for buttons and accents.
  6. **Google Tasks Scheduling**: Quick scheduling with Today, Tomorrow, specific dates, and "Show daily" recurrence.
  7. **Global Summon**: Press <kbd>F9</kbd> or <kbd>Ctrl</kbd> + <kbd>Alt</kbd> + <kbd>T</kbd> anywhere in Windows to elevate above active windows.

---

## 🏛️ Architecture Evolution & Editions

The repository contains two operational editions, plus the original legacy prototype:

```
┌─────────────────────────────────────────────────────────────────────────────────────────┐
│                                  EDITION COMPARISON                                     │
├───────────────────────┬──────────────────────────┬──────────────────────────────────────┤
│ Metric                │ Native WebView2 Edition  │ Pure Native WinForms Edition         │
├───────────────────────┼──────────────────────────┼──────────────────────────────────────┤
│ Status                │ Production (Default)     │ Fully Functional Standalone Prototype│
│ Primary Executable    │ DesktopToDoWidget.exe    │ DesktopToDoWidget-PureNative.exe     │
│ Working Set RAM       │ ~25–35 MB (Optimized)    │ ~1.6–2.2 MB (Settles to < 1 MB)      │
│ Child Processes       │ 4–5 (Chromium subsystem) │ 0 (Single isolated Windows process)  │
│ Standalone Binary Size│ 145 KB (Exe only)        │ 146 KB (Single standalone Exe!)      │
│ Total Package Size    │ ~2.28 MB (with web assets│ 146 KB (Zero assets/DLLs needed!)    │
│ Setup Installer       │ Desktop-To-Do-Widget-    │ Single portable .exe                 │
│                       │ Setup.exe (1.67 MB)      │                                      │
│ Technology            │ C# WinForms + WebView2   │ 100% C# WinForms + GDI+ Double Buffer│
│ External Dependencies │ Edge WebView2 Runtime    │ None (Uses Windows built-in .NET 4.x)│
│ Compiler Required     │ Built-in csc.exe (~2s)   │ Built-in csc.exe (~1s)               │
└───────────────────────┴──────────────────────────┴──────────────────────────────────────┘
```

---

## 📜 Chronological Evolution & Changes

### Phase 1: Electron Baseline (Initial State)
- Built with Electron 45 + Node.js.
- **Problem**:
  - The final NSIS installer was ~122 MB, extracted to over 300 MB of Chromium binaries on disk.
  - Idle RAM usage hovered between 180 MB and 220 MB across 4 separate node/browser processes.
  - User requested: *"Why does the app take 300 MB for a simple to-do widget, can't we reduce it? I don't want electron setup."*

### Phase 2: Ultra-Lightweight Native Windows Edition (WebView2 Host)
- Replaced Electron with a native C# host in `src-native/Program.cs` that embeds Microsoft Edge WebView2.
- Compiled via Windows built-in `C:\Windows\Microsoft.NET\Framework64\v4.0.30319\csc.exe` in ~2 seconds with zero SDK installation.
- Disk footprint dropped from 300 MB to **~2.2 MB (99.3% reduction)**. Standalone binary is only 145 KB.
- Added 1-click batch installer (`install-native.bat` & `install-native.ps1`) and uninstaller (`uninstall-native.bat`).

### Phase 3: Frameless Window Edge & Corner Resizing
- **Problem**: Setting `FormBorderStyle = FormBorderStyle.None` disabled Windows standard resize borders. The widget could not be resized by dragging borders.
- **Solution**:
  1. In `src-native/Program.cs`: Added `WS_THICKFRAME` (0x00040000) to `CreateParams`, and handled `WM_NCCALCSIZE` to return `IntPtr.Zero` (suppresses default titlebar/borders while keeping resizing active).
  2. In `src/index.html` & `src/styles.css`: Added 8 perimeter resize handles (`.resize-handle[data-edge="..."]`) along top, bottom, left, right, and 4 corners, plus a visual diagonal resize grip at the bottom-right.
  3. In `src/renderer.js` & C# IPC: Added `mousedown` listener on resize handles that calls `window.widgetAPI.startResize(edge)`, forwarding to Win32 `SendMessage(Handle, WM_NCLBUTTONDOWN, htCode, 0)`.

### Phase 4: Full-Spectrum Color Studio & Theming
- Built a custom Color Studio in `src/index.html`, `src/styles.css`, and `src/renderer.js`:
  - 2D Saturation/Value touch/mouse box.
  - 360° rainbow hue slider.
  - Dedicated shade/lightness slider.
  - Hex code input + System Eyedropper API (`EyeDropper`).
  - 10 curated dark presets and 1-click Default Pure Pitch Black (`#000000`) reset.
  - Dynamic button/checkbox/tab adaptation: When background color changes, secondary and tertiary shades are dynamically calculated for buttons, checkboxes, and active pills to guarantee contrast.

### Phase 5: CI/CD Pipeline & 1-Click Standalone Installer
- User insight: *"Most of the users want to directly install the exe file and be done with it, most don't care about cloning the repo. Focus more on that, include a badge for installing the exe file in the readme."*
- Created `src-native/Installer.cs`: A C# standalone installer compiled with embedded payload zip. Outputs `Desktop-To-Do-Widget-Setup.exe` (~1.67 MB).
  - Automatically kills existing instances, installs to `%LOCALAPPDATA%\Programs\DesktopToDoWidget`, creates Desktop and Start Menu shortcuts, registers Windows startup, and launches the widget live on wallpaper.
- Updated `.github/workflows/build.yml` to compile native executable, package `Desktop-To-Do-Widget-Setup.exe` and `Desktop-To-Do-Widget-Native.zip`, and publish releases automatically.
- Added large green `Download_Installer-.exe` badge to `README.md` and streamlined instructions to a 1-click flow.

### Phase 6: Low-Memory Tuning & Ponytail Audit
- Performed a deep Ponytail audit for over-engineering and profiled live memory:
  - Discovered C# host was ~52 MB working set, and WebView2 was spawning 4 child processes (coordinator, renderer, GPU, crashpad).
- **C# Host Optimizations**:
  - Implemented `EmptyWorkingSet(Process.GetCurrentProcess().Handle)` in Win32 `psapi.dll`. Drops host working set from **52.06 MB to 1.64 MB**!
  - Added `memTrimTimer` (ticks every 45s) and `TrimMemory()` called whenever widget loses focus or is sent to wallpaper.
  - Added `webView.CoreWebView2.MemoryUsageTargetLevel = CoreWebView2MemoryUsageTargetLevel.Low`.
  - Added low-memory Chromium browser arguments:
    `--disable-features=AudioServiceOutOfProcess,CalculateNativeWinOcclusion,SpareRendererForSitePerProcess,BackForwardCache --renderer-process-limit=1 --disable-background-networking --disable-sync --disable-extensions --disable-default-apps --disable-breakpad --disable-crash-reporter --disable-gpu-shader-disk-cache --js-flags="--max-old-space-size=16 --lite-mode --optimize-for-size --expose-gc"`
- **JavaScript Optimizations (`src/renderer.js`)**:
  - Replaced chained `.filter().filter()` calls in `renderTasks()` with `DocumentFragment` single-pass iteration, eliminating intermediate array allocations and batching DOM reflows.
  - Plugged task memory leak: Cleared `state.lastDeletedTask = null` on toast timeout.
  - Triggered `window.gc()` when settings drawer closes or toast finishes.

### Phase 7: Pure Native WinForms Prototype (0% Chromium, ~2 MB RAM)
- Created `src-pure-native/Program.cs` and `build_pure_native.ps1`.
- Built a 100% native WinForms app using double-buffered GDI+ (`SmoothingMode.AntiAlias` + `ClearTypeGridFit`).
- **Results**:
  - Working set: **~2.2 MB** (settles to < 1 MB on standby).
  - Child processes: **0** (Zero Chromium, zero WebView2).
  - Single standalone binary: **146 KB** (`dist-pure-native/DesktopToDoWidget-PureNative.exe`).
  - Full feature parity: Tasks, custom checkboxes, strike-through, category tabs (`All`, `Personal`, `Work`), search bar, Google Tasks daily recurrence, collapsible completed section, undo toast, settings drawer, custom color dialog, 0px magnetic snapping, 8-directional edge/corner resizing, <kbd>F9</kbd> global hotkey summon, and system tray.
  - 100% shared data file interoperability with `%APPDATA%\windows11-todo-widget\todo-widget-data.json`.

---

## 📁 Repository Directory Map

```text
c:\Users\popur\Documents\Projects\TO_DO_Widget\
├── .github/
│   └── workflows/
│       └── build.yml               # GitHub Actions CI/CD (Native exe + Setup + Electron builds)
├── assets/
│   ├── icon.ico                    # Windows application icon
│   ├── icon.png                    # High-res icon (512x512)
│   ├── icon_64.png                 # 64x64 icon
│   └── tray.png                    # System tray icon
├── src/                            # Web frontend assets (Used by WebView2 Edition)
│   ├── index.html                  # Widget HTML structure & resize handles
│   ├── renderer.js                 # Frontend business logic, state, drag-drop, color studio
│   └── styles.css                  # Pure vanilla CSS design system, dark mode, animations
├── src-native/                     # Native WebView2 Edition (C# Host)
│   ├── Program.cs                  # Win32 DWM, WebView2 host, snapping, resizing, IPC bridge
│   └── Installer.cs                # Self-extracting standalone 1-click setup installer
├── src-pure-native/                # Pure Native Windows Edition (Zero Chromium)
│   └── Program.cs                  # Complete custom-drawn GDI+ WinForms widget (~2 MB RAM)
├── build_native.ps1                # Builds WebView2 exe (dist-native/) + Setup.exe (1.67 MB)
├── build_pure_native.ps1           # Builds pure native exe (dist-pure-native/, 146 KB)
├── install-native.bat              # 1-click batch launcher for native installation
├── install-native.ps1              # PowerShell installer logic (shortcuts, autostart)
├── uninstall-native.bat            # 1-click batch launcher for native uninstallation
├── uninstall-native.ps1            # Removes shortcuts, registry autostart, and local files
├── package.json                    # Project metadata & fallback Electron build scripts
├── README.md                       # User-facing documentation with installer badge
├── ARCHITECTURE_AND_SPECS.md       # Technical specs & IPC documentation
└── context_log.md                  # THIS FILE (Comprehensive AI context dump)
```

---

## 💾 Local Storage Schema

Both the **WebView2 Edition** and the **Pure Native Edition** read and write to the same single JSON file:

**File Path**:
```text
%APPDATA%\windows11-todo-widget\todo-widget-data.json
```

**JSON Schema**:
```json
{
  "tasks": [
    {
      "id": "task-1792854908391",
      "title": "Study Computer Networks",
      "completed": false,
      "category": "Work",
      "dueDate": "2026-10-29",
      "isDaily": false,
      "createdAt": 1792854908391
    }
  ],
  "categories": ["Personal", "Work"],
  "settings": {
    "locked": false,
    "opacity": 1.0,
    "miniMode": false,
    "autoStart": true,
    "themeColor": "#000000",
    "bounds": {
      "x": 1570,
      "y": 40,
      "width": 350,
      "height": 560
    }
  }
}
```

---

## ⚠️ Gotchas, Edge Cases & Practical Engineering Lessons

1. **`write_to_file` Tool Constraint**:
   - Passing `ArtifactMetadata` to `write_to_file` when modifying non-artifact project files causes an `invalid_args` error (`path is not a valid artifact path; artifacts must be in <appDataDir>\brain\...`). For repository workspace files, **omit `ArtifactMetadata`**.

2. **Windows Detached Process Spawning**:
   - In PowerShell, `Start-Process` child processes are terminated when the parent tool step completes.
   - To launch persistent desktop background processes, use `Invoke-CimMethod`:
     ```powershell
     Invoke-CimMethod -ClassName Win32_Process -MethodName Create -Arguments @{CommandLine = "`"$exePath`""}
     ```

3. **C# 5 Compatibility for Built-in `csc.exe`**:
   - `C:\Windows\Microsoft.NET\Framework64\v4.0.30319\csc.exe` is the default .NET 4.x compiler installed on every Windows machine without external SDKs.
   - It targets **C# 5**. Avoid C# 6+ features:
     - ❌ `public string id { get; set; } = "";` (Use constructor initialization instead).
     - ❌ `$"Hello {name}"` (Use `string.Format` or `"Hello " + name`).
     - ❌ `if (x is ArrayList list)` (Use `var list = x as ArrayList; if (list != null)`).

4. **PowerShell CommandLine Character Escaping**:
   - In `powershell -Command "..."`, `$_.` is consumed by the shell interpreter unless escaped as `` `$_ ``.
   - For multi-line PowerShell logic, write to a `.ps1` script file and execute with `-ExecutionPolicy Bypass -File .\script.ps1`.

5. **Frameless Window Resizing Architecture**:
   - `FormBorderStyle = FormBorderStyle.None` disables native Windows resize borders.
   - Solution requires:
     1. Adding `WS_THICKFRAME` (0x00040000) to `CreateParams`.
     2. Returning `IntPtr.Zero` in `WM_NCCALCSIZE` to suppress default titlebar and frame.
     3. Forwarding edge clicks via `SendMessage(Handle, WM_NCLBUTTONDOWN, (IntPtr)htCode, IntPtr.Zero)`.

6. **Process Working Set Trimming**:
   - .NET garbage collection frees managed memory, but Windows keeps pages in the process Working Set.
   - Calling `EmptyWorkingSet(Process.GetCurrentProcess().Handle)` from `psapi.dll` flushes unreferenced pages, dropping WinForms memory from ~50 MB to **~1.6–3 MB** instantly.

---

## 🚀 How to Build & Run Any Edition

### 1. Build Native WebView2 Edition (~2 MB, Default)
```powershell
powershell -ExecutionPolicy Bypass -File .\build_native.ps1
```
- Outputs:
  - `dist-native\DesktopToDoWidget.exe` (145 KB binary)
  - `Desktop-To-Do-Widget-Setup.exe` (1.67 MB standalone 1-click installer)
  - `Desktop-To-Do-Widget-Native.zip` (1.56 MB portable zip)

### 2. Build Pure Native WinForms Edition (~2 MB RAM, Zero Chromium)
```powershell
powershell -ExecutionPolicy Bypass -File .\build_pure_native.ps1
```
- Outputs:
  - `dist-pure-native\DesktopToDoWidget-PureNative.exe` (146 KB single standalone binary)

### 3. Build Legacy Electron Edition (Optional)
```powershell
npm install
npm run build
```
- Outputs in `dist/`: NSIS Setup installer (~122 MB) and Portable binary (~111 MB).
