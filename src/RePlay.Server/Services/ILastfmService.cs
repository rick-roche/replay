using System.Net.Http.Json;
using System.Text.Json;
using System.Text.Json.Serialization;
using Microsoft.Extensions.Options;
using RePlay.Server.Configuration;

namespace RePlay.Server.Services;

/// <summary>
/// Service for Last.fm API interactions.
/// </summary>
public interface ILastfmService
{
    /// <summary>
    /// Validate a Last.fm username and get user info.
    /// </summary>
    Task<LastfmUser?> GetUserAsync(string username, CancellationToken cancellationToken = default);
}

/// <summary>
/// Implementation of Last.fm API service.
/// </summary>
public sealed class LastfmService : ILastfmService
{
    private readonly HttpClient _httpClient;
    private readonly LastfmOptions _options;

    public LastfmService(HttpClient httpClient, IOptions<LastfmOptions> options)
    {
        _httpClient = httpClient;
        _options = options.Value;

        if (string.IsNullOrWhiteSpace(_options.ApiKey))
        {
            throw new InvalidOperationException(
                "Last.fm API key is not configured. Please set 'Lastfm:ApiKey' in appsettings.");
        }
    }

    public async Task<LastfmUser?> GetUserAsync(string username, CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(username))
        {
            throw new ArgumentException("Username cannot be empty", nameof(username));
        }

        var url = BuildUrl("user.getinfo", new Dictionary<string, string>
        {
            { "user", username }
        });

        try
        {
            var response = await _httpClient.GetAsync(url, cancellationToken);

            var content = await response.Content.ReadAsStringAsync(cancellationToken);
            
            if (string.IsNullOrWhiteSpace(content))
            {
                return null;
            }

            var json = JsonDocument.Parse(content);
            var root = json.RootElement;

            // Check for error in response
            if (root.TryGetProperty("error", out var errorElement))
            {
                // Last.fm API returns error codes for invalid users, etc.
                return null;
            }

            if (!root.TryGetProperty("user", out var userElement))
            {
                return null;
            }

            var playCountElement = userElement.GetProperty("playcount");
            var playcountStr = playCountElement.ValueKind == JsonValueKind.String 
                ? playCountElement.GetString() 
                : playCountElement.GetInt32().ToString();

            var user = new LastfmUser
            {
                Username = userElement.GetProperty("name").GetString() ?? username,
                PlayCount = int.TryParse(playcountStr, out var count) ? count : 0,
                Registered = userElement.GetProperty("registered").GetProperty("unixtime").GetString() ?? ""
            };

            return user;
        }
        catch (Exception)
        {
            // Any parsing error or connection error returns null
            return null;
        }
    }

    private string BuildUrl(string method, Dictionary<string, string> parameters)
    {
        var queryParams = new List<string>
        {
            $"method={Uri.EscapeDataString(method)}",
            $"api_key={Uri.EscapeDataString(_options.ApiKey)}",
            "format=json"
        };

        foreach (var (key, value) in parameters)
        {
            queryParams.Add($"{Uri.EscapeDataString(key)}={Uri.EscapeDataString(value)}");
        }

        return $"{_options.ApiUrl}?{string.Join("&", queryParams)}";
    }
}

/// <summary>
/// Last.fm user information.
/// </summary>
public sealed record LastfmUser
{
    public required string Username { get; init; }
    public required int PlayCount { get; init; }
    public required string Registered { get; init; }
}
