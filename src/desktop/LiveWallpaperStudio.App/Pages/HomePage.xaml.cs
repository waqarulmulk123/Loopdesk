using LiveWallpaperStudio.App.ViewModels;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml;

namespace LiveWallpaperStudio.App.Pages;

public sealed partial class HomePage : Page
{
    public HomeViewModel ViewModel { get; } = new();

    public HomePage()
    {
        InitializeComponent();
    }

    private void BrowseButton_Click(object sender, RoutedEventArgs e) => Frame.Navigate(typeof(DiscoverPage));

    private void LibraryButton_Click(object sender, RoutedEventArgs e) => Frame.Navigate(typeof(LibraryPage));

    private void CatalogItem_Click(object sender, ItemClickEventArgs e) => Frame.Navigate(typeof(DiscoverPage));
}
