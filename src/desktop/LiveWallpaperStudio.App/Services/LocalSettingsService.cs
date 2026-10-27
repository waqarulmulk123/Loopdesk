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
    private static readonly object FileGate = new();
    public const string ProductionApiBaseUrl = "https://live-wallpaper-studio-api.mwaqardula.workers.dev";
    public bool Muted { get; set; } = true;
    public bool PauseOnFullscreen { get; set; } = true;
    public bool PauseOnLock { get; set; } = true;
    public bool PauseOnDisplayOff { get; set; } = true;
    public bool PauseOnBatterySaver { get; set; } = true;
    public bool SendUsageAnalytics { get; set; } = false;
    public bool SendCrashReports { get; set; } = false;
    public bool HasCompletedOnboarding { get; set; } = false;
    public bool LowPowerMode { get; set; } = false;
    public string? AppliedWallpaperId { get; set; }

    public static string FilePath => Path.Combine(
        Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),
        "Loopdesk", "settings.json");

    public void Save()
    {
        lock (FileGate) SaveCore();
    }

    public static void Update(Action<LocalSettingsService> update)
    {
        ArgumentNullException.ThrowIfNull(update);
        lock (FileGate)
        {
            LocalSettingsService settings = LoadCore();
            update(settings);
            settings.SaveCore();
        }
    }

    private void SaveCore()
    {
        Directory.CreateDirectory(Path.GetDirectoryName(FilePath)!);
        string temporaryPath = FilePath + ".tmp";
        File.WriteAllText(temporaryPath, JsonSerializer.Serialize(this, new JsonSerializerOptions { WriteIndented = true }));
        File.Move(temporaryPath, FilePath, overwrite: true);
    }

    public static LocalSettingsService Load()
    {
        lock (FileGate) return LoadCore();
    }

    private static LocalSettingsService LoadCore()
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
