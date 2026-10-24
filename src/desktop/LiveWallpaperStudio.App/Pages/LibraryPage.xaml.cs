using LiveWallpaperStudio.App.ViewModels;
using Microsoft.UI.Xaml.Controls;

namespace LiveWallpaperStudio.App.Pages;

public sealed partial class LibraryPage : Page
{
    public LibraryViewModel ViewModel { get; } = new();

    public LibraryPage()
    {
        InitializeComponent();

    }
}
