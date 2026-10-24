using System.Text.Json;

namespace LiveWallpaperStudio.App.Services;

/// <summary>
/// Loads the bundled catalog (assets/catalog.json) when it is present
/// next to the app, in the project tree, or in LocalAppData. Returns
/// an empty list when no catalog is found — the UI then says the
/// catalog is not configured, instead of showing fake content.
/// </summary>
public static class CatalogService
{
    public sealed record CatalogItem(string Id, string Title, string Video, string? Poster, bool Premium, string Theme, string CatalogPath)
    {
        public string? LocalVideoPath => ResolveLocalAsset(Video, CatalogPath);
    }

    public static List<CatalogItem> Load()
    {
        string[] candidates =
        {
            Path.Combine(AppContext.BaseDirectory, "assets", "catalog.json"),
            FindProjectAsset("assets", "catalog.json"),
            Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData), "LiveWallpaperStudio", "catalog.json"),
        };
        foreach (string path in candidates)
        {
            try
            {
                if (!File.Exists(path)) continue;
                using JsonDocument doc = JsonDocument.Parse(File.ReadAllText(path));
                var items = new List<CatalogItem>();
                foreach (JsonElement el in doc.RootElement.GetProperty("items").EnumerateArray())
                {
                    items.Add(new CatalogItem(
                        el.GetProperty("id").GetString() ?? string.Empty,
                        el.GetProperty("title").GetString() ?? string.Empty,
                        el.GetProperty("video").GetString() ?? string.Empty,
                        el.TryGetProperty("poster", out JsonElement poster) && poster.ValueKind == JsonValueKind.String ? poster.GetString() : null,
                        el.TryGetProperty("premium", out JsonElement premium) && premium.GetBoolean(),
                        el.TryGetProperty("theme", out JsonElement theme) ? theme.GetString() ?? string.Empty : string.Empty,
                        path));
                }
                return items;
            }
            catch
            {
                // A corrupt catalog must not crash the app; try the next candidate.
            }
        }
        return new List<CatalogItem>();
    }

    private static string? ResolveLocalAsset(string relativePath, string catalogPath)
    {
        if (string.IsNullOrWhiteSpace(relativePath) || Path.IsPathFullyQualified(relativePath))
        {
            return File.Exists(relativePath) ? relativePath : null;
        }

        // Catalog entries are rooted at the deployed app/project root (assets/...)
        // rather than at catalog.json's directory (assets/).
        string catalogRoot = Directory.GetParent(Path.GetDirectoryName(catalogPath) ?? string.Empty)?.FullName ?? string.Empty;
        string[] candidates =
        {
            Path.Combine(AppContext.BaseDirectory, relativePath),
            Path.Combine(catalogRoot, relativePath),
            FindProjectAsset(relativePath),
        };
        return candidates.FirstOrDefault(File.Exists);
    }

    private static string FindProjectAsset(params string[] parts)
    {
        for (DirectoryInfo? current = new(AppContext.BaseDirectory); current is not null; current = current.Parent)
        {
            string candidate = Path.Combine([current.FullName, .. parts]);
            if (File.Exists(candidate)) return candidate;
        }
        return string.Empty;
    }
}
