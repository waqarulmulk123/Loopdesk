using Microsoft.UI.Xaml;

namespace LiveWallpaperStudio.App;

public partial class App : Application
{
    private Mutex? _singleInstance;
    private Window? _window;
    private Services.TrayService? _tray;
    private bool _exiting;
    private int _restoring;

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
        // Do not wait for the existing instance: a second launch must exit,
        // not hang until the first Loopdesk process closes.
        _singleInstance = new Mutex(initiallyOwned: false, "Local\\Loopdesk.Desktop", out bool isFirstInstance);
        if (!isFirstInstance)
        {
            _singleInstance.Dispose();
            _singleInstance = null;
            Exit();
            return;
        }
        _singleInstance.WaitOne();

        Services.DownloadedWallpaperService.Instance.CleanupPlaybackCopies();
        _window = new MainWindow();
        _tray = new Services.TrayService(WinRT.Interop.WindowNative.GetWindowHandle(_window),
            () => { _window.AppWindow.Show(); _window.Activate(); },
            StopAndForgetWallpaper,
            () => _ = RestoreLastWallpaperAsync(waitForDesktop: true),
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
            Services.RenderSupervisor.Instance.Shutdown();
            _singleInstance?.Dispose();
            _singleInstance = null;
        };
        _window.Activate();
        _ = RestoreLastWallpaperAsync();
    }

    private void StopAndForgetWallpaper()
    {
        Services.RenderSupervisor.Instance.Stop();
        Services.LocalSettingsService.Update(settings => settings.AppliedWallpaperId = null);
    }

    private async Task RestoreLastWallpaperAsync(bool waitForDesktop = false)
    {
        if (Interlocked.Exchange(ref _restoring, 1) != 0) return;
        try
        {
            if (waitForDesktop) await Task.Delay(TimeSpan.FromSeconds(2));
            var settings = Services.LocalSettingsService.Load();
            if (string.IsNullOrWhiteSpace(settings.AppliedWallpaperId) || Services.RenderSupervisor.Instance.IsRunning) return;

            var wallpaper = Services.DownloadedWallpaperService.Instance.List()
                .FirstOrDefault(item => item.Id == settings.AppliedWallpaperId);
            if (wallpaper is null)
            {
                Services.LocalSettingsService.Update(latest => latest.AppliedWallpaperId = null);
                return;
            }

            for (int attempt = 0; attempt < 3; attempt++)
            {
                try
                {
                    string playbackPath = Services.DownloadedWallpaperService.Instance.CreatePlaybackCopy(wallpaper);
                    if (Services.RenderSupervisor.Instance.Start(playbackPath)) return;
                }
                catch (System.Security.Cryptography.CryptographicException)
                {
                    Services.LocalSettingsService.Update(latest => latest.AppliedWallpaperId = null);
                    return;
                }
                catch (IOException) { }
                await Task.Delay(TimeSpan.FromSeconds(2));
            }
        }
        finally
        {
            Interlocked.Exchange(ref _restoring, 0);
        }
    }
}
