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
    private string _selectedCategory = "All";

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

    [RelayCommand]
    private void SelectCategory(string category)
    {
        SelectedCategory = category;
    }

    partial void OnSelectedCategoryChanged(string value) => ApplySearch();

    private void ApplySearch()
    {
        CatalogItems.Clear();
        string query = SearchText.Trim();
        foreach (var item in _allItems)
        {
            bool matchesQuery = string.IsNullOrEmpty(query) ||
                item.Title.Contains(query, StringComparison.OrdinalIgnoreCase) ||
                item.Description?.Contains(query, StringComparison.OrdinalIgnoreCase) == true ||
                item.Kind.Contains(query, StringComparison.OrdinalIgnoreCase);
            string category = item.Categories?.FirstOrDefault() ?? "Uncategorized";
            bool matchesCategory = SelectedCategory == "All" ||
                category.Equals(SelectedCategory, StringComparison.OrdinalIgnoreCase);
            if (matchesQuery && matchesCategory)
            {
                string resolution = item.Width is > 0 && item.Height is > 0 ? $"{item.Width}×{item.Height}" : "Resolution unavailable";
                CatalogItems.Add(new DiscoverItem(item.Id, item.Title, category, resolution, item.Premium,
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
            _ => "media-generation-poster-glass-ocean-0-78a639e5-b813-4024-95cc-0b8e1ac9f802.webp"
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
        string? finalPath = null;
        bool saved = false;
        try
        {
            CatalogStatus = $"Authorizing {selected.Title}…";
            OnlineDownload download = await _onlineCatalog.GetDownloadAsync(LocalSettingsService.ProductionApiBaseUrl, selected.Id);
            string safeName = string.Concat(selected.Id.Where(character => char.IsAsciiLetterOrDigit(character) || character is '-' or '_'));
            string stagingDirectory = Path.Combine(
                Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),
                "Loopdesk", "staging");
            finalPath = Path.Combine(stagingDirectory, safeName + ".mp4");
            var manager = new DownloadManager();
            DownloadResult result = await manager.DownloadAsync(download.DownloadUri.ToString(), finalPath + ".partial", finalPath,
                download.Variant.Sha256, download.Variant.SizeBytes);
            if (!result.Success)
            {
                CatalogStatus = $"Download failed: {result.FailureReason}";
                return;
            }

            await DownloadedWallpaperService.Instance.SaveAsync(finalPath, selected.Id, selected.Title);
            saved = true;
            int index = CatalogItems.IndexOf(selected);
            CatalogItems[index] = selected with { IsDownloaded = true };
            SelectedItem = CatalogItems[index];
            CatalogStatus = $"Downloaded {selected.Title}. It is protected in your Loopdesk vault; preview or apply it from My wallpapers.";
        }
        catch (Exception)
        {
            CatalogStatus = "Download unavailable. Check your connection and try again.";
        }
        finally
        {
            if (!saved && finalPath is not null)
            {
                TryDelete(finalPath);
                TryDelete(finalPath + ".partial");
            }
        }
    }

    private static void TryDelete(string path)
    {
        try { File.Delete(path); } catch (IOException) { }
    }

    [RelayCommand]
    private void ApplySelected()
    {
        if (SelectedItem is null || !SelectedItem.IsDownloaded)
        {
            CatalogStatus = "Download the selected wallpaper first.";
            return;
        }

        DownloadedWallpaper? wallpaper = DownloadedWallpaperService.Instance.List()
            .FirstOrDefault(download => download.Id == SelectedItem.Id);
        if (wallpaper is null)
        {
            CatalogStatus = "The protected download is unavailable. Download it again.";
            return;
        }

        try
        {
            string playbackPath = DownloadedWallpaperService.Instance.CreatePlaybackCopy(wallpaper);
            CatalogStatus = RenderSupervisor.Instance.Start(playbackPath)
                ? $"Applied {SelectedItem.Title}."
                : $"Could not apply wallpaper: {RenderSupervisor.Instance.LastError ?? "unknown error"}";
        }
        catch (Exception)
        {
            CatalogStatus = "The protected download could not be opened. Download it again.";
        }
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

public sealed record DiscoverItem(string Id, string Title, string Theme, string Resolution, bool Premium, bool IsDownloaded, string PosterUri)
{
    public string Subtitle => $"{Theme} · {Resolution}{(Premium ? " · Premium" : string.Empty)}{(IsDownloaded ? " · Downloaded" : string.Empty)}";
    public string Availability => Premium ? "PREMIUM" : "FREE";
}
