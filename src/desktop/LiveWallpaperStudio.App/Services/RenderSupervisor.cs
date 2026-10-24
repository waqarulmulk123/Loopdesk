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

    private Process? _process;
    private int _unexpectedExits;
    private bool _stopping;

    public string? LastError { get; private set; }

    public bool IsRunning => _process is { HasExited: false };

    public bool Start(string videoPath)
    {
        Stop();
        LastError = null;
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

        _process.EnableRaisingEvents = true;
        _process.Exited += (sender, _) =>
        {
            if (!_stopping && ReferenceEquals(sender, _process)) _unexpectedExits++;
        };
        return true;
    }

    public void Stop()
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
        _unexpectedExits = 0;
        _stopping = false;
    }

    public bool ShouldRestartAfterUnexpectedExit() => _unexpectedExits < 3;
}
