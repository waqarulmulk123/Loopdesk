using LiveWallpaperStudio.App.ViewModels;
using Microsoft.UI.Xaml.Controls;

namespace LiveWallpaperStudio.App.Pages;

public sealed partial class HomePage : Page
{
    public MainViewModel ViewModel { get; } = new();

    public HomePage()
    {
        InitializeComponent();
    }
}
