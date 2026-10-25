namespace LiveWallpaperStudio.Core;

/// <summary>
/// M0 IPC validation rules from spec section 8.
/// Control messages only, 1 MiB maximum, protocol version 1.
/// Paths must be validated by the caller against the app cache or a
/// user-approved import; network/UNC paths are rejected by default.
/// </summary>
public static class IpcValidation
{
    public const int ProtocolVersion = 1;
    public const int MaxMessageBytes = 1024 * 1024;

    private static readonly string[] Methods =
    {
        "engine.hello", "engine.shutdown", "engine.status", "engine.diagnostics",
        "displays.list", "wallpaper.apply", "wallpaper.remove", "wallpaper.setLayout",
        "playback.pause", "playback.resume", "playback.setVolume", "playback.setRate",
        "settings.setPolicy", "events.subscribe",
    };

    public static bool IsValidMethod(string method) => Methods.Contains(method);

    public static bool IsValidEnvelope(IpcEnvelope envelope, int messageBytes) =>
        envelope.ProtocolVersion == ProtocolVersion
        && messageBytes > 0
        && messageBytes <= MaxMessageBytes
        && Guid.TryParse(envelope.RequestId, out _)
        && envelope.Kind == "request"
        && IsValidMethod(envelope.Method);

    public static bool IsAllowedLocalPath(string path)
    {
        if (string.IsNullOrWhiteSpace(path))
        {
            return false;
        }

        // Reject UNC and network paths by default.
        if (path.StartsWith(@"\\", StringComparison.Ordinal) ||
            path.StartsWith("http://", StringComparison.OrdinalIgnoreCase) ||
            path.StartsWith("https://", StringComparison.OrdinalIgnoreCase))
        {
            return false;
        }

        return true;
    }
}
