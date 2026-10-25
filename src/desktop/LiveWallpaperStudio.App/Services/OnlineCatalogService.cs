using System.Net.Http.Json;

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

    private sealed record CatalogResponse(List<OnlineCatalogItem>? Items);
}

public sealed record OnlineCatalogItem(string Id, string Title, string? Description, string Kind, bool Premium, string Created_At);
