using System.Collections.ObjectModel;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using LiveWallpaperStudio.App.Services;

namespace LiveWallpaperStudio.App.ViewModels;

public sealed partial class FavouritesViewModel : ObservableObject
{
    public FavouritesViewModel() => Refresh();

    [ObservableProperty]
    private string _status = string.Empty;

    public ObservableCollection<string> Favourites { get; } = new();

    [RelayCommand]
    private void Refresh()
    {
        Favourites.Clear();
        var favouriteIds = FavouritesService.Load();
        var catalog = CatalogService.Load();
        foreach (var item in catalog)
        {
            if (favouriteIds.Contains(item.Id))
            {
                Favourites.Add($"{item.Title} [{item.Theme}]{(item.Premium ? " — Premium" : string.Empty)}");
            }
        }
        // Local file favourites (paths) are shown as-is.
        foreach (string id in favouriteIds)
        {
            if (File.Exists(id) && !Favourites.Any(f => f.StartsWith(id)))
            {
                Favourites.Add(id);
            }
        }
        Status = Favourites.Count > 0
            ? $"Favourites: {Favourites.Count} item(s)."
            : "No favourites yet. Add one from Discover by saving its ID in favourites.json, or use the Favourites service from a catalog item.";
    }
}
