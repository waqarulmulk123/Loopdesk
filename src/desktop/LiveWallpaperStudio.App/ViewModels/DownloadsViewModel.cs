using System.Collections.ObjectModel;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using LiveWallpaperStudio.App.Services;

namespace LiveWallpaperStudio.App.ViewModels;

public sealed partial class DownloadsViewModel : ObservableObject
{
    public DownloadsViewModel() => Refresh();

    public ObservableCollection<string> Downloads { get; } = new();

    [RelayCommand]
    private void Refresh()
    {
        Downloads.Clear();
        foreach (DownloadedWallpaper wallpaper in DownloadedWallpaperService.Instance.List())
            Downloads.Add($"{wallpaper.Title} — {wallpaper.Subtitle}");
    }
}
