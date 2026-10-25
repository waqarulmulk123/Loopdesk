// M0 test harness: a small console runner with real assertions.
// Exit code 0 = all pass, 1 = any failure. No test framework dependency
// in M0, so the Windows CI build cannot fail on a NuGet restore issue.
using LiveWallpaperStudio.Core;

var failures = 0;

void Check(bool condition, string name)
{
    Console.WriteLine($"{(condition ? "PASS" : "FAIL")} {name}");
    if (!condition)
    {
        failures++;
    }
}

Check(IpcValidation.ProtocolVersion == 1, "IPC protocol version is 1");
Check(IpcValidation.MaxMessageBytes == 1024 * 1024, "IPC max message is 1 MiB");
Check(IpcValidation.IsValidMethod("wallpaper.apply"), "wallpaper.apply is a known method");
Check(!IpcValidation.IsValidMethod("wallpaper.deleteEverything"), "unknown method is rejected");
Check(!IpcValidation.IsAllowedLocalPath(@"\\server\share\evil.mp4"), "UNC path is rejected");
Check(!IpcValidation.IsAllowedLocalPath("https://example.com/video.mp4"), "URL path is rejected");
Check(IpcValidation.IsAllowedLocalPath(@"C:\Users\Example\Videos\loop.mp4"), "local path is allowed");

var envelope = new IpcEnvelope(1, Guid.NewGuid().ToString(), "request", "engine.status", null);
Check(IpcValidation.IsValidEnvelope(envelope, 128), "valid envelope passes");
Check(!IpcValidation.IsValidEnvelope(envelope with { ProtocolVersion = 2 }, 128), "wrong protocol version is rejected");
Check(!IpcValidation.IsValidEnvelope(envelope with { Kind = "response" }, 128), "response envelopes are rejected as renderer requests");
Check(!IpcValidation.IsValidEnvelope(envelope, IpcValidation.MaxMessageBytes + 1), "oversized message is rejected");

var variant = new MediaVariant("v1", "video/mp4", "h264", 1920, 1080, 30, 1000, new string('0', 64), null);
Check(variant.Width == 1920 && variant.Codec == "h264", "media variant record holds values");

var settings = AppSettings.Default;
Check(settings.Playback.MuteByDefault, "audio is muted by default");
Check(!settings.Privacy.SendCrashReports && !settings.Privacy.SendUsageAnalytics, "telemetry is off by default");
Check(!settings.CatalogEnabled && settings.ApiBaseUrl is null, "catalog is off by default, no fake content");

Check(DownloadManager.ComputeSha256Hex(System.Text.Encoding.UTF8.GetBytes("abc")) ==
    "ba7816bf8f01cfea414140de5dae2223b00361a396177a9cb410ff61f20015ad",
    "SHA-256 of 'abc' matches the known test vector");
Check(CacheService.CacheFileName("abc123", ".mp4") == "abc123.mp4", "cache file name is sha256 + extension");

var downloadRoot = Path.Combine(Path.GetTempPath(), "LiveWallpaperStudio-tests", Guid.NewGuid().ToString("N"));
Directory.CreateDirectory(downloadRoot);
try
{
    string tempFile = Path.Combine(downloadRoot, "video.partial");
    string finalFile = Path.Combine(downloadRoot, "video.mp4");
    await File.WriteAllTextAsync(finalFile, "old");
    byte[] downloaded = System.Text.Encoding.UTF8.GetBytes("new-video");
    using var http = new HttpClient(new StaticResponseHandler(downloaded));
    var manager = new DownloadManager(http);
    var result = await manager.DownloadAsync("https://example.invalid/video.mp4", tempFile, finalFile,
        DownloadManager.ComputeSha256Hex(downloaded), downloaded.Length);
    Check(result.Success && await File.ReadAllTextAsync(finalFile) == "new-video" && !File.Exists(tempFile),
        "download atomically replaces an existing cache file without deleting it first");

    await File.WriteAllTextAsync(tempFile, "stale-partial");
    var restarted = await manager.DownloadAsync("https://example.invalid/video.mp4", tempFile, finalFile,
        DownloadManager.ComputeSha256Hex(downloaded), downloaded.Length);
    Check(restarted.Success && await File.ReadAllTextAsync(finalFile) == "new-video" && !File.Exists(tempFile),
        "download replaces a stale partial file when the origin ignores its range");

    var rejected = await manager.DownloadAsync("https://example.invalid/video.mp4", tempFile, finalFile,
        new string('0', 64), downloaded.Length);
    Check(!rejected.Success && !File.Exists(tempFile), "checksum mismatch removes the rejected partial file");
}
finally
{
    if (Directory.Exists(downloadRoot)) Directory.Delete(downloadRoot, recursive: true);
}

var entries = new[]
{
    new CacheService.CacheEntry("old", 100, new DateTimeOffset(2026, 1, 1, 0, 0, 0, TimeSpan.Zero), false, false, false),
    new CacheService.CacheEntry("pinned", 100, new DateTimeOffset(2026, 1, 1, 0, 0, 0, TimeSpan.Zero), true, false, false),
    new CacheService.CacheEntry("active", 100, new DateTimeOffset(2026, 1, 1, 0, 0, 0, TimeSpan.Zero), false, true, false),
    new CacheService.CacheEntry("imported", 100, new DateTimeOffset(2026, 1, 1, 0, 0, 0, TimeSpan.Zero), false, false, true),
    new CacheService.CacheEntry("new", 100, new DateTimeOffset(2026, 1, 2, 0, 0, 0, TimeSpan.Zero), false, false, false),
};
var evict = CacheService.SelectEvictionCandidates(entries, 100);
Check(evict.Count == 1 && evict[0].Sha256 == "old", "LRU eviction picks oldest unpinned, inactive, non-imported entry only");

Console.WriteLine(failures == 0 ? "ALL TESTS PASSED" : $"{failures} TEST(S) FAILED");
return failures == 0 ? 0 : 1;

sealed class StaticResponseHandler(byte[] content) : HttpMessageHandler
{
    protected override Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken) =>
        Task.FromResult(new HttpResponseMessage(System.Net.HttpStatusCode.OK)
        {
            Content = new ByteArrayContent(content),
        });
}
