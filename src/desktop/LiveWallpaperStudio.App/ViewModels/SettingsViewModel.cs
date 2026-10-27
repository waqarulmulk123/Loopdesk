using CommunityToolkit.Mvvm.ComponentModel;
using LiveWallpaperStudio.App.Services;
using Windows.ApplicationModel;

namespace LiveWallpaperStudio.App.ViewModels;

public sealed partial class SettingsViewModel : ObservableObject
{
    [ObservableProperty] private bool _isStartupEnabled;
    [ObservableProperty] private string _startupStatus = "Checking Windows startup preference…";

    public async Task RefreshStartupAsync() => ApplyStartupState(await StartupTaskService.GetStateAsync());

    public async Task SetStartupEnabledAsync(bool enabled) => ApplyStartupState(await StartupTaskService.SetEnabledAsync(enabled));

    private void ApplyStartupState(StartupTaskState? state)
    {
        IsStartupEnabled = state == StartupTaskState.Enabled;
        StartupStatus = state switch
        {
            StartupTaskState.Enabled => "Loopdesk will restore your applied wallpaper after you sign in.",
            StartupTaskState.DisabledByUser => "Windows has disabled startup. Re-enable Loopdesk in Task Manager’s Startup apps page.",
            StartupTaskState.DisabledByPolicy => "Startup is disabled by this device’s policy.",
            StartupTaskState.Disabled => "Enable this to restore your applied wallpaper after you sign in.",
            _ => "Startup is available only in the installed MSIX app.",
        };
    }
}
