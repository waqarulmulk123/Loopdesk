using System.Collections.ObjectModel;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;

namespace LiveWallpaperStudio.App.ViewModels;

public sealed partial class DownloadsViewModel : ObservableObject
{
    public DownloadsViewModel() => Refresh();

    public ObservableCollection<string> Downloads { get; } = new();

    public string DownloadsFolder => Path.Combine(
        Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),
        "LiveWallpaperStudio", "downloads");

    [RelayCommand]
    private void Refresh()
    {
        Downloads.Clear();
        if (!Directory.Exists(DownloadsFolder)) return;
        foreach (string file in Directory.EnumerateFiles(DownloadsFolder, "*", SearchOption.TopDirectoryOnly))
        {
            var info = new FileInfo(file);
            Downloads.Add($"{info.Name} — {info.Length / 1024} KB");
        }
    }
}
