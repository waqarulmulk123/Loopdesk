using System.Runtime.InteropServices;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using LiveWallpaperStudio.App.Services;

namespace LiveWallpaperStudio.App.ViewModels;

/// <summary>
/// Home status and display information. It deliberately has no local media
/// import or file-system discovery surface.
/// </summary>
public sealed partial class MainViewModel : ObservableObject
{
    public MainViewModel()
    {
        var settings = LocalSettingsService.Load();
        Muted = settings.Muted;
        PauseOnFullscreen = settings.PauseOnFullscreen;
        PauseOnLock = settings.PauseOnLock;
        PauseOnDisplayOff = settings.PauseOnDisplayOff;
        PauseOnBatterySaver = settings.PauseOnBatterySaver;
        SendUsageAnalytics = settings.SendUsageAnalytics;
        SendCrashReports = settings.SendCrashReports;
        RefreshDisplays();
    }

    [ObservableProperty]
    private string _status = "Offline mode. Online catalog is not configured.";

    [ObservableProperty]
    private string _displaySummary = "Displays: unknown";

    [ObservableProperty]
    private bool _muted = true;

    [ObservableProperty]
    private bool _pauseOnFullscreen = true;

    [ObservableProperty]
    private bool _pauseOnLock = true;

    [ObservableProperty]
    private bool _pauseOnDisplayOff = true;

    [ObservableProperty]
    private bool _pauseOnBatterySaver = true;

    [ObservableProperty]
    private bool _sendUsageAnalytics;

    [ObservableProperty]
    private bool _sendCrashReports;

    [RelayCommand]
    private void SaveSettings()
    {
        var settings = new LocalSettingsService
        {
            Muted = Muted,
            PauseOnFullscreen = PauseOnFullscreen,
            PauseOnLock = PauseOnLock,
            PauseOnDisplayOff = PauseOnDisplayOff,
            PauseOnBatterySaver = PauseOnBatterySaver,
            SendUsageAnalytics = SendUsageAnalytics,
            SendCrashReports = SendCrashReports,
        };
        settings.Save();
        Status = "Settings saved locally.";
    }

    [RelayCommand]
    private void RefreshDisplays()
    {
        int monitors = GetSystemMetrics(80); // SM_CMONITORS
        int primaryWidth = GetSystemMetrics(0); // SM_CXSCREEN
        int primaryHeight = GetSystemMetrics(1); // SM_CYSCREEN
        DisplaySummary = monitors > 0
            ? $"Displays: {monitors} monitor(s) detected; primary {primaryWidth}x{primaryHeight}."
            : "Displays: could not read monitor count from Win32.";
    }

    [DllImport("user32.dll")]
    private static extern int GetSystemMetrics(int nIndex);
}
