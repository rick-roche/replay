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
public sealed class SpotifyAuthService(
    HttpClient httpClient,
    IOptions<SpotifyOptions> options) : ISpotifyAuthService
{
    private const string AuthorizeEndpoint = "https://accounts.spotify.com/authorize";
    private const string TokenEndpoint = "https://accounts.spotify.com/api/token";
    private const string UserProfileEndpoint = "https://api.spotify.com/v1/me";

    private readonly SpotifyOptions _options = options.Value;

    public string GetAuthorizationUrl(string state)
    {
        var scopes = string.Join(" ", _options.Scopes);
        var queryParams = new Dictionary<string, string>
        {
            ["client_id"] = _options.ClientId,
            ["response_type"] = "code",
            ["redirect_uri"] = _options.RedirectUri,
            ["state"] = state,
            ["scope"] = scopes
        };

        var query = string.Join("&", queryParams.Select(kvp =>
            $"{Uri.EscapeDataString(kvp.Key)}={Uri.EscapeDataString(kvp.Value)}"));

        return $"{AuthorizeEndpoint}?{query}";
    }

    public async Task<AuthSession> ExchangeCodeAsync(string code, CancellationToken cancellationToken = default)
    {
        var tokenResponse = await RequestTokenAsync(new Dictionary<string, string>
        {
            ["grant_type"] = "authorization_code",
            ["code"] = code,
            ["redirect_uri"] = _options.RedirectUri
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

        using var response = await httpClient.SendAsync(request, cancellationToken);
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

        using var response = await httpClient.SendAsync(request, cancellationToken);
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
