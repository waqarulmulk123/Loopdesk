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
    // ponytail: one process-wide vault lock; use a named OS mutex only if a second app instance is supported.
    private static readonly SemaphoreSlim IndexGate = new(1, 1);

    public static DownloadedWallpaperService Instance { get; } = new();

    private DownloadedWallpaperService() { }

    private static string Root => AppContext.GetData("Loopdesk.VaultRoot") as string ?? Path.Combine(
        Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData), "Loopdesk");
    private static string VaultDirectory => Path.Combine(Root, "vault");
    private static string PlaybackDirectory => Path.Combine(Root, "playback");
    private static string IndexPath => Path.Combine(Root, "downloads.json");

    public IReadOnlyList<DownloadedWallpaper> List()
    {
        IndexGate.Wait();
        try
        {
            return ListCore();
        }
        finally
        {
            IndexGate.Release();
        }
    }

    public async Task SaveAsync(string verifiedVideoPath, string id, string title, CancellationToken cancellationToken = default)
    {
        if (!File.Exists(verifiedVideoPath)) throw new FileNotFoundException("Verified download was not found.", verifiedVideoPath);
        if (!IsSafeId(id)) throw new InvalidOperationException("The cloud catalog returned an invalid wallpaper identifier.");

        byte[] clear = await File.ReadAllBytesAsync(verifiedVideoPath, cancellationToken);
        byte[] protectedBytes = ProtectedData.Protect(clear, Entropy, DataProtectionScope.CurrentUser);
        await IndexGate.WaitAsync(cancellationToken);
        try
        {
            Directory.CreateDirectory(VaultDirectory);
            Directory.CreateDirectory(Path.GetDirectoryName(IndexPath)!);
            string vaultPath = GetVaultPath(id);
            string temporaryPath = vaultPath + ".partial";
            await File.WriteAllBytesAsync(temporaryPath, protectedBytes, cancellationToken);
            File.Move(temporaryPath, vaultPath, overwrite: true);
            File.Delete(verifiedVideoPath);

            var items = ListCore().Where(item => !string.Equals(item.Id, id, StringComparison.Ordinal)).ToList();
            items.Add(new DownloadedWallpaper(id, title, DateTimeOffset.UtcNow));
            await WriteIndexAsync(items, cancellationToken);
        }
        finally
        {
            IndexGate.Release();
        }
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
        IndexGate.Wait();
        try
        {
            File.Delete(GetVaultPath(wallpaper.Id));
            var remaining = ListCore().Where(item => !string.Equals(item.Id, wallpaper.Id, StringComparison.Ordinal)).ToList();
            WriteIndexAsync(remaining, CancellationToken.None).GetAwaiter().GetResult();
            LocalSettingsService.Update(settings =>
            {
                if (string.Equals(settings.AppliedWallpaperId, wallpaper.Id, StringComparison.Ordinal))
                    settings.AppliedWallpaperId = null;
            });
        }
        finally
        {
            IndexGate.Release();
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

    private static IReadOnlyList<DownloadedWallpaper> ListCore()
    {
        try
        {
            if (!File.Exists(IndexPath)) return RebuildIndexFromVault();
            return JsonSerializer.Deserialize<List<DownloadedWallpaper>>(File.ReadAllText(IndexPath))
                ?.Where(item => IsSafeId(item.Id) && File.Exists(GetVaultPath(item.Id)))
                .OrderByDescending(item => item.DownloadedAt)
                .ToArray() ?? [];
        }
        catch (JsonException)
        {
            string backupPath = Path.Combine(Root, $"downloads.{DateTimeOffset.UtcNow:yyyyMMddHHmmssfff}.bad");
            try { File.Move(IndexPath, backupPath, overwrite: true); } catch (IOException) { }
            IReadOnlyList<DownloadedWallpaper> rebuilt = RebuildIndexFromVault();
            WriteIndex(rebuilt);
            return rebuilt;
        }
    }

    private static IReadOnlyList<DownloadedWallpaper> RebuildIndexFromVault()
    {
        if (!Directory.Exists(VaultDirectory)) return [];
        return Directory.EnumerateFiles(VaultDirectory, "*.lwp", SearchOption.TopDirectoryOnly)
            .Select(path => new FileInfo(path))
            .Where(file => file.Length > 0 && IsSafeId(Path.GetFileNameWithoutExtension(file.Name)))
            .Select(file => new DownloadedWallpaper(Path.GetFileNameWithoutExtension(file.Name), "Recovered wallpaper", file.LastWriteTimeUtc))
            .OrderByDescending(item => item.DownloadedAt)
            .ToArray();
    }

    private static void WriteIndex(IEnumerable<DownloadedWallpaper> items)
    {
        Directory.CreateDirectory(Path.GetDirectoryName(IndexPath)!);
        string temporaryPath = IndexPath + ".partial";
        File.WriteAllText(temporaryPath, JsonSerializer.Serialize(items, JsonOptions));
        File.Move(temporaryPath, IndexPath, overwrite: true);
    }

    private static async Task WriteIndexAsync(IEnumerable<DownloadedWallpaper> items, CancellationToken cancellationToken)
    {
        Directory.CreateDirectory(Path.GetDirectoryName(IndexPath)!);
        string temporaryPath = IndexPath + ".partial";
        await File.WriteAllTextAsync(temporaryPath, JsonSerializer.Serialize(items, JsonOptions), cancellationToken);
        File.Move(temporaryPath, IndexPath, overwrite: true);
    }

    private static bool IsSafeId(string id) => !string.IsNullOrWhiteSpace(id) &&
        id.All(character => char.IsAsciiLetterOrDigit(character) || character is '-' or '_');
}

public sealed record DownloadedWallpaper(string Id, string Title, DateTimeOffset DownloadedAt)
{
    public string Subtitle => $"Downloaded {DownloadedAt.ToLocalTime():d}";
}
