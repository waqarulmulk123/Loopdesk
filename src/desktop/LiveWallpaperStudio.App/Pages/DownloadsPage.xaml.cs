using LiveWallpaperStudio.App.ViewModels;
using Microsoft.UI.Xaml.Controls;

namespace LiveWallpaperStudio.App.Pages;

public sealed partial class DownloadsPage : Page
{
    public DownloadsViewModel ViewModel { get; } = new();

    public DownloadsPage()
    {
        InitializeComponent();

    }
}
