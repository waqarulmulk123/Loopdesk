using System.Collections.ObjectModel;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using LiveWallpaperStudio.App.Services;

namespace LiveWallpaperStudio.App.ViewModels;

public sealed partial class LibraryViewModel : ObservableObject
{
    public LibraryViewModel() => Refresh();

    [ObservableProperty]
    private string _searchText = string.Empty;

    [ObservableProperty]
    private DownloadedWallpaper? _selectedMedia;

    [ObservableProperty]
    private string _status = string.Empty;

    public ObservableCollection<DownloadedWallpaper> Wallpapers { get; } = new();

    [RelayCommand]
    private void Refresh()
    {
        Wallpapers.Clear();
        string query = SearchText.Trim();
        foreach (DownloadedWallpaper wallpaper in DownloadedWallpaperService.Instance.List())
        {
            if (string.IsNullOrEmpty(query) || wallpaper.Title.Contains(query, StringComparison.OrdinalIgnoreCase))
            {
                Wallpapers.Add(wallpaper);
            }
        }
        SelectedMedia = Wallpapers.FirstOrDefault();
        Status = Wallpapers.Count == 0
            ? "No cloud downloads yet. Choose a wallpaper in Discover first."
            : $"{Wallpapers.Count} protected cloud download(s) available.";
    }

    [RelayCommand]
    private void Search() => Refresh();

    public string? PreparePreview()
    {
        if (SelectedMedia is null)
        {
            Status = "Select a downloaded wallpaper first.";
            return null;
        }
        try
        {
            Status = $"Previewing {SelectedMedia.Title}.";
            return DownloadedWallpaperService.Instance.CreatePlaybackCopy(SelectedMedia);
        }
        catch (System.Security.Cryptography.CryptographicException)
        {
            Status = "This protected download belongs to a different Windows account. Download it again from Discover.";
            return null;
        }
        catch (Exception)
        {
            Status = "The protected download could not be opened. Download it again from Discover.";
            return null;
        }
    }

    [RelayCommand]
    private void ApplySelected()
    {
        string? playbackPath = PreparePreview();
        if (playbackPath is null) return;
        Status = RenderSupervisor.Instance.Start(playbackPath)
            ? $"Applied {SelectedMedia!.Title}."
            : $"Could not apply wallpaper: {RenderSupervisor.Instance.LastError ?? "unknown error"}";
    }

    [RelayCommand]
    private void StopWallpaper()
    {
        RenderSupervisor.Instance.Stop();
        Status = "Wallpaper stopped.";
    }

    [RelayCommand]
    private void RemoveSelected()
    {
        if (SelectedMedia is null)
        {
            Status = "Select a downloaded wallpaper first.";
            return;
        }

        try
        {
            string title = SelectedMedia.Title;
            RenderSupervisor.Instance.Stop();
            DownloadedWallpaperService.Instance.Remove(SelectedMedia);
            Refresh();
            Status = $"Removed {title} from this device.";
        }
        catch (Exception)
        {
            Status = "Could not remove this download. Try again after stopping playback.";
        }
    }
}
