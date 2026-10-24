using LiveWallpaperStudio.App.Pages;
using LiveWallpaperStudio.App.Services;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;

namespace LiveWallpaperStudio.App;

public sealed partial class MainWindow : Window
{
    public MainWindow()
    {
        InitializeComponent();
    }

    private void NavView_Loaded(object sender, RoutedEventArgs e)
    {
        // Complete flow, first run: onboarding first, then Home.
        // Returning users go straight to Home.
        if (!LocalSettingsService.Load().HasCompletedOnboarding)
        {
            ContentFrame.Navigate(typeof(OnboardingPage));
            return;
        }
        ContentFrame.Navigate(typeof(HomePage));
        NavView.SelectedItem = NavView.MenuItems[0];
    }

    private void NavView_SelectionChanged(NavigationView sender, NavigationViewSelectionChangedEventArgs args)
    {
        if (args.SelectedItem is not NavigationViewItem item) return;
        Type? page = item.Tag switch
        {
            "Home" => typeof(HomePage),
            "Library" => typeof(LibraryPage),
            "Discover" => typeof(DiscoverPage),
            "Favourites" => typeof(FavouritesPage),
            "Downloads" => typeof(DownloadsPage),
            "Displays" => typeof(DisplaysPage),
            "Settings" => typeof(SettingsPage),
            "Diagnostics" => typeof(DiagnosticsPage),
            "About" => typeof(AboutPage),
            _ => null,
        };
        if (page is not null && ContentFrame.CurrentSourcePageType != page)
        {
            ContentFrame.Navigate(page);
        }
    }
}
