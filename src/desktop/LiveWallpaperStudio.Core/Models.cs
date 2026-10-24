namespace LiveWallpaperStudio.Core;

public enum WallpaperKind
{
    Video,
    StaticImage,
    HtmlInteractive,
    Shader,
}

public enum FitMode
{
    Fill,
    Fit,
    Stretch,
    Center,
}

public enum PlaybackState
{
    Stopped,
    Preparing,
    Attaching,
    Playing,
    Paused,
    Stopping,
    Recovering,
    Failed,
}

public sealed record MediaVariant(
    string Id,
    string MimeType,
    string Codec,
    int Width,
    int Height,
    double Fps,
    long Bytes,
    string Sha256,
    string? DownloadUrl);

public sealed record WallpaperDescriptor(
    Guid Id,
    string Title,
    WallpaperKind Kind,
    string? Description,
    string? ThumbnailUrl,
    string? Author,
    IReadOnlyList<MediaVariant> Variants);

public sealed record IpcEnvelope(
    int ProtocolVersion,
    string RequestId,
    string Kind,
    string Method,
    object? Payload);
