using System.Net;
using System.Net.Http.Headers;
using System.Text.Json;
using System.Text.Json.Serialization;
using Microsoft.Extensions.Options;
using RePlay.Server.Configuration;
using RePlay.Server.Models;

namespace RePlay.Server.Services;

/// <summary>
/// Service for Setlist.fm API interactions.
/// </summary>
public interface ISetlistFmService
{
    /// <summary>
    /// Validate a Setlist.fm username or ID and fetch profile information.
    /// </summary>
    Task<SetlistUser?> GetUserAsync(string usernameOrId, CancellationToken cancellationToken = default);
}

/// <summary>
/// Implementation of Setlist.fm API service.
/// </summary>
public sealed class SetlistFmService : ISetlistFmService
{
    private static readonly JsonSerializerOptions SerializerOptions = new();

    private readonly HttpClient _httpClient;
    private readonly SetlistFmOptions _options;

    public SetlistFmService(HttpClient httpClient, IOptions<SetlistFmOptions> options)
    {
        _httpClient = httpClient;
        _options = options.Value;

        if (string.IsNullOrWhiteSpace(_options.ApiKey))
        {
            throw new InvalidOperationException(
                "Setlist.fm API key is not configured. Please set 'SetlistFm:ApiKey' in appsettings.");
        }

        if (_httpClient.BaseAddress == null)
        {
            _httpClient.BaseAddress = new Uri(_options.ApiUrl.TrimEnd('/') + "/");
        }
    }

    public async Task<SetlistUser?> GetUserAsync(string usernameOrId, CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(usernameOrId))
        {
            throw new ArgumentException("Username or ID cannot be empty", nameof(usernameOrId));
        }

        using var request = CreateRequest($"user/{Uri.EscapeDataString(usernameOrId)}");
        var response = await _httpClient.SendAsync(request, cancellationToken).ConfigureAwait(false);

        if (response.StatusCode == HttpStatusCode.NotFound)
        {
            return null;
        }

        if (!response.IsSuccessStatusCode)
        {
            return null;
        }

        await using var stream = await response.Content.ReadAsStreamAsync(cancellationToken).ConfigureAwait(false);
        var payload = await JsonSerializer.DeserializeAsync<SetlistUserResponse>(stream, SerializerOptions, cancellationToken)
            .ConfigureAwait(false);

        if (payload?.UserId is null)
        {
            return null;
        }

        // Fetch attended concerts count from the attended endpoint
        var attendedCount = await GetAttendedConcertsCountAsync(payload.UserId, cancellationToken).ConfigureAwait(false);

        return new SetlistUser
        {
            UserId = payload.UserId,
            DisplayName = string.IsNullOrWhiteSpace(payload.FullName) ? payload.UserId : payload.FullName,
            Url = payload.Url,
            AttendedConcerts = attendedCount
        };
    }

    private async Task<int> GetAttendedConcertsCountAsync(string userId, CancellationToken cancellationToken)
    {
        try
        {
            using var request = CreateRequest($"user/{Uri.EscapeDataString(userId)}/attended?perPage=1");
            var response = await _httpClient.SendAsync(request, cancellationToken).ConfigureAwait(false);

            if (!response.IsSuccessStatusCode)
            {
                return 0;
            }

            await using var stream = await response.Content.ReadAsStreamAsync(cancellationToken).ConfigureAwait(false);
            var payload = await JsonSerializer.DeserializeAsync<SetlistAttendedResponse>(stream, SerializerOptions, cancellationToken)
                .ConfigureAwait(false);

            return payload?.Total ?? 0;
        }
        catch
        {
            // If attended endpoint fails, return 0
            return 0;
        }
    }

    private HttpRequestMessage CreateRequest(string relativePath)
    {
        var request = new HttpRequestMessage(HttpMethod.Get, relativePath);
        request.Headers.Accept.Add(new MediaTypeWithQualityHeaderValue("application/json"));
        request.Headers.UserAgent.ParseAdd(_options.UserAgent);
        request.Headers.Add("x-api-key", _options.ApiKey);
        return request;
    }

    private sealed record SetlistUserResponse
    {
        [JsonPropertyName("userId")]
        public string? UserId { get; init; }

        [JsonPropertyName("fullname")]
        public string? FullName { get; init; }

        [JsonPropertyName("url")]
        public string? Url { get; init; }
    }

    private sealed record SetlistAttendedResponse
    {
        [JsonPropertyName("setlist")]
        public List<SetlistItem>? Setlist { get; init; }

        [JsonPropertyName("total")]
        public int? Total { get; init; }

        [JsonPropertyName("page")]
        public int? Page { get; init; }

        [JsonPropertyName("itemsPerPage")]
        public int? ItemsPerPage { get; init; }

        public sealed record SetlistItem
        {
            [JsonPropertyName("id")]
            public string? Id { get; init; }
        }
    }
}
