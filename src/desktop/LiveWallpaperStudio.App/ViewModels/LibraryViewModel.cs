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
    private string? _selectedMedia;

    [ObservableProperty]
    private string _status = string.Empty;

    public ObservableCollection<string> LocalMedia { get; } = new();

    public string MediaFolder => Path.Combine(
        Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),
        "LiveWallpaperStudio", "media");

    [RelayCommand]
    private void Refresh()
    {
        LocalMedia.Clear();
        if (!Directory.Exists(MediaFolder)) return;
        string query = SearchText.Trim();
        foreach (string file in Directory.EnumerateFiles(MediaFolder, "*.mp4", SearchOption.TopDirectoryOnly))
        {
            if (string.IsNullOrEmpty(query) || Path.GetFileName(file).Contains(query, StringComparison.OrdinalIgnoreCase))
            {
                LocalMedia.Add(file);
            }
        }
        foreach (string file in CatalogService.Load().Select(item => item.LocalVideoPath).OfType<string>())
        {
            if ((string.IsNullOrEmpty(query) || Path.GetFileName(file).Contains(query, StringComparison.OrdinalIgnoreCase)) &&
                !LocalMedia.Contains(file))
            {
                LocalMedia.Add(file);
            }
        }
    }

    [RelayCommand]
    private void Search() => Refresh();

    [RelayCommand]
    private void ApplySelected()
    {
        if (string.IsNullOrWhiteSpace(SelectedMedia) || !File.Exists(SelectedMedia))
        {
            Status = "Select a local MP4 file first.";
            return;
        }

        Status = RenderSupervisor.Instance.Start(SelectedMedia)
            ? "Wallpaper applied."
            : $"Could not start renderer: {RenderSupervisor.Instance.LastError ?? "unknown error"}";
    }
}
