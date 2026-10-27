using Microsoft.UI.Xaml;

namespace LiveWallpaperStudio.App;

public partial class App : Application
{
    private Window? _window;
    private Services.TrayService? _tray;
    private bool _exiting;

    public App()
    {
        UnhandledException += (_, e) =>
        {
            try
            {
                string directory = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData), "Loopdesk", "logs");
                Directory.CreateDirectory(directory);
                File.WriteAllText(Path.Combine(directory, "last-crash.txt"), $"{DateTimeOffset.UtcNow:O}\n{e.Exception}");
            }
            catch (IOException) { }
            catch (UnauthorizedAccessException) { }
        };
        InitializeComponent();
    }

    protected override void OnLaunched(LaunchActivatedEventArgs args)
    {
        Services.DownloadedWallpaperService.Instance.CleanupPlaybackCopies();
        _window = new MainWindow();
        _tray = new Services.TrayService(WinRT.Interop.WindowNative.GetWindowHandle(_window),
            () => { _window.AppWindow.Show(); _window.Activate(); },
            () => { _exiting = true; _window.Close(); });
        _window.AppWindow.Closing += (_, e) =>
        {
            if (!_exiting && _tray.IsAvailable)
            {
                e.Cancel = true;
                _window.AppWindow.Hide();
            }
        };
        _window.Closed += (_, _) =>
        {
            _tray.Dispose();
            Services.RenderSupervisor.Instance.Stop();
        };
        _window.Activate();
    }
}
