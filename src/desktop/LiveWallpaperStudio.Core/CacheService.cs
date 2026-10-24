namespace LiveWallpaperStudio.Core;

/// <summary>
/// Verified local cache rules (spec section 10). Files are keyed by
/// SHA-256. Eviction is LRU, and only for files that are not pinned,
/// not active, and not user-imported. This class is pure logic so it
/// can be tested without a filesystem; the filesystem eviction itself
/// is in the app's cache manager.
/// </summary>
public static class CacheService
{
    public sealed record CacheEntry(string Sha256, long SizeBytes, DateTimeOffset LastAccessedAt, bool Pinned, bool Active, bool UserImported);

    public static IReadOnlyList<CacheEntry> SelectEvictionCandidates(
        IReadOnlyList<CacheEntry> entries,
        long bytesToFree)
    {
        var candidates = entries
            .Where(e => !e.Pinned && !e.Active && !e.UserImported)
            .OrderBy(e => e.LastAccessedAt)
            .ToList();

        var selected = new List<CacheEntry>();
        long freed = 0;
        foreach (var entry in candidates)
        {
            if (freed >= bytesToFree) break;
            selected.Add(entry);
            freed += entry.SizeBytes;
        }
        return selected;
    }

    public static string CacheFileName(string sha256, string extension) =>
        $"{sha256}.{extension.TrimStart('.')}";
}
