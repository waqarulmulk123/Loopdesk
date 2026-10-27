using LiveWallpaperStudio.App.Services;
using LiveWallpaperStudio.App.ViewModels;

static void Check(bool condition, string message)
{
    if (!condition) throw new Exception(message);
    Console.WriteLine($"PASS {message}");
}

// Keep the request pending to reproduce the first rendered frame, before HTTP completes.
OnlineCatalogService.Response = new();
var loading = new HomeViewModel();
Check(loading.FeaturedPosterUri is null, "Loading hero has no invalid empty image URI");
OnlineCatalogService.Response.SetResult([]);
Check(loading.FeaturedPosterUri is null && loading.Items.Count == 0, "Empty catalog remains safe to render");

OnlineCatalogService.Response = new();
var offline = new HomeViewModel();
OnlineCatalogService.Response.SetException(new HttpRequestException("Offline"));
Check(offline.FeaturedPosterUri is null, "Offline catalog remains safe to render");

OnlineCatalogService.Response = new();
var populated = new HomeViewModel();
OnlineCatalogService.Response.SetResult([
    new("theme-loopdesk-aurora-dreamscape", "Aurora", "Cloud wallpaper", ["Nature"], 1080, false)
]);
Check(populated.FeaturedTitle == "Aurora", "Catalog supplies the featured title");
Check(populated.FeaturedPosterUri is { IsAbsoluteUri: true }, "Loaded hero exposes a typed absolute image URI");
Check(populated.Items.Count == 1, "Catalog supplies collection cards");

namespace LiveWallpaperStudio.App.Services
{
    public sealed class OnlineCatalogService
    {
        public static TaskCompletionSource<IReadOnlyList<OnlineCatalogItem>> Response { get; set; } = new();
        public Task<IReadOnlyList<OnlineCatalogItem>> LoadAsync(string _) => Response.Task;
    }
    public static class LocalSettingsService { public const string ProductionApiBaseUrl = "https://example.invalid"; }
    public sealed record OnlineCatalogItem(string Id, string Title, string? Description, string[]? Categories, int? Height, bool Premium);
}
