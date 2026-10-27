using System.Security.Cryptography;
using System.Text.Json;

namespace LiveWallpaperStudio.App.Services;

/// <summary>
/// Private, per-user storage for Cloudflare downloads. The UI works with
/// metadata only: local filenames and paths are never shown or accepted as
/// wallpaper sources. Video bytes are protected at rest with Windows DPAPI
/// and are materialized only as a short-lived playback file for the native
/// renderer or preview control.
/// </summary>
public sealed class DownloadedWallpaperService
{
    private static readonly byte[] Entropy = SHA256.HashData("Loopdesk.MediaVault.v1"u8.ToArray());
    private static readonly JsonSerializerOptions JsonOptions = new() { WriteIndented = true };

    public static DownloadedWallpaperService Instance { get; } = new();

    private DownloadedWallpaperService() { }

    private static string Root => Path.Combine(
        Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData), "Loopdesk");
    private static string VaultDirectory => Path.Combine(Root, "vault");
    private static string PlaybackDirectory => Path.Combine(Root, "playback");
    private static string IndexPath => Path.Combine(Root, "downloads.json");

    public IReadOnlyList<DownloadedWallpaper> List()
    {
        try
        {
            if (!File.Exists(IndexPath)) return [];
            return JsonSerializer.Deserialize<List<DownloadedWallpaper>>(File.ReadAllText(IndexPath))
                ?.Where(item => IsSafeId(item.Id) && File.Exists(GetVaultPath(item.Id)))
                .OrderByDescending(item => item.DownloadedAt)
                .ToArray() ?? [];
        }
        catch (JsonException)
        {
            return [];
        }
    }

    public async Task SaveAsync(string verifiedVideoPath, string id, string title, CancellationToken cancellationToken = default)
    {
        if (!File.Exists(verifiedVideoPath)) throw new FileNotFoundException("Verified download was not found.", verifiedVideoPath);
        if (!IsSafeId(id)) throw new InvalidOperationException("The cloud catalog returned an invalid wallpaper identifier.");

        Directory.CreateDirectory(VaultDirectory);
        Directory.CreateDirectory(Path.GetDirectoryName(IndexPath)!);
        byte[] clear = await File.ReadAllBytesAsync(verifiedVideoPath, cancellationToken);
        byte[] protectedBytes = ProtectedData.Protect(clear, Entropy, DataProtectionScope.CurrentUser);
        string vaultPath = GetVaultPath(id);
        string temporaryPath = vaultPath + ".partial";
        await File.WriteAllBytesAsync(temporaryPath, protectedBytes, cancellationToken);
        File.Move(temporaryPath, vaultPath, overwrite: true);
        File.Delete(verifiedVideoPath);

        var items = List().Where(item => !string.Equals(item.Id, id, StringComparison.Ordinal)).ToList();
        items.Add(new DownloadedWallpaper(id, title, DateTimeOffset.UtcNow));
        string indexTemporaryPath = IndexPath + ".partial";
        await File.WriteAllTextAsync(indexTemporaryPath, JsonSerializer.Serialize(items, JsonOptions), cancellationToken);
        File.Move(indexTemporaryPath, IndexPath, overwrite: true);
    }

    public string CreatePlaybackCopy(DownloadedWallpaper wallpaper)
    {
        if (!IsSafeId(wallpaper.Id)) throw new InvalidOperationException("Invalid downloaded wallpaper identifier.");
        byte[] protectedBytes = File.ReadAllBytes(GetVaultPath(wallpaper.Id));
        byte[] clear = ProtectedData.Unprotect(protectedBytes, Entropy, DataProtectionScope.CurrentUser);
        Directory.CreateDirectory(PlaybackDirectory);
        CleanupPlaybackCopies();
        string path = Path.Combine(PlaybackDirectory, $"{wallpaper.Id}-{Guid.NewGuid():N}.mp4");
        File.WriteAllBytes(path, clear);
        File.SetAttributes(path, FileAttributes.Hidden | FileAttributes.Temporary);
        return path;
    }

    public void Remove(DownloadedWallpaper wallpaper)
    {
        if (!IsSafeId(wallpaper.Id)) throw new InvalidOperationException("Invalid downloaded wallpaper identifier.");
        File.Delete(GetVaultPath(wallpaper.Id));
        var remaining = List().Where(item => !string.Equals(item.Id, wallpaper.Id, StringComparison.Ordinal)).ToList();
        string temporaryPath = IndexPath + ".partial";
        File.WriteAllText(temporaryPath, JsonSerializer.Serialize(remaining, JsonOptions));
        File.Move(temporaryPath, IndexPath, overwrite: true);
        var settings = LocalSettingsService.Load();
        if (string.Equals(settings.AppliedWallpaperId, wallpaper.Id, StringComparison.Ordinal))
        {
            settings.AppliedWallpaperId = null;
            settings.Save();
        }
    }

    public void CleanupPlaybackCopies()
    {
        if (!Directory.Exists(PlaybackDirectory)) return;
        foreach (string file in Directory.EnumerateFiles(PlaybackDirectory, "*.mp4", SearchOption.TopDirectoryOnly))
        {
            try { File.Delete(file); } catch (IOException) { /* Active renderer owns this file. */ }
        }
    }

    private static string GetVaultPath(string id) => Path.Combine(VaultDirectory, id + ".lwp");

    private static bool IsSafeId(string id) => !string.IsNullOrWhiteSpace(id) &&
        id.All(character => char.IsAsciiLetterOrDigit(character) || character is '-' or '_');
}

public sealed record DownloadedWallpaper(string Id, string Title, DateTimeOffset DownloadedAt)
{
    public string Subtitle => $"Downloaded {DownloadedAt.ToLocalTime():d}";
}
