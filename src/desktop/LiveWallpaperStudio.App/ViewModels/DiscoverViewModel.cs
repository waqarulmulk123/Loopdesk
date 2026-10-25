using System.Collections.ObjectModel;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using LiveWallpaperStudio.App.Services;
using LiveWallpaperStudio.Core;

namespace LiveWallpaperStudio.App.ViewModels;

public sealed partial class DiscoverViewModel : ObservableObject
{
    private readonly OnlineCatalogService _onlineCatalog = new();

    public DiscoverViewModel() => _ = Refresh();

    [ObservableProperty]
    private string _catalogStatus = "Catalog: not loaded.";

    [ObservableProperty]
    private string _searchText = string.Empty;

    [ObservableProperty]
    private DiscoverItem? _selectedItem;

    public ObservableCollection<DiscoverItem> CatalogItems { get; } = new();

    private List<OnlineCatalogItem> _allItems = new();

    [RelayCommand]
    private async Task Refresh()
    {
        const string apiBaseUrl = LocalSettingsService.ProductionApiBaseUrl;
        if (string.IsNullOrWhiteSpace(apiBaseUrl))
        {
            _allItems = [];
            CatalogStatus = "Cloud catalog is not configured.";
            ApplySearch();
            return;
        }
        try
        {
            _allItems = (await _onlineCatalog.LoadAsync(apiBaseUrl)).ToList();
            CatalogStatus = $"Cloud catalog: {_allItems.Count} item(s) available.";
        }
        catch (Exception)
        {
            _allItems = [];
            CatalogStatus = "Cloud catalog is temporarily unavailable. Please try again.";
        }
        ApplySearch();
    }

    [RelayCommand]
    private void Search()
    {
        ApplySearch();
    }

    private void ApplySearch()
    {
        CatalogItems.Clear();
        string query = SearchText.Trim();
        foreach (var item in _allItems)
        {
            if (string.IsNullOrEmpty(query) ||
                item.Title.Contains(query, StringComparison.OrdinalIgnoreCase) ||
                item.Description?.Contains(query, StringComparison.OrdinalIgnoreCase) == true ||
                item.Kind.Contains(query, StringComparison.OrdinalIgnoreCase))
            {
                CatalogItems.Add(new DiscoverItem(item.Id, item.Title, "Cloud", item.Premium,
                    DownloadedWallpaperService.Instance.List().Any(download => download.Id == item.Id), PosterFor(item.Id)));
            }
        }
        if (SelectedItem is null && CatalogItems.Count > 0)
            SelectedItem = CatalogItems[0];
    }

    private static string PosterFor(string id)
    {
        string poster = id switch
        {
            "theme-loopdesk-aurora-dreamscape" => "media-generation-poster-aurora-mountains-0-b0747490-fea4-40da-b98a-3c4202fe903d.webp",
            "theme-loopdesk-cosmic-infinity" => "media-generation-poster-galaxy-nebula-0-bf04bf8b-9f6b-4e6a-aeab-ebed4775e506.webp",
            "theme-loopdesk-liquid-neon-flow" => "media-generation-poster-neon-cyberpunk-city-0-4ddfb59d-0306-4ffe-9660-3c4a776e127d.webp",
            "theme-loopdesk-bioluminescent-ocean" => "media-generation-poster-glass-ocean-0-78a639e5-b813-4024-95cc-0b8e1ac9f802.webp",
            _ => "media-generation-poster-ocean-sunset-0-211d15cc-bd5b-47cb-82df-35df9f6d2098.webp"
        };
        return $"ms-appx:///assets/wallpapers/posters/{poster}";
    }

    [RelayCommand]
    private async Task DownloadSelected()
    {
        DiscoverItem? selected = SelectedItem;
        if (selected is null)
        {
            CatalogStatus = "Select a wallpaper to download.";
            return;
        }
        try
        {
            CatalogStatus = $"Authorizing {selected.Title}…";
            OnlineDownload download = await _onlineCatalog.GetDownloadAsync(LocalSettingsService.ProductionApiBaseUrl, selected.Id);
            string safeName = string.Concat(selected.Id.Where(character => char.IsAsciiLetterOrDigit(character) || character is '-' or '_'));
            string stagingDirectory = Path.Combine(Path.GetTempPath(), "Loopdesk", "downloads");
            string finalPath = Path.Combine(stagingDirectory, safeName + ".mp4");
            var manager = new DownloadManager();
            DownloadResult result = await manager.DownloadAsync(download.DownloadUri.ToString(), finalPath + ".partial", finalPath,
                download.Variant.Sha256, download.Variant.SizeBytes);
            if (!result.Success)
            {
                CatalogStatus = $"Download failed: {result.FailureReason}";
                return;
            }

            await DownloadedWallpaperService.Instance.SaveAsync(finalPath, selected.Id, selected.Title);
            int index = CatalogItems.IndexOf(selected);
            CatalogItems[index] = selected with { IsDownloaded = true };
            SelectedItem = CatalogItems[index];
            CatalogStatus = $"Downloaded {selected.Title}. It is protected in your Loopdesk vault; preview or apply it from My wallpapers.";
        }
        catch (Exception ex)
        {
            CatalogStatus = $"Download unavailable: {ex.Message}";
        }
    }

    [RelayCommand]
    private void ApplySelected()
    {
        if (SelectedItem is null || !SelectedItem.IsDownloaded)
        {
            CatalogStatus = "Download the selected wallpaper first, then preview or apply it from My wallpapers.";
            return;
        }
        CatalogStatus = "Open My wallpapers to preview or apply this protected download.";
    }

    [RelayCommand]
    private void ToggleFavourite()
    {
        if (SelectedItem is null)
        {
            CatalogStatus = "Select a wallpaper to save it to favourites.";
            return;
        }

        CatalogStatus = FavouritesService.Toggle(SelectedItem.Id)
            ? $"Saved {SelectedItem.Title} to favourites."
            : $"Removed {SelectedItem.Title} from favourites.";
    }
}

public sealed record DiscoverItem(string Id, string Title, string Theme, bool Premium, bool IsDownloaded, string PosterUri)
{
    public string Subtitle => $"{Theme}{(Premium ? " — Premium" : string.Empty)}{(IsDownloaded ? " — Downloaded" : string.Empty)}";
}
