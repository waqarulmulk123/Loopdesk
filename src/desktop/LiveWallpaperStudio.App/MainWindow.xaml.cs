using LiveWallpaperStudio.App.Pages;
using LiveWallpaperStudio.App.Services;
using Microsoft.UI.Dispatching;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Navigation;

namespace LiveWallpaperStudio.App;

public sealed partial class MainWindow : Window
{
    private readonly DispatcherQueueTimer _rendererStatusTimer;

    public string AppVersion => $"Loopdesk {GetType().Assembly.GetName().Version?.ToString(3) ?? "1.0.0"}";

    public MainWindow()
    {
        InitializeComponent();
        _rendererStatusTimer = DispatcherQueue.CreateTimer();
        _rendererStatusTimer.Interval = TimeSpan.FromSeconds(2);
        _rendererStatusTimer.Tick += (_, _) => UpdateRendererStatus();
        Activated += (_, _) => UpdateRendererStatus();
        Closed += (_, _) => _rendererStatusTimer.Stop();
        _rendererStatusTimer.Start();
    }

    private void UpdateRendererStatus()
    {
        bool isRunning = RenderSupervisor.Instance.IsRunning;
        RendererStatusText.Text = isRunning ? "Renderer service • Ready" : "Renderer service • Not running";
        RendererStatusDot.Fill = new Microsoft.UI.Xaml.Media.SolidColorBrush(
            Windows.UI.Color.FromArgb(255, isRunning ? (byte)59 : (byte)229, isRunning ? (byte)234 : (byte)163, isRunning ? (byte)160 : (byte)61));
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
        NavView.SelectedItem = NavView.MenuItems
            .OfType<NavigationViewItem>()
            .FirstOrDefault(item => string.Equals(item.Tag as string, "Home", StringComparison.Ordinal));
    }

    private void NavView_SelectionChanged(NavigationView sender, NavigationViewSelectionChangedEventArgs args)
    {
        if (args.SelectedItem is not NavigationViewItem item) return;
        Type? page = item.Tag switch
        {
            "Onboarding" => typeof(OnboardingPage),
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

    private void ContentFrame_Navigated(object sender, NavigationEventArgs e)
    {
        NavView.SelectedItem = NavView.MenuItems.OfType<NavigationViewItem>()
            .FirstOrDefault(item => string.Equals(item.Tag as string,
                e.SourcePageType.Name.Replace("Page", string.Empty), StringComparison.Ordinal));
        BreadcrumbPage.Text = e.SourcePageType.Name.Replace("Page", string.Empty) switch
        {
            "Home" => "Home", "Discover" => "Discover", "Library" => "Library",
            "Favourites" => "Favourites", "Downloads" => "Downloads", "Displays" => "Displays",
            "Settings" => "Settings", "Diagnostics" => "Diagnostics", "About" => "About",
            "Onboarding" => "Onboarding", var page => page,
        };
    }
}
