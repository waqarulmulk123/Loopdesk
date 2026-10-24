using LiveWallpaperStudio.App.ViewModels;
using Microsoft.UI.Xaml.Controls;

namespace LiveWallpaperStudio.App.Pages;

public sealed partial class DiscoverPage : Page
{
    public DiscoverViewModel ViewModel { get; } = new();

    public DiscoverPage()
    {
        InitializeComponent();

    }
}
