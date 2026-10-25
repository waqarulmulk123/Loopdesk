using System.Net.Http.Json;
using System.Text.Json.Serialization;

namespace LiveWallpaperStudio.App.Services;

public sealed class OnlineCatalogService
{
    private readonly HttpClient _http;

    public OnlineCatalogService(HttpClient? http = null) => _http = http ?? new HttpClient { Timeout = TimeSpan.FromSeconds(15) };

    public async Task<IReadOnlyList<OnlineCatalogItem>> LoadAsync(string baseUrl, CancellationToken cancellationToken = default)
    {
        if (!Uri.TryCreate(baseUrl.TrimEnd('/') + "/", UriKind.Absolute, out var baseUri) || baseUri.Scheme != Uri.UriSchemeHttps)
            throw new InvalidOperationException("Online catalog URL must use HTTPS.");

        var response = await _http.GetFromJsonAsync<CatalogResponse>(new Uri(baseUri, "v1/wallpapers?limit=100"), cancellationToken);
        return response?.Items ?? [];
    }

    public async Task<OnlineDownload> GetDownloadAsync(string baseUrl, string wallpaperId, CancellationToken cancellationToken = default)
    {
        Uri baseUri = ValidateBaseUrl(baseUrl);
        var detail = await _http.GetFromJsonAsync<WallpaperDetail>(new Uri(baseUri, $"v1/wallpapers/{Uri.EscapeDataString(wallpaperId)}"), cancellationToken)
            ?? throw new InvalidOperationException("Wallpaper details were not returned.");
        if (detail.Item.Premium)
            throw new InvalidOperationException("Premium wallpapers require an entitlement provider before download.");

        OnlineVariant? variant = detail.Variants
            .Where(candidate => string.Equals(candidate.MimeType, "video/mp4", StringComparison.OrdinalIgnoreCase) &&
                                string.Equals(candidate.Codec, "h264", StringComparison.OrdinalIgnoreCase))
            .OrderByDescending(candidate => candidate.Width * (long)candidate.Height)
            .FirstOrDefault();
        if (variant is null)
            throw new InvalidOperationException("No downloadable H.264 MP4 variant is available.");

        var authorization = await _http.PostAsync(
            new Uri(baseUri, $"v1/assets/{Uri.EscapeDataString(variant.Id)}/authorize-download"),
            content: null,
            cancellationToken);
        authorization.EnsureSuccessStatusCode();
        var ticket = await authorization.Content.ReadFromJsonAsync<DownloadAuthorization>(cancellationToken)
            ?? throw new InvalidOperationException("Download authorization was not returned.");
        if (!Uri.TryCreate(ticket.DownloadUrl, UriKind.Absolute, out Uri? downloadUri) || downloadUri.Scheme != Uri.UriSchemeHttps)
            throw new InvalidOperationException("Download URL must use HTTPS.");

        return new OnlineDownload(detail.Item, variant, downloadUri);
    }

    private static Uri ValidateBaseUrl(string baseUrl)
    {
        if (!Uri.TryCreate(baseUrl.TrimEnd('/') + "/", UriKind.Absolute, out var baseUri) || baseUri.Scheme != Uri.UriSchemeHttps)
            throw new InvalidOperationException("Online catalog URL must use HTTPS.");
        return baseUri;
    }

    private sealed record CatalogResponse(List<OnlineCatalogItem>? Items);
    private sealed record WallpaperDetail(OnlineCatalogItem Item, List<OnlineVariant> Variants);
    private sealed record DownloadAuthorization(string DownloadUrl);
}

public sealed record OnlineCatalogItem(
    string Id,
    string Title,
    string? Description,
    string Kind,
    bool Premium,
    [property: JsonPropertyName("created_at")] string CreatedAt,
    List<string>? Categories = null,
    int? Width = null,
    int? Height = null);

public sealed record OnlineVariant(
    string Id,
    [property: JsonPropertyName("variant_key")] string VariantKey,
    [property: JsonPropertyName("mime_type")] string MimeType,
    string Codec,
    int Width,
    int Height,
    int Fps,
    [property: JsonPropertyName("size_bytes")] long SizeBytes,
    string Sha256);
public sealed record OnlineDownload(OnlineCatalogItem Item, OnlineVariant Variant, Uri DownloadUri);
