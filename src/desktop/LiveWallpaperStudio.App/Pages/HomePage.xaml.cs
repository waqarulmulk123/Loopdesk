using LiveWallpaperStudio.App.Services;
using LiveWallpaperStudio.App.ViewModels;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Windows.Media.Core;

namespace LiveWallpaperStudio.App.Pages;

public sealed partial class HomePage : Page
{
    public MainViewModel ViewModel { get; } = new();

    public HomePage()
    {
        InitializeComponent();
        Loaded += (_, _) => AnimationService.Pulse(AnimatedBanner).Begin();
    }

    private void PreviewButton_Click(object sender, RoutedEventArgs e)
    {
        if (string.IsNullOrWhiteSpace(ViewModel.VideoPath) || !File.Exists(ViewModel.VideoPath))
        {
            return;
        }
        PreviewPlayer.Source = MediaSource.CreateFromUri(new Uri(ViewModel.VideoPath));
        PreviewPlayer.MediaPlayer?.Play();
    }
}
