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
    private string _featuredPosterUri = string.Empty;

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
                Items.Add(new HomeCatalogItem(item.Title, $"{category} · {resolution}", item.Premium ? "PREMIUM" : "FREE", PosterFor(item.Id)));
            }

            OnlineCatalogItem? featured = catalog.FirstOrDefault();
            if (featured is not null)
            {
                FeaturedTitle = featured.Title;
                FeaturedDescription = string.IsNullOrWhiteSpace(featured.Description)
                    ? "A curated motion wallpaper from the Loopdesk catalog."
                    : featured.Description;
                FeaturedPosterUri = PosterFor(featured.Id);
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

    private static string PosterFor(string id)
    {
        string poster = id switch
        {
            "theme-loopdesk-aurora-dreamscape" => "media-generation-poster-aurora-mountains-0-b0747490-fea4-40da-b98a-3c4202fe903d.webp",
            "theme-loopdesk-cosmic-infinity" => "media-generation-poster-galaxy-nebula-0-bf04bf8b-9f6b-4e6a-aeab-ebed4775e506.webp",
            "theme-loopdesk-liquid-neon-flow" => "media-generation-poster-neon-cyberpunk-city-0-4ddfb59d-0306-4ffe-9660-3c4a776e127d.webp",
            _ => "media-generation-poster-glass-ocean-0-78a639e5-b813-4024-95cc-0b8e1ac9f802.webp",
        };
        return $"ms-appx:///assets/wallpapers/posters/{poster}";
    }
}

public sealed record HomeCatalogItem(string Title, string Subtitle, string Availability, string PosterUri);
