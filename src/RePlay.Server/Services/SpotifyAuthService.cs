using System.Net.Http.Headers;
using System.Text;
using System.Text.Json;
using System.Text.Json.Serialization;
using Microsoft.Extensions.Options;
using RePlay.Server.Configuration;
using RePlay.Server.Models;

namespace RePlay.Server.Services;

/// <summary>
/// Implementation of Spotify OAuth authentication service.
/// </summary>
public sealed class SpotifyAuthService : ISpotifyAuthService
{
    private const string AuthorizeEndpoint = "https://accounts.spotify.com/authorize";
    private const string TokenEndpoint = "https://accounts.spotify.com/api/token";
    private const string UserProfileEndpoint = "https://api.spotify.com/v1/me";

    private readonly HttpClient _httpClient;
    private readonly SpotifyOptions _options;

    public SpotifyAuthService(HttpClient httpClient, IOptions<SpotifyOptions> options)
    {
        _httpClient = httpClient;
        _options = options.Value;
    }

    // Validate Spotify configuration when service is created
    private void ValidateConfiguration()
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(_options.ClientId,
            $"{nameof(SpotifyOptions.ClientId)} must be configured. Set Spotify__ClientId environment variable or configure in appsettings.Development.json");
        ArgumentException.ThrowIfNullOrWhiteSpace(_options.ClientSecret,
            $"{nameof(SpotifyOptions.ClientSecret)} must be configured. Set Spotify__ClientSecret environment variable or configure in appsettings.Development.json");
        ArgumentException.ThrowIfNullOrWhiteSpace(_options.RedirectUri,
            $"{nameof(SpotifyOptions.RedirectUri)} must be configured. Set Spotify__RedirectUri environment variable or configure in appsettings.Development.json");

        // Validate redirect URI format per Spotify requirements
        if (_options.RedirectUri.Contains("localhost"))
        {
            throw new ArgumentException(
                "Spotify does not allow 'localhost' as a redirect URI. " +
                "Use explicit loopback address instead: http://127.0.0.1:PORT or http://[::1]:PORT. " +
                "See: https://developer.spotify.com/documentation/web-api/concepts/redirect_uri",
                nameof(SpotifyOptions.RedirectUri));
        }
    }

    public string GetAuthorizationUrl(string state, string? redirectUri = null)
    {
        ValidateConfiguration();

        // Use provided redirect URI or fall back to configured default
        var effectiveRedirectUri = redirectUri ?? _options.RedirectUri;

        var scopes = string.Join(" ", _options.Scopes);
        var queryParams = new Dictionary<string, string>
        {
            ["client_id"] = _options.ClientId,
            ["response_type"] = "code",
            ["redirect_uri"] = effectiveRedirectUri,
            ["state"] = state,
            ["scope"] = scopes
        };

        var query = string.Join("&", queryParams.Select(kvp =>
            $"{Uri.EscapeDataString(kvp.Key)}={Uri.EscapeDataString(kvp.Value)}"));

        return $"{AuthorizeEndpoint}?{query}";
    }

    public async Task<AuthSession> ExchangeCodeAsync(string code, CancellationToken cancellationToken = default)
    {
        return await ExchangeCodeAsync(code, null, cancellationToken);
    }

    public async Task<AuthSession> ExchangeCodeAsync(string code, string? redirectUri, CancellationToken cancellationToken = default)
    {
        var effectiveRedirectUri = redirectUri ?? _options.RedirectUri;
        
        var tokenResponse = await RequestTokenAsync(new Dictionary<string, string>
        {
            ["grant_type"] = "authorization_code",
            ["code"] = code,
            ["redirect_uri"] = effectiveRedirectUri
        }, cancellationToken);

        var user = await GetUserProfileAsync(tokenResponse.AccessToken, cancellationToken);

        return new AuthSession
        {
            SessionId = Guid.NewGuid().ToString(),
            AccessToken = tokenResponse.AccessToken,
            RefreshToken = tokenResponse.RefreshToken,
            ExpiresAt = DateTime.UtcNow.AddSeconds(tokenResponse.ExpiresIn),
            User = user
        };
    }

    public async Task<AuthSession> RefreshTokenAsync(string refreshToken, CancellationToken cancellationToken = default)
    {
        var tokenResponse = await RequestTokenAsync(new Dictionary<string, string>
        {
            ["grant_type"] = "refresh_token",
            ["refresh_token"] = refreshToken
        }, cancellationToken);

        var user = await GetUserProfileAsync(tokenResponse.AccessToken, cancellationToken);

        return new AuthSession
        {
            SessionId = Guid.NewGuid().ToString(),
            AccessToken = tokenResponse.AccessToken,
            RefreshToken = tokenResponse.RefreshToken,
            ExpiresAt = DateTime.UtcNow.AddSeconds(tokenResponse.ExpiresIn),
            User = user
        };
    }

    public async Task<SpotifyUser> GetUserProfileAsync(string accessToken, CancellationToken cancellationToken = default)
    {
        using var request = new HttpRequestMessage(HttpMethod.Get, UserProfileEndpoint);
        request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", accessToken);

        using var response = await _httpClient.SendAsync(request, cancellationToken);
        response.EnsureSuccessStatusCode();

        var json = await response.Content.ReadAsStringAsync(cancellationToken);
        var userProfile = JsonSerializer.Deserialize<SpotifyUserProfile>(json)
            ?? throw new InvalidOperationException("Failed to deserialize user profile");

        return new SpotifyUser
        {
            Id = userProfile.Id,
            DisplayName = userProfile.DisplayName ?? userProfile.Id,
            Email = userProfile.Email,
            ImageUrl = userProfile.Images?.FirstOrDefault()?.Url
        };
    }

    private async Task<TokenResponse> RequestTokenAsync(
        Dictionary<string, string> parameters,
        CancellationToken cancellationToken)
    {
        using var request = new HttpRequestMessage(HttpMethod.Post, TokenEndpoint);

        // Add client credentials in Authorization header
        var credentials = Convert.ToBase64String(
            Encoding.UTF8.GetBytes($"{_options.ClientId}:{_options.ClientSecret}"));
        request.Headers.Authorization = new AuthenticationHeaderValue("Basic", credentials);

        request.Content = new FormUrlEncodedContent(parameters);

        using var response = await _httpClient.SendAsync(request, cancellationToken);
        response.EnsureSuccessStatusCode();

        var json = await response.Content.ReadAsStringAsync(cancellationToken);
        return JsonSerializer.Deserialize<TokenResponse>(json)
            ?? throw new InvalidOperationException("Failed to deserialize token response");
    }

    // DTOs for Spotify API responses
    private sealed record TokenResponse(
        [property: JsonPropertyName("access_token")] string AccessToken,
        [property: JsonPropertyName("token_type")] string TokenType,
        [property: JsonPropertyName("expires_in")] int ExpiresIn,
        [property: JsonPropertyName("refresh_token")] string RefreshToken,
        [property: JsonPropertyName("scope")] string Scope
    );

    private sealed record SpotifyUserProfile(
        [property: JsonPropertyName("id")] string Id,
        [property: JsonPropertyName("display_name")] string? DisplayName,
        [property: JsonPropertyName("email")] string? Email,
        [property: JsonPropertyName("images")] SpotifyImage[]? Images
    );

    private sealed record SpotifyImage(
        [property: JsonPropertyName("url")] string Url,
        [property: JsonPropertyName("height")] int? Height,
        [property: JsonPropertyName("width")] int? Width
    );
}
