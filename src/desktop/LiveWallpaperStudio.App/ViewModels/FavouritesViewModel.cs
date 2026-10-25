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
        foreach (string id in favouriteIds)
        {
            if (!string.IsNullOrWhiteSpace(id) && id.All(character => char.IsAsciiLetterOrDigit(character) || character is '-' or '_'))
                Favourites.Add(id);
        }
        Status = Favourites.Count > 0
            ? $"Favourites: {Favourites.Count} item(s)."
            : "No favourites yet. Save one from Discover.";
    }
}
