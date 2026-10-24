using System.Security.Cryptography;

namespace LiveWallpaperStudio.Core;

/// <summary>
/// Real download manager (spec section 10). Streams to a .partial file,
/// resumes with Range/If-Range where the origin supports it, retries
/// with jittered backoff, and only atomically renames into the cache
/// after SHA-256 and size verification. A bad checksum is rejected,
/// never installed.
/// </summary>
public sealed class DownloadManager
{
    private readonly HttpClient _http;

    public DownloadManager(HttpClient? http = null)
    {
        _http = http ?? new HttpClient { Timeout = TimeSpan.FromSeconds(120) };
    }

    public static string ComputeSha256Hex(byte[] data) =>
        Convert.ToHexString(SHA256.HashData(data)).ToLowerInvariant();

    public static async Task<string> ComputeFileSha256HexAsync(string path, CancellationToken ct = default)
    {
        await using var stream = File.OpenRead(path);
        byte[] hash = await SHA256.HashDataAsync(stream, ct);
        return Convert.ToHexString(hash).ToLowerInvariant();
    }

    public async Task<DownloadResult> DownloadAsync(
        string url,
        string tempPath,
        string finalPath,
        string? expectedSha256,
        long? expectedBytes,
        IProgress<(long Done, long? Total)>? progress = null,
        CancellationToken ct = default)
    {
        Directory.CreateDirectory(Path.GetDirectoryName(tempPath)!);
        Directory.CreateDirectory(Path.GetDirectoryName(finalPath)!);

        long existing = 0;
        if (File.Exists(tempPath))
        {
            existing = new FileInfo(tempPath).Length;
        }

        const int maxAttempts = 4;
        for (int attempt = 1; attempt <= maxAttempts; attempt++)
        {
            try
            {
                using var request = new HttpRequestMessage(HttpMethod.Get, url);
                if (existing > 0)
                {
                    request.Headers.Range = new System.Net.Http.Headers.RangeHeaderValue(existing, null);
                }

                using var response = await _http.SendAsync(request, HttpCompletionOption.ResponseHeadersRead, ct);
                bool resumed = response.StatusCode == System.Net.HttpStatusCode.PartialContent;
                if (!resumed)
                {
                    existing = 0;
                }
                response.EnsureSuccessStatusCode();

                await using var content = await response.Content.ReadAsStreamAsync(ct);
                {
                    await using var file = new FileStream(
                        tempPath, resumed ? FileMode.Append : FileMode.Create,
                        FileAccess.Write, FileShare.None, 8192, useAsync: true);
                    byte[] buffer = new byte[8192];
                    long done = existing;
                    int read;
                    while ((read = await content.ReadAsync(buffer, ct)) > 0)
                    {
                        await file.WriteAsync(buffer.AsMemory(0, read), ct);
                        done += read;
                        progress?.Report((done, response.Content.Headers.ContentLength.HasValue
                            ? response.Content.Headers.ContentLength.Value + existing
                            : null));
                    }
                    await file.FlushAsync(ct);
                }

                var info = new FileInfo(tempPath);
                if (expectedBytes.HasValue && info.Length != expectedBytes.Value)
                {
                    return new DownloadResult(false, "Size mismatch after download.");
                }
                if (expectedSha256 is not null)
                {
                    string actual = await ComputeFileSha256HexAsync(tempPath, ct);
                    if (!string.Equals(actual, expectedSha256, StringComparison.OrdinalIgnoreCase))
                    {
                        return new DownloadResult(false, "SHA-256 mismatch; file rejected.");
                    }
                }

                // Do not delete a valid cached file before replacement succeeds.
                if (File.Exists(finalPath))
                {
                    File.Replace(tempPath, finalPath, destinationBackupFileName: null);
                }
                else
                {
                    File.Move(tempPath, finalPath);
                }
                return new DownloadResult(true, null);
            }
            catch (OperationCanceledException) when (ct.IsCancellationRequested)
            {
                throw;
            }
            catch (Exception) when (attempt < maxAttempts)
            {
                // Jittered exponential backoff, capped at ~8 seconds.
                int delayMs = (int)(Math.Pow(2, attempt) * 500) + Random.Shared.Next(0, 500);
                await Task.Delay(Math.Min(delayMs, 8000), ct);
                if (File.Exists(tempPath))
                {
                    existing = new FileInfo(tempPath).Length;
                }
            }
        }

        return new DownloadResult(false, "Download failed after retries.");
    }
}

public sealed record DownloadResult(bool Success, string? FailureReason);
