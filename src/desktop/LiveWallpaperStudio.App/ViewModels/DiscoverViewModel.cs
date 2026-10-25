using System.Collections.ObjectModel;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using LiveWallpaperStudio.App.Services;

namespace LiveWallpaperStudio.App.ViewModels;

public sealed partial class DiscoverViewModel : ObservableObject
{
    private readonly OnlineCatalogService _onlineCatalog = new();

    public DiscoverViewModel() => _ = Refresh();

    [ObservableProperty]
    private string _catalogStatus = "Catalog: not loaded.";

    [ObservableProperty]
    private string _searchText = string.Empty;

    public ObservableCollection<string> CatalogItems { get; } = new();

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
                CatalogItems.Add($"{item.Title} [{item.Theme}]{(item.Premium ? " — Premium" : string.Empty)}");
            }
        }
    }
}
