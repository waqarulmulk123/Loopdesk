namespace LiveWallpaperStudio.Core;

/// <summary>
/// App settings, defaults from spec section 16. Muted by default,
/// audio disabled by default, no telemetry or crash upload by default.
/// </summary>
public sealed record PlaybackSettings(
    bool AutoplayAfterLogin = false,
    bool MuteByDefault = true,
    bool PauseOnFullscreen = true,
    bool PauseOnLock = true,
    bool PauseOnDisplayOff = true,
    bool PauseOnBatterySaver = true,
    bool PauseOnBattery = false,
    int FrameRateLimit = 30,
    string PreferredDecoder = "MediaFoundation");

public sealed record CacheSettings(
    long MaxGigabytes = 10,
    long MinimumFreeGigabytes = 2);

public sealed record PrivacySettings(
    bool SendCrashReports = false,
    bool SendUsageAnalytics = false);

public sealed record AppSettings(
    PlaybackSettings Playback,
    CacheSettings Cache,
    PrivacySettings Privacy,
    string? ApiBaseUrl = null,
    bool CatalogEnabled = false)
{
    public static AppSettings Default => new(
        new PlaybackSettings(),
        new CacheSettings(),
        new PrivacySettings());
}
