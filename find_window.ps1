Add-Type @'
using System;
using System.Text;
using System.Runtime.InteropServices;
public struct RECT { public int Left, Top, Right, Bottom; }
public class WinInspector {
    public delegate bool EnumWindowsProc(IntPtr hWnd, IntPtr lParam);
    [DllImport("user32.dll")]
    public static extern bool EnumWindows(EnumWindowsProc lpEnumFunc, IntPtr lParam);
    [DllImport("user32.dll")]
    public static extern uint GetWindowThreadProcessId(IntPtr hWnd, out uint lpdwProcessId);
    [DllImport("user32.dll")]
    public static extern bool IsWindowVisible(IntPtr hWnd);
    [DllImport("user32.dll")]
    public static extern bool GetWindowRect(IntPtr hWnd, out RECT lpRect);
    [DllImport("user32.dll")]
    public static extern int GetWindowLong(IntPtr hWnd, int nIndex);
    public const int GWL_EXSTYLE = -20;
    public const int WS_EX_TOPMOST = 0x00000008;
}
'@

$pids = (Get-Process electron -ErrorAction SilentlyContinue).Id
Write-Host "PIDs: $($pids -join ', ')"

[WinInspector]::EnumWindows({
    param([IntPtr]$hwnd, [IntPtr]$lparam)
    [uint32]$procId = 0
    [WinInspector]::GetWindowThreadProcessId($hwnd, [ref]$procId) | Out-Null
    if ($pids -contains [int]$procId) {
        $vis = [WinInspector]::IsWindowVisible($hwnd)
        $rect = New-Object RECT
        [WinInspector]::GetWindowRect($hwnd, [ref]$rect) | Out-Null
        $w = $rect.Right - $rect.Left
        $h = $rect.Bottom - $rect.Top
        $exStyle = [WinInspector]::GetWindowLong($hwnd, [WinInspector]::GWL_EXSTYLE)
        $isTopmost = ($exStyle -band [WinInspector]::WS_EX_TOPMOST) -ne 0
        Write-Host "HWND: $hwnd PID: $procId Visible: $vis Topmost: $isTopmost Bounds: [x=$($rect.Left), y=$($rect.Top), w=$w, h=$h]"
    }
    return $true
}, [IntPtr]::Zero) | Out-Null
