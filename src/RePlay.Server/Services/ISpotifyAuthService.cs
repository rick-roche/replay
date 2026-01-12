using RePlay.Server.Models;

namespace RePlay.Server.Services;

/// <summary>
/// Service for handling Spotify OAuth authentication.
/// </summary>
public interface ISpotifyAuthService
{
    /// <summary>
    /// Generate the Spotify OAuth authorization URL.
    /// </summary>
    /// <param name="state">Anti-forgery state token.</param>
    /// <returns>Authorization URL to redirect user to.</returns>
    string GetAuthorizationUrl(string state);

    /// <summary>
    /// Exchange authorization code for access and refresh tokens.
    /// </summary>
    /// <param name="code">Authorization code from Spotify callback.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>Authenticated session with tokens and user info.</returns>
    Task<AuthSession> ExchangeCodeAsync(string code, CancellationToken cancellationToken = default);

    /// <summary>
    /// Refresh an expired access token using the refresh token.
    /// </summary>
    /// <param name="refreshToken">Spotify refresh token.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>Updated session with new access token.</returns>
    Task<AuthSession> RefreshTokenAsync(string refreshToken, CancellationToken cancellationToken = default);

    /// <summary>
    /// Get Spotify user profile information.
    /// </summary>
    /// <param name="accessToken">Valid Spotify access token.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>User profile information.</returns>
    Task<SpotifyUser> GetUserProfileAsync(string accessToken, CancellationToken cancellationToken = default);
}
