using LiveWallpaperStudio.App.ViewModels;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Windows.Media.Core;

namespace LiveWallpaperStudio.App.Pages;

public sealed partial class LibraryPage : Page
{
    public LibraryViewModel ViewModel { get; } = new();

    public LibraryPage()
    {
        InitializeComponent();

    }

    private void PreviewButton_Click(object sender, RoutedEventArgs e)
    {
        string? path = ViewModel.PreparePreview();
        if (path is null) return;
        PreviewPlayer.Source = MediaSource.CreateFromUri(new Uri(path));
        PreviewPlayer.MediaPlayer?.Play();
    }

    private void RemoveButton_Click(object sender, RoutedEventArgs e)
    {
        PreviewPlayer.MediaPlayer?.Pause();
        PreviewPlayer.Source = null;
        ViewModel.RemoveSelectedCommand.Execute(null);
    }
}
