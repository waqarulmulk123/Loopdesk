using System.Diagnostics;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;

namespace LiveWallpaperStudio.App.ViewModels;

public sealed partial class DiagnosticsViewModel : ObservableObject
{
    public DiagnosticsViewModel() => Refresh();

    [ObservableProperty] private string _appVersion = "1.0.0-dev";
    [ObservableProperty] private string _osVersion = Environment.OSVersion.ToString();
    [ObservableProperty] private string _rendererStatus = "Unknown";
    [ObservableProperty] private string _mediaFolder = string.Empty;
    [ObservableProperty] private string _catalogStatus = string.Empty;

    [RelayCommand]
    private void Refresh()
    {
        RendererStatus = Process.GetProcessesByName("lws-renderer").Length > 0
            ? "Renderer process is running."
            : "Renderer process is not running.";
        MediaFolder = Path.Combine(
            Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),
            "LiveWallpaperStudio", "media");
        string catalog = Path.Combine(AppContext.BaseDirectory, "assets", "catalog.json");
        CatalogStatus = File.Exists(catalog)
            ? $"Bundled catalog found at {catalog}."
            : "Bundled catalog.json not found next to the app.";
    }
}
