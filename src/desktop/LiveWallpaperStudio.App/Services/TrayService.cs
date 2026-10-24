using System.Runtime.InteropServices;

namespace LiveWallpaperStudio.App.Services;

/// <summary>
/// System-tray icon via Win32 Shell_NotifyIcon (M3/M5). The icon is
/// added when the app starts and removed on exit. It does not hide
/// the app, fake a wallpaper, or require elevation. The WinUI window
/// owns the callback window handle; this service only owns the icon.
/// If tray registration fails, the app still runs normally.
/// </summary>
public sealed class TrayService : IDisposable
{
    private const uint NIM_ADD = 0x00000000;
    private const uint NIM_DELETE = 0x00000002;
    private const uint NIF_MESSAGE = 0x00000001;
    private const uint NIF_ICON = 0x00000002;
    private const uint NIF_TIP = 0x00000004;

    [StructLayout(LayoutKind.Sequential, CharSet = CharSet.Unicode)]
    private struct NotifyIconData
    {
        public uint cbSize;
        public IntPtr hWnd;
        public uint uID;
        public uint uFlags;
        public uint uCallbackMessage;
        public IntPtr hIcon;
        [MarshalAs(UnmanagedType.ByValTStr, SizeConst = 128)]
        public string szTip;
    }

    [DllImport("shell32.dll", CharSet = CharSet.Unicode)]
    private static extern bool Shell_NotifyIcon(uint dwMessage, ref NotifyIconData lpData);

    private readonly IntPtr _hwnd;
    private bool _added;

    public TrayService(IntPtr hwnd)
    {
        _hwnd = hwnd;
    }

    public bool Add()
    {
        var data = new NotifyIconData
        {
            cbSize = (uint)Marshal.SizeOf<NotifyIconData>(),
            hWnd = _hwnd,
            uID = 1,
            uFlags = NIF_MESSAGE | NIF_ICON | NIF_TIP,
            uCallbackMessage = 0x0400 + 1,
            hIcon = IntPtr.Zero,
            szTip = "Live Wallpaper Studio",
        };
        _added = Shell_NotifyIcon(NIM_ADD, ref data);
        return _added;
    }

    public void Dispose()
    {
        if (!_added) return;
        var data = new NotifyIconData
        {
            cbSize = (uint)Marshal.SizeOf<NotifyIconData>(),
            hWnd = _hwnd,
            uID = 1,
        };
        Shell_NotifyIcon(NIM_DELETE, ref data);
        _added = false;
    }
}
