using System.Text.Json;

namespace LiveWallpaperStudio.App.Services;

/// <summary>
/// Local favourites, stored as JSON under LocalAppData. Favourites
/// are wallpaper IDs from the bundled catalog (or local file paths).
/// No account, no sync, no server — favourites work fully offline.
/// </summary>
public static class FavouritesService
{
    public static string FilePath => Path.Combine(
        Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),
        "LiveWallpaperStudio", "favourites.json");

    public static List<string> Load()
    {
        try
        {
            if (File.Exists(FilePath))
            {
                return JsonSerializer.Deserialize<List<string>>(File.ReadAllText(FilePath)) ?? new List<string>();
            }
        }
        catch
        {
            // Corrupt favourites must not prevent launch.
        }
        return new List<string>();
    }

    public static void Save(List<string> favourites)
    {
        Directory.CreateDirectory(Path.GetDirectoryName(FilePath)!);
        File.WriteAllText(FilePath, JsonSerializer.Serialize(favourites, new JsonSerializerOptions { WriteIndented = true }));
    }

    public static bool Toggle(string id)
    {
        var list = Load();
        bool added;
        if (list.Contains(id))
        {
            list.Remove(id);
            added = false;
        }
        else
        {
            list.Add(id);
            added = true;
        }
        Save(list);
        return added;
    }
}
