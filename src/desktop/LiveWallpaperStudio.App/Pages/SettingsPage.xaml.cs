using LiveWallpaperStudio.App.ViewModels;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;

namespace LiveWallpaperStudio.App.Pages;

public sealed partial class SettingsPage : Page
{
    private bool _loadingStartup;
    public SettingsViewModel ViewModel { get; } = new();

    public SettingsPage()
    {
        InitializeComponent();
        Loaded += SettingsPage_Loaded;
    }

    private async void SettingsPage_Loaded(object sender, RoutedEventArgs e)
    {
        _loadingStartup = true;
        await ViewModel.RefreshStartupAsync();
        StartupToggle.IsOn = ViewModel.IsStartupEnabled;
        _loadingStartup = false;
    }

    private async void StartupToggle_Toggled(object sender, RoutedEventArgs e)
    {
        if (_loadingStartup) return;
        await ViewModel.SetStartupEnabledAsync(StartupToggle.IsOn);
        _loadingStartup = true;
        StartupToggle.IsOn = ViewModel.IsStartupEnabled;
        _loadingStartup = false;
    }
}
