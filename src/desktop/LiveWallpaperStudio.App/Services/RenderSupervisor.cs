using System.Diagnostics;

namespace LiveWallpaperStudio.App.Services;

/// <summary>
/// Renderer supervisor (spec section 5). Starts the native renderer in
/// the interactive user session, watches for unexpected exit, and
/// stops it on explicit Exit. It is NOT a Windows Service and it is
/// never elevated. Bounded restarts only: after 3 unexpected exits
/// in a row it stops trying and reports failure instead of looping.
/// </summary>
public sealed class RenderSupervisor
{
    public static RenderSupervisor Instance { get; } = new();

    private const int MaximumUnexpectedExits = 3;
    private readonly object _gate = new();
    private Process? _process;
    private int _unexpectedExits;
    private bool _stopping;
    private int _generation;
    private string? _activeVideoPath;

    public string? LastError { get; private set; }

    public bool IsRunning
    {
        get
        {
            lock (_gate) return _process is { HasExited: false };
        }
    }

    public bool Start(string videoPath)
    {
        lock (_gate)
        {
            _generation++;
            StopCore(cleanupPlaybackCopies: false);
            _unexpectedExits = 0;
            _activeVideoPath = videoPath;
            LastError = null;
            return StartCore(videoPath);
        }
    }

    private bool StartCore(string videoPath)
    {
        if (!File.Exists(videoPath))
        {
            LastError = "The selected video no longer exists.";
            return false;
        }

        string exe = Path.Combine(AppContext.BaseDirectory, "lws-renderer.exe");
        if (!File.Exists(exe))
        {
            // Development layout: renderer output next to the app output
            // is copied by build-desktop.ps1. Without it, do not fake a start.
            LastError = $"Renderer not found: {exe}";
            return false;
        }

        var start = new ProcessStartInfo(exe)
        {
            UseShellExecute = false,
            CreateNoWindow = true,
        };
        start.ArgumentList.Add("--video");
        start.ArgumentList.Add(Path.GetFullPath(videoPath));
        try
        {
            _process = Process.Start(start);
        }
        catch (Exception ex)
        {
            LastError = ex.Message;
            return false;
        }
        if (_process is null)
        {
            LastError = "Windows did not start the renderer process.";
            return false;
        }

        // A process can start and immediately exit when Explorer cannot host
        // the wallpaper window. Report that real failure, not a false apply.
        if (_process.WaitForExit(300))
        {
            LastError = $"Renderer exited immediately (code {_process.ExitCode}).";
            _process.Dispose();
            _process = null;
            return false;
        }

        Process started = _process;
        started.EnableRaisingEvents = true;
        started.Exited += (_, _) =>
        {
            HandleUnexpectedExit(started);
        };
        return true;
    }

    private void HandleUnexpectedExit(Process exitedProcess)
    {
        string? path;
        int generation;
        int failures;
        lock (_gate)
        {
            if (_stopping || !ReferenceEquals(exitedProcess, _process)) return;
            _process = null;
            exitedProcess.Dispose();
            path = _activeVideoPath;
            generation = _generation;
            failures = ++_unexpectedExits;
            if (failures >= MaximumUnexpectedExits)
            {
                LastError = "Renderer stopped repeatedly. Reapply the wallpaper after checking your display and graphics driver.";
                return;
            }
        }

        if (!string.IsNullOrWhiteSpace(path)) _ = RestartAfterUnexpectedExitAsync(path, generation, failures);
    }

    private async Task RestartAfterUnexpectedExitAsync(string videoPath, int generation, int failures)
    {
        // 1s then 2s: recover short Explorer/GPU transitions without a busy loop.
        await Task.Delay(TimeSpan.FromSeconds(1 << (failures - 1)));
        lock (_gate)
        {
            if (_stopping || generation != _generation || IsRunning) return;
            if (!StartCore(videoPath) && generation == _generation)
            {
                LastError ??= "Renderer could not restart.";
            }
        }
    }

    public void Stop(bool cleanupPlaybackCopies = true)
    {
        lock (_gate)
        {
            _generation++;
            StopCore(cleanupPlaybackCopies);
        }
    }

    private void StopCore(bool cleanupPlaybackCopies)
    {
        _stopping = true;
        if (_process is { HasExited: false })
        {
            try
            {
                _process.Kill(entireProcessTree: true);
            }
            catch
            {
                // Process already exited; nothing to kill.
            }
        }
        _process?.Dispose();
        _process = null;
        _activeVideoPath = null;
        if (cleanupPlaybackCopies) DownloadedWallpaperService.Instance.CleanupPlaybackCopies();
        _unexpectedExits = 0;
        _stopping = false;
    }

    public bool ShouldRestartAfterUnexpectedExit() => _unexpectedExits < MaximumUnexpectedExits;
}
