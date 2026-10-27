using LiveWallpaperStudio.App.Services;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;

namespace LiveWallpaperStudio.App.Pages;

public sealed partial class OnboardingPage : Page
{
    public OnboardingPage()
    {
        InitializeComponent();
    }

    private void StartButton_Click(object sender, RoutedEventArgs e)
    {
        LocalSettingsService.Update(settings => settings.HasCompletedOnboarding = true);
        Frame.Navigate(typeof(HomePage));
    }
}
