using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using LiveWallpaperStudio.App.Services;

namespace LiveWallpaperStudio.App.ViewModels;

public sealed partial class SettingsViewModel : ObservableObject
{
    public SettingsViewModel()
    {
        var s = LocalSettingsService.Load();
        Muted = s.Muted;
        PauseOnFullscreen = s.PauseOnFullscreen;
        PauseOnLock = s.PauseOnLock;
        PauseOnDisplayOff = s.PauseOnDisplayOff;
        PauseOnBatterySaver = s.PauseOnBatterySaver;
        SendUsageAnalytics = s.SendUsageAnalytics;
        SendCrashReports = s.SendCrashReports;
    }

    [ObservableProperty] private bool _muted = true;
    [ObservableProperty] private bool _pauseOnFullscreen = true;
    [ObservableProperty] private bool _pauseOnLock = true;
    [ObservableProperty] private bool _pauseOnDisplayOff = true;
    [ObservableProperty] private bool _pauseOnBatterySaver = true;
    [ObservableProperty] private bool _sendUsageAnalytics;
    [ObservableProperty] private bool _sendCrashReports;
    [ObservableProperty] private string _status = string.Empty;

    [RelayCommand]
    private void Save()
    {
        var existing = LocalSettingsService.Load();
        new LocalSettingsService
        {
            Muted = Muted,
            PauseOnFullscreen = PauseOnFullscreen,
            PauseOnLock = PauseOnLock,
            PauseOnDisplayOff = PauseOnDisplayOff,
            PauseOnBatterySaver = PauseOnBatterySaver,
            SendUsageAnalytics = SendUsageAnalytics,
            SendCrashReports = SendCrashReports,
            HasCompletedOnboarding = existing.HasCompletedOnboarding,
            LowPowerMode = existing.LowPowerMode,
        }.Save();
        Status = "Settings saved locally.";
    }
}
