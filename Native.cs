using System;
using System.Runtime.InteropServices;

namespace InkOverlay;

internal static class Native
{
    public const int GWL_EXSTYLE = -20;
    public const int WS_EX_TRANSPARENT = 0x00000020;
    public const int WS_EX_TOOLWINDOW = 0x00000080;

    public static readonly IntPtr HWND_TOPMOST = new(-1);
    public const uint SWP_NOACTIVATE = 0x0010;

    public const int WM_HOTKEY = 0x0312;
    public const uint MOD_NOREPEAT = 0x4000;

    [DllImport("user32.dll", SetLastError = true)]
    static extern int GetWindowLong(IntPtr hWnd, int nIndex);

    [DllImport("user32.dll", SetLastError = true)]
    static extern int SetWindowLong(IntPtr hWnd, int nIndex, int dwNewLong);

    [DllImport("user32.dll", SetLastError = true)]
    [return: MarshalAs(UnmanagedType.Bool)]
    public static extern bool SetWindowPos(IntPtr hWnd, IntPtr hWndInsertAfter, int x, int y, int cx, int cy, uint uFlags);

    [DllImport("user32.dll", SetLastError = true)]
    [return: MarshalAs(UnmanagedType.Bool)]
    public static extern bool RegisterHotKey(IntPtr hWnd, int id, uint fsModifiers, uint vk);

    [DllImport("user32.dll", SetLastError = true)]
    [return: MarshalAs(UnmanagedType.Bool)]
    public static extern bool UnregisterHotKey(IntPtr hWnd, int id);

    public static void SetExStyle(IntPtr hwnd, int flag, bool on)
    {
        int style = GetWindowLong(hwnd, GWL_EXSTYLE);
        int updated = on ? style | flag : style & ~flag;
        if (updated != style) SetWindowLong(hwnd, GWL_EXSTYLE, updated);
    }

    const uint ABM_GETSTATE = 0x00000004;
    const uint ABM_GETTASKBARPOS = 0x00000005;
    const int ABS_AUTOHIDE = 0x1;
    const uint ABE_LEFT = 0, ABE_TOP = 1, ABE_RIGHT = 2, ABE_BOTTOM = 3;

    [StructLayout(LayoutKind.Sequential)]
    struct RECT { public int Left, Top, Right, Bottom; }

    [StructLayout(LayoutKind.Sequential)]
    struct APPBARDATA
    {
        public int cbSize;
        public IntPtr hWnd;
        public uint uCallbackMessage;
        public uint uEdge;
        public RECT rc;
        public IntPtr lParam;
    }

    [DllImport("shell32.dll")]
    static extern IntPtr SHAppBarMessage(uint dwMessage, ref APPBARDATA pData);

    /// <summary>
    /// When the taskbar auto-hides, shrinks a monitor rectangle by one pixel on the taskbar's edge.
    /// A topmost window covering the whole monitor counts as fullscreen, and Windows then keeps an
    /// auto-hide taskbar from showing; the free edge row also lets the mouse reach it.
    /// Applied to every monitor, since taskbars on other displays share the edge and the setting
    /// (ABM_GETAUTOHIDEBAREX is unreliable per monitor on Windows 11).
    /// </summary>
    public static System.Drawing.Rectangle InsetForAutoHideTaskbars(System.Drawing.Rectangle monitor)
    {
        var data = new APPBARDATA { cbSize = Marshal.SizeOf<APPBARDATA>() };
        if (((int)SHAppBarMessage(ABM_GETSTATE, ref data) & ABS_AUTOHIDE) == 0) return monitor;
        if (SHAppBarMessage(ABM_GETTASKBARPOS, ref data) == IntPtr.Zero) data.uEdge = ABE_BOTTOM;

        int left = monitor.Left, top = monitor.Top, right = monitor.Right, bottom = monitor.Bottom;
        switch (data.uEdge)
        {
            case ABE_LEFT: left++; break;
            case ABE_TOP: top++; break;
            case ABE_RIGHT: right--; break;
            default: bottom--; break;
        }
        return System.Drawing.Rectangle.FromLTRB(left, top, right, bottom);
    }
}
