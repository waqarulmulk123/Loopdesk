using System.Text.Json;

namespace LiveWallpaperStudio.App.Services;

/// <summary>
/// Local settings, stored as JSON under LocalAppData. Defaults match
/// the Core settings model: muted, telemetry off, catalog off, and
/// pause on fullscreen / lock / display-off / battery saver.
/// No settings are sent anywhere; this file is local only.
/// </summary>
public sealed class LocalSettingsService
{
    public const string DefaultApiBaseUrl = "https://live-wallpaper-studio-api.mwaqardula.workers.dev";
    public bool Muted { get; set; } = true;
    public bool PauseOnFullscreen { get; set; } = true;
    public bool PauseOnLock { get; set; } = true;
    public bool PauseOnDisplayOff { get; set; } = true;
    public bool PauseOnBatterySaver { get; set; } = true;
    public bool SendUsageAnalytics { get; set; } = false;
    public bool SendCrashReports { get; set; } = false;
    public string? ApiBaseUrl { get; set; } = DefaultApiBaseUrl;
    public bool HasCompletedOnboarding { get; set; } = false;
    public bool LowPowerMode { get; set; } = false;

    public static string FilePath => Path.Combine(
        Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),
        "LiveWallpaperStudio", "settings.json");

    public void Save()
    {
        Directory.CreateDirectory(Path.GetDirectoryName(FilePath)!);
        string temporaryPath = FilePath + ".tmp";
        File.WriteAllText(temporaryPath, JsonSerializer.Serialize(this, new JsonSerializerOptions { WriteIndented = true }));
        File.Move(temporaryPath, FilePath, overwrite: true);
    }

    public static LocalSettingsService Load()
    {
        try
        {
            if (File.Exists(FilePath))
            {
                return JsonSerializer.Deserialize<LocalSettingsService>(File.ReadAllText(FilePath)) ?? new LocalSettingsService();
            }
        }
        catch
        {
            // Corrupt settings must not prevent launch; defaults apply.
        }
        return new LocalSettingsService();
    }
}
