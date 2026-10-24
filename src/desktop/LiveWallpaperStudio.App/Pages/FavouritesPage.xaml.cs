using LiveWallpaperStudio.App.ViewModels;
using Microsoft.UI.Xaml.Controls;

namespace LiveWallpaperStudio.App.Pages;

public sealed partial class FavouritesPage : Page
{
    public FavouritesViewModel ViewModel { get; } = new();

    public FavouritesPage()
    {
        InitializeComponent();
    }
}
