using System.Diagnostics;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;

namespace LiveWallpaperStudio.App.ViewModels;

public sealed partial class DiagnosticsViewModel : ObservableObject
{
    public DiagnosticsViewModel() => Refresh();

    [ObservableProperty] private string _appVersion = typeof(DiagnosticsViewModel).Assembly.GetName().Version?.ToString(3) ?? "1.0.0";
    [ObservableProperty] private string _osVersion = Environment.OSVersion.ToString();
    [ObservableProperty] private string _rendererStatus = "Unknown";
    [ObservableProperty] private string _storageStatus = string.Empty;
    [ObservableProperty] private string _catalogStatus = string.Empty;

    [RelayCommand]
    private void Refresh()
    {
        RendererStatus = Process.GetProcessesByName("lws-renderer").Length > 0
            ? "Renderer process is running."
            : "Renderer process is not running.";
        StorageStatus = "Cloud downloads are protected per Windows user; playback copies are short-lived.";
        CatalogStatus = "Cloud catalog only. No drive folders or local catalog files are scanned.";
    }
}
