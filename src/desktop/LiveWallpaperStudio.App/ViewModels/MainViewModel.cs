using System.Collections.ObjectModel;
using System.Runtime.InteropServices;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using LiveWallpaperStudio.App.Services;

namespace LiveWallpaperStudio.App.ViewModels;

/// <summary>
/// M3 view model. Apply actually starts the native renderer with the
/// selected local video; Stop actually stops it. Library lists real
/// local MP4 files found under the app's local media folder, and the
/// display summary is read from Win32, not hardcoded. No placeholder
/// actions.
/// </summary>
public sealed partial class MainViewModel : ObservableObject
{
    private readonly RenderSupervisor _supervisor = RenderSupervisor.Instance;

    public MainViewModel()
    {
        var settings = LocalSettingsService.Load();
        Muted = settings.Muted;
        PauseOnFullscreen = settings.PauseOnFullscreen;
        PauseOnLock = settings.PauseOnLock;
        PauseOnDisplayOff = settings.PauseOnDisplayOff;
        PauseOnBatterySaver = settings.PauseOnBatterySaver;
        SendUsageAnalytics = settings.SendUsageAnalytics;
        SendCrashReports = settings.SendCrashReports;
        ApiBaseUrl = settings.ApiBaseUrl ?? string.Empty;
        RefreshLocalMedia();
        RefreshDisplays();
        RefreshCatalog();
    }

    [ObservableProperty]
    private string _status = "Offline mode. Online catalog is not configured.";

    [ObservableProperty]
    private string _videoPath = string.Empty;

    [ObservableProperty]
    private string _displaySummary = "Displays: unknown";

    [ObservableProperty]
    private bool _muted = true;

    [ObservableProperty]
    private bool _pauseOnFullscreen = true;

    [ObservableProperty]
    private bool _pauseOnLock = true;

    [ObservableProperty]
    private bool _pauseOnDisplayOff = true;

    [ObservableProperty]
    private bool _pauseOnBatterySaver = true;

    [ObservableProperty]
    private bool _sendUsageAnalytics;

    [ObservableProperty]
    private bool _sendCrashReports;

    [ObservableProperty]
    private string _apiBaseUrl = string.Empty;

    [ObservableProperty]
    private string _catalogStatus = "Catalog: not loaded.";

    public ObservableCollection<string> LocalMedia { get; } = new();

    public ObservableCollection<string> CatalogItems { get; } = new();

    public string MediaFolder => Path.Combine(
        Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),
        "LiveWallpaperStudio", "media");

    [RelayCommand]
    private void Apply()
    {
        if (string.IsNullOrWhiteSpace(VideoPath) || !File.Exists(VideoPath))
        {
            Status = "Choose a local H.264 MP4 file first.";
            return;
        }

        if (_supervisor.Start(VideoPath))
        {
            Status = "Renderer started. If attachment is unsupported, it will stop and leave the desktop untouched.";
        }
        else
        {
            Status = $"Renderer failed to start: {_supervisor.LastError ?? "unknown error"}";
        }
    }

    [RelayCommand]
    private void Stop()
    {
        _supervisor.Stop();
        Status = "Wallpaper stopped.";
    }

    [RelayCommand]
    private void RefreshLocalMedia()
    {
        LocalMedia.Clear();
        if (!Directory.Exists(MediaFolder)) return;
        foreach (string file in Directory.EnumerateFiles(MediaFolder, "*.mp4", SearchOption.TopDirectoryOnly))
        {
            LocalMedia.Add(file);
        }
    }

    [RelayCommand]
    private void SaveSettings()
    {
        var settings = new LocalSettingsService
        {
            Muted = Muted,
            PauseOnFullscreen = PauseOnFullscreen,
            PauseOnLock = PauseOnLock,
            PauseOnDisplayOff = PauseOnDisplayOff,
            PauseOnBatterySaver = PauseOnBatterySaver,
            SendUsageAnalytics = SendUsageAnalytics,
            SendCrashReports = SendCrashReports,
            ApiBaseUrl = string.IsNullOrWhiteSpace(ApiBaseUrl) ? null : ApiBaseUrl.Trim(),
        };
        settings.Save();
        Status = "Settings saved locally.";
    }

    [RelayCommand]
    private void RefreshCatalog()
    {
        CatalogItems.Clear();
        var items = CatalogService.Load();
        foreach (var item in items)
        {
            CatalogItems.Add($"{item.Title} [{item.Theme}]{(item.Premium ? " — Premium" : string.Empty)}");
        }
        CatalogStatus = items.Count > 0
            ? $"Catalog: {items.Count} item(s) loaded from assets/catalog.json."
            : "Catalog: not configured or catalog.json not found next to the app.";
        if (items.Count > 0)
        {
            Status = "Catalog loaded. Online catalog is not configured unless an API URL is set in Settings.";
        }
    }

    [RelayCommand]
    private void RefreshDisplays()
    {
        int monitors = GetSystemMetrics(80); // SM_CMONITORS
        int primaryWidth = GetSystemMetrics(0); // SM_CXSCREEN
        int primaryHeight = GetSystemMetrics(1); // SM_CYSCREEN
        DisplaySummary = monitors > 0
            ? $"Displays: {monitors} monitor(s) detected; primary {primaryWidth}x{primaryHeight}."
            : "Displays: could not read monitor count from Win32.";
    }

    [DllImport("user32.dll")]
    private static extern int GetSystemMetrics(int nIndex);
}
