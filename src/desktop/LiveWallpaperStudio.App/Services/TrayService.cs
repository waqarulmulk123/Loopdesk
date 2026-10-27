using System.Runtime.InteropServices;

namespace LiveWallpaperStudio.App.Services;

public sealed class TrayService : IDisposable
{
    private const uint CallbackMessage = 0x8001;
    private readonly IntPtr _hwnd;
    private readonly Action _open;
    private readonly Action _stop;
    private readonly Action _resume;
    private readonly Action _exit;
    private readonly SubclassProc _callback;
    private readonly uint _taskbarCreated = RegisterWindowMessage("TaskbarCreated");
    private bool _disposed;
    public bool IsAvailable { get; private set; }

    public TrayService(IntPtr hwnd, Action open, Action stop, Action resume, Action exit)
    {
        _hwnd = hwnd;
        _open = open;
        _stop = stop;
        _resume = resume;
        _exit = exit;
        _callback = WindowProc;
        if (SetWindowSubclass(hwnd, _callback, 1, 0)) Add();
    }

    private NotifyIconData Data() => new()
    {
        cbSize = (uint)Marshal.SizeOf<NotifyIconData>(), hWnd = _hwnd, uID = 1,
        uFlags = 7, uCallbackMessage = CallbackMessage,
        hIcon = LoadIcon(IntPtr.Zero, new IntPtr(32512)),
        szTip = "Loopdesk — open or exit from here", szInfo = "", szInfoTitle = ""
    };

    private void Add()
    {
        var data = Data();
        IsAvailable = Shell_NotifyIcon(0, ref data);
    }

    private IntPtr WindowProc(IntPtr hwnd, uint message, UIntPtr wParam, IntPtr lParam, UIntPtr id, UIntPtr data)
    {
        try
        {
            if (!_disposed && message == _taskbarCreated)
            {
                Add();
                if (!IsAvailable) _open(); // Never strand a hidden window without its tray icon.
            }
            if (!_disposed && message == CallbackMessage && wParam.ToUInt64() == 1)
            {
                uint mouseMessage = unchecked((uint)lParam.ToInt64());
                if (mouseMessage == 0x0202 || mouseMessage == 0x0203) _open();
                else if (mouseMessage == 0x0205 || mouseMessage == 0x007B) ShowMenu();
                return IntPtr.Zero;
            }
            if (!_disposed && message == 0x0218 && (wParam.ToUInt64() == 0x7 || wParam.ToUInt64() == 0x12))
            {
                _resume();
            }
            if (message == 0x0082) Dispose();
        }
        catch (Exception exception)
        {
            System.Diagnostics.Debug.WriteLine($"Tray action failed: {exception}");
        }
        return DefSubclassProc(hwnd, message, wParam, lParam);
    }

    private void ShowMenu()
    {
        var menu = CreatePopupMenu();
        if (menu == IntPtr.Zero) return;
        uint command;
        try
        {
            AppendMenu(menu, 0, 1, "Open Loopdesk");
            AppendMenu(menu, 0, 2, "Stop wallpaper");
            AppendMenu(menu, 0x800, 0, null);
            AppendMenu(menu, 0, 3, "Exit Loopdesk");
            GetCursorPos(out var point);
            SetForegroundWindow(_hwnd);
            command = TrackPopupMenu(menu, 0x0102, point.X, point.Y, 0, _hwnd, IntPtr.Zero);
            PostMessage(_hwnd, 0, UIntPtr.Zero, IntPtr.Zero);
        }
        finally { DestroyMenu(menu); }
        if (command == 1) _open();
        else if (command == 2) _stop();
        else if (command == 3) _exit();
    }

    public void Dispose()
    {
        if (_disposed) return;
        _disposed = true;
        var data = Data();
        if (IsAvailable) Shell_NotifyIcon(2, ref data);
        IsAvailable = false;
        RemoveWindowSubclass(_hwnd, _callback, 1);
    }

    [StructLayout(LayoutKind.Sequential, CharSet = CharSet.Unicode)]
    private struct NotifyIconData
    {
        public uint cbSize; public IntPtr hWnd; public uint uID; public uint uFlags;
        public uint uCallbackMessage; public IntPtr hIcon;
        [MarshalAs(UnmanagedType.ByValTStr, SizeConst = 128)] public string szTip;
        public uint dwState; public uint dwStateMask;
        [MarshalAs(UnmanagedType.ByValTStr, SizeConst = 256)] public string szInfo;
        public uint uVersion;
        [MarshalAs(UnmanagedType.ByValTStr, SizeConst = 64)] public string szInfoTitle;
        public uint dwInfoFlags; public Guid guidItem; public IntPtr hBalloonIcon;
    }
    [StructLayout(LayoutKind.Sequential)]
    private struct Point { public int X; public int Y; }
    [UnmanagedFunctionPointer(CallingConvention.Winapi)]
    private delegate IntPtr SubclassProc(IntPtr hwnd, uint message, UIntPtr wParam, IntPtr lParam, UIntPtr id, UIntPtr data);
    [DllImport("shell32.dll", CharSet = CharSet.Unicode)] private static extern bool Shell_NotifyIcon(uint message, ref NotifyIconData data);
    [DllImport("comctl32.dll")] private static extern bool SetWindowSubclass(IntPtr hwnd, SubclassProc callback, UIntPtr id, UIntPtr data);
    [DllImport("comctl32.dll")] private static extern bool RemoveWindowSubclass(IntPtr hwnd, SubclassProc callback, UIntPtr id);
    [DllImport("comctl32.dll")] private static extern IntPtr DefSubclassProc(IntPtr hwnd, uint message, UIntPtr wParam, IntPtr lParam);
    [DllImport("user32.dll", CharSet = CharSet.Unicode)] private static extern uint RegisterWindowMessage(string name);
    [DllImport("user32.dll", CharSet = CharSet.Unicode)] private static extern IntPtr LoadIcon(IntPtr instance, IntPtr name);
    [DllImport("user32.dll")] private static extern IntPtr CreatePopupMenu();
    [DllImport("user32.dll", CharSet = CharSet.Unicode)] private static extern bool AppendMenu(IntPtr menu, uint flags, UIntPtr id, string? text);
    [DllImport("user32.dll")] private static extern bool DestroyMenu(IntPtr menu);
    [DllImport("user32.dll")] private static extern bool GetCursorPos(out Point point);
    [DllImport("user32.dll")] private static extern bool SetForegroundWindow(IntPtr hwnd);
    [DllImport("user32.dll")] private static extern uint TrackPopupMenu(IntPtr menu, uint flags, int x, int y, int reserved, IntPtr hwnd, IntPtr rect);
    [DllImport("user32.dll")] private static extern bool PostMessage(IntPtr hwnd, uint message, UIntPtr wParam, IntPtr lParam);
}
