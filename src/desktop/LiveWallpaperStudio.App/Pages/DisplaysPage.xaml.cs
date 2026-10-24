using LiveWallpaperStudio.App.ViewModels;
using Microsoft.UI.Xaml.Controls;

namespace LiveWallpaperStudio.App.Pages;

public sealed partial class DisplaysPage : Page
{
    public DisplaysViewModel ViewModel { get; } = new();

    public DisplaysPage()
    {
        InitializeComponent();

    }
}
