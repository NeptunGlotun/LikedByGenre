using System.Net;
using System.Net.Http.Headers;
using System.Text;
using System.Text.Json;
using System.Text.Json.Serialization;
using LikedByGenre.Options;
using Microsoft.Extensions.Options;

namespace LikedByGenre.Services;

public class SpotifyApiClient
{
    private static readonly JsonSerializerOptions JsonOptions = new()
    {
        PropertyNameCaseInsensitive = true
    };

    private readonly HttpClient _http;
    private readonly SpotifyOptions _options;

    public SpotifyApiClient(HttpClient http, IOptions<SpotifyOptions> options)
    {
        _http = http;
        _options = options.Value;
        _http.BaseAddress = new Uri("https://api.spotify.com/v1/");
        _http.Timeout = TimeSpan.FromSeconds(60);
    }

    public async Task<SpotifySavedTracksPage> GetSavedTracksAsync(string accessToken, int offset, CancellationToken cancellationToken)
    {
        using var response = await SendWithRetryAsync(
            () =>
            {
                var request = new HttpRequestMessage(HttpMethod.Get, $"me/tracks?limit=50&offset={offset}");
                request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", accessToken);
                return request;
            },
            cancellationToken);

        var json = await response.Content.ReadAsStringAsync(cancellationToken);
        return JsonSerializer.Deserialize<SpotifySavedTracksPage>(json, JsonOptions)
               ?? new SpotifySavedTracksPage();
    }

    public async Task<SpotifyArtistDto?> GetArtistAsync(string accessToken, string artistId, CancellationToken cancellationToken)
    {
        using var response = await SendWithRetryAsync(
            () =>
            {
                var request = new HttpRequestMessage(HttpMethod.Get, $"artists/{artistId}");
                request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", accessToken);
                return request;
            },
            cancellationToken);

        var json = await response.Content.ReadAsStringAsync(cancellationToken);
        return JsonSerializer.Deserialize<SpotifyArtistDto>(json, JsonOptions);
    }

    public async Task<string> CreatePlaylistAsync(string accessToken, string name, string description, CancellationToken cancellationToken)
    {
        var body = JsonSerializer.Serialize(new
        {
            name,
            description,
            @public = false
        });

        using var response = await SendWithRetryAsync(
            () =>
            {
                var request = new HttpRequestMessage(HttpMethod.Post, "me/playlists")
                {
                    Content = new StringContent(body, Encoding.UTF8, "application/json")
                };
                request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", accessToken);
                return request;
            },
            cancellationToken);

        using var document = JsonDocument.Parse(await response.Content.ReadAsStringAsync(cancellationToken));
        return document.RootElement.GetProperty("id").GetString()
               ?? throw new InvalidOperationException("Spotify не повернув id плейлиста.");
    }

    public async Task AddPlaylistItemsAsync(string accessToken, string playlistId, IReadOnlyList<string> uris, CancellationToken cancellationToken)
    {
        foreach (var chunk in uris.Chunk(100))
        {
            var body = JsonSerializer.Serialize(new { uris = chunk });
            using var response = await SendWithRetryAsync(
                () =>
                {
                    var request = new HttpRequestMessage(HttpMethod.Post, $"playlists/{playlistId}/items")
                    {
                        Content = new StringContent(body, Encoding.UTF8, "application/json")
                    };
                    request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", accessToken);
                    return request;
                },
                cancellationToken);
        }
    }

    public async Task<string> RefreshAccessTokenAsync(string refreshToken, CancellationToken cancellationToken)
    {
        using var request = new HttpRequestMessage(HttpMethod.Post, "https://accounts.spotify.com/api/token");
        var pairs = new List<KeyValuePair<string, string>>
        {
            new("grant_type", "refresh_token"),
            new("refresh_token", refreshToken),
            new("client_id", _options.ClientId)
        };
        if (!string.IsNullOrWhiteSpace(_options.ClientSecret))
        {
            pairs.Add(new("client_secret", _options.ClientSecret));
        }

        request.Content = new FormUrlEncodedContent(pairs);
        using var response = await _http.SendAsync(request, cancellationToken);
        var json = await response.Content.ReadAsStringAsync(cancellationToken);
        if (!response.IsSuccessStatusCode)
        {
            throw new InvalidOperationException("Не вдалося оновити Spotify access token. Увійдіть знову.");
        }

        using var document = JsonDocument.Parse(json);
        return document.RootElement.GetProperty("access_token").GetString()
               ?? throw new InvalidOperationException("Відповідь Spotify не містить access_token.");
    }

    private async Task<HttpResponseMessage> SendWithRetryAsync(Func<HttpRequestMessage> factory, CancellationToken cancellationToken)
    {
        for (var attempt = 0; attempt < 5; attempt++)
        {
            using var request = factory();
            var response = await _http.SendAsync(request, cancellationToken);
            if (response.StatusCode == (HttpStatusCode)429)
            {
                var delay = TimeSpan.FromSeconds(2 * (attempt + 1));
                if (response.Headers.RetryAfter?.Delta is { } retryAfter)
                {
                    delay = retryAfter;
                }

                response.Dispose();
                await Task.Delay(delay, cancellationToken);
                continue;
            }

            if (!response.IsSuccessStatusCode)
            {
                var error = await response.Content.ReadAsStringAsync(cancellationToken);
                response.Dispose();
                throw new InvalidOperationException($"Spotify API {response.StatusCode}: {TrimError(error)}");
            }

            return response;
        }

        throw new InvalidOperationException("Spotify API відхилив запит через обмеження частоти (429). Спробуйте пізніше.");
    }

    private static string TrimError(string error) =>
        error.Length <= 300 ? error : error[..300];
}

public class SpotifySavedTracksPage
{
    [JsonPropertyName("total")]
    public int Total { get; set; }

    [JsonPropertyName("items")]
    public List<SpotifySavedTrackItem> Items { get; set; } = new();
}

public class SpotifySavedTrackItem
{
    [JsonPropertyName("added_at")]
    public DateTime AddedAt { get; set; }

    [JsonPropertyName("track")]
    public SpotifyTrackDto? Track { get; set; }
}

public class SpotifyTrackDto
{
    [JsonPropertyName("id")]
    public string? Id { get; set; }

    [JsonPropertyName("uri")]
    public string? Uri { get; set; }

    [JsonPropertyName("name")]
    public string? Name { get; set; }

    [JsonPropertyName("duration_ms")]
    public int DurationMs { get; set; }

    [JsonPropertyName("album")]
    public SpotifyAlbumDto? Album { get; set; }

    [JsonPropertyName("artists")]
    public List<SpotifyArtistRefDto> Artists { get; set; } = new();
}

public class SpotifyAlbumDto
{
    [JsonPropertyName("name")]
    public string? Name { get; set; }
}

public class SpotifyArtistRefDto
{
    [JsonPropertyName("id")]
    public string? Id { get; set; }

    [JsonPropertyName("name")]
    public string? Name { get; set; }
}

public class SpotifyArtistDto
{
    [JsonPropertyName("id")]
    public string? Id { get; set; }

    [JsonPropertyName("name")]
    public string? Name { get; set; }

    [JsonPropertyName("genres")]
    public List<string>? Genres { get; set; }
}
