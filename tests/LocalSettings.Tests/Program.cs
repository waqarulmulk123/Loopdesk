using System.Text.Json;
using LiveWallpaperStudio.App.Services;

var saved = new LocalSettingsService { AppliedWallpaperId = "theme-loopdesk-aurora-dreamscape" };
var restored = JsonSerializer.Deserialize<LocalSettingsService>(JsonSerializer.Serialize(saved));
if (restored?.AppliedWallpaperId != saved.AppliedWallpaperId) throw new Exception("Active wallpaper selection was not persisted.");
Console.WriteLine("PASS active wallpaper selection survives settings serialization");
