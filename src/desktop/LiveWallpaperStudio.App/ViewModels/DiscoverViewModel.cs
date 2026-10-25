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

    private List<CatalogService.CatalogItem> _allItems = new();

    [RelayCommand]
    private async Task Refresh()
    {
        _allItems = CatalogService.Load();
        var apiBaseUrl = LocalSettingsService.Load().ApiBaseUrl ?? LocalSettingsService.DefaultApiBaseUrl;
        if (!string.IsNullOrWhiteSpace(apiBaseUrl))
        {
            try
            {
                var onlineItems = await _onlineCatalog.LoadAsync(apiBaseUrl);
                _allItems = onlineItems.Select(item => new CatalogService.CatalogItem(item.Id, item.Title, string.Empty, null, item.Premium, "Online", string.Empty)).ToList();
                CatalogStatus = $"Online catalog: {_allItems.Count} item(s) from {new Uri(apiBaseUrl).Host}.";
            }
            catch (Exception ex)
            {
                CatalogStatus = _allItems.Count > 0
                    ? $"Online catalog unavailable ({ex.Message}); showing local catalog."
                    : $"Online catalog unavailable: {ex.Message}";
            }
        }
        ApplySearch();
        if (string.IsNullOrWhiteSpace(apiBaseUrl))
            CatalogStatus = _allItems.Count > 0
                ? $"Catalog: {_allItems.Count} item(s) loaded from assets/catalog.json."
                : "Catalog: not configured or catalog.json not found next to the app.";
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
                item.Theme.Contains(query, StringComparison.OrdinalIgnoreCase))
            {
                CatalogItems.Add(new DiscoverItem(item.Id, item.Title, item.Theme, item.Premium, item.LocalVideoPath, item.Theme == "Online", PosterFor(item)));
            }
        }
        if (SelectedItem is null && CatalogItems.Count > 0)
            SelectedItem = CatalogItems[0];
    }

    private static string PosterFor(CatalogService.CatalogItem item)
    {
        if (!string.IsNullOrWhiteSpace(item.Poster))
            return $"ms-appx:///{item.Poster.Replace('\\', '/')}";

        string poster = item.Id switch
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
        if (!selected.IsOnline)
        {
            CatalogStatus = "This wallpaper is already bundled locally. Select Apply to use it.";
            return;
        }

        try
        {
            CatalogStatus = $"Authorizing {selected.Title}…";
            string apiBaseUrl = LocalSettingsService.Load().ApiBaseUrl ?? LocalSettingsService.DefaultApiBaseUrl;
            OnlineDownload download = await _onlineCatalog.GetDownloadAsync(apiBaseUrl, selected.Id);
            string mediaDirectory = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData), "LiveWallpaperStudio", "media");
            string safeName = string.Concat(selected.Id.Where(character => char.IsAsciiLetterOrDigit(character) || character is '-' or '_'));
            string finalPath = Path.Combine(mediaDirectory, safeName + ".mp4");
            var manager = new DownloadManager();
            DownloadResult result = await manager.DownloadAsync(download.DownloadUri.ToString(), finalPath + ".partial", finalPath,
                download.Variant.Sha256, download.Variant.Size_Bytes);
            if (!result.Success)
            {
                CatalogStatus = $"Download failed: {result.FailureReason}";
                return;
            }

            int index = CatalogItems.IndexOf(selected);
            CatalogItems[index] = selected with { LocalPath = finalPath };
            SelectedItem = CatalogItems[index];
            CatalogStatus = $"Downloaded {selected.Title}. Select Apply to set it as your wallpaper.";
        }
        catch (Exception ex)
        {
            CatalogStatus = $"Download unavailable: {ex.Message}";
        }
    }

    [RelayCommand]
    private void ApplySelected()
    {
        if (SelectedItem?.LocalPath is not { Length: > 0 } path || !File.Exists(path))
        {
            CatalogStatus = "Download the selected wallpaper before applying it.";
            return;
        }

        CatalogStatus = RenderSupervisor.Instance.Start(path)
            ? $"Applied {SelectedItem.Title}."
            : $"Could not start renderer: {RenderSupervisor.Instance.LastError ?? "unknown error"}";
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

public sealed record DiscoverItem(string Id, string Title, string Theme, bool Premium, string? LocalPath, bool IsOnline, string PosterUri)
{
    public string Subtitle => $"{Theme}{(Premium ? " — Premium" : string.Empty)}{(LocalPath is not null ? " — Ready" : string.Empty)}";
}
