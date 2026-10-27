using System.Collections.ObjectModel;
using CommunityToolkit.Mvvm.ComponentModel;
using LiveWallpaperStudio.App.Services;

namespace LiveWallpaperStudio.App.ViewModels;

public sealed partial class HomeViewModel : ObservableObject
{
    private readonly OnlineCatalogService _catalog = new();

    public HomeViewModel()
    {
        Greeting = GreetingFor(DateTime.Now.Hour);
        _ = RefreshAsync();
    }

    [ObservableProperty]
    private string _greeting = string.Empty;

    [ObservableProperty]
    private string _featuredTitle = "Explore the catalog.";

    [ObservableProperty]
    private string _featuredDescription = "Browse the latest wallpapers available for your desktop.";

    [ObservableProperty]
    private Uri? _featuredPosterUri;

    public ObservableCollection<HomeCatalogItem> Items { get; } = new();

    private async Task RefreshAsync()
    {
        try
        {
            IReadOnlyList<OnlineCatalogItem> catalog = await _catalog.LoadAsync(LocalSettingsService.ProductionApiBaseUrl);
            foreach (OnlineCatalogItem item in catalog.Take(4))
            {
                string category = item.Categories?.FirstOrDefault() ?? "Uncategorized";
                string resolution = item.Height is > 0 ? $"{item.Height}p" : "Resolution unavailable";
                Items.Add(new HomeCatalogItem(item.Title, $"{category} · {resolution}", item.Premium ? "PREMIUM" : "FREE", OnlineCatalogService.PosterFor(item)));
            }

            OnlineCatalogItem? featured = catalog.FirstOrDefault(item => item.Featured) ?? catalog.FirstOrDefault();
            if (featured is not null)
            {
                FeaturedTitle = featured.Title;
                FeaturedDescription = string.IsNullOrWhiteSpace(featured.Description)
                    ? "A curated motion wallpaper from the Loopdesk catalog."
                    : featured.Description;
                string poster = OnlineCatalogService.PosterFor(featured);
                FeaturedPosterUri = string.IsNullOrEmpty(poster) ? null : new Uri(poster);
            }
        }
        catch
        {
            // Home stays usable as a route to Discover when the catalog is offline.
        }
    }

    private static string GreetingFor(int hour) => hour switch
    {
        >= 5 and < 12 => "Good morning.",
        >= 12 and < 17 => "Good afternoon.",
        >= 17 and < 22 => "Good evening.",
        _ => "Good night.",
    };

}

public sealed record HomeCatalogItem(string Title, string Subtitle, string Availability, string PosterUri);
