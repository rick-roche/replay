namespace RePlay.Server.Models;

/// <summary>
/// Represents an authenticated user session.
/// </summary>
public sealed record AuthSession
{
    public required string SessionId { get; init; }
    public required string AccessToken { get; init; }
    public required string RefreshToken { get; init; }
    public required DateTime ExpiresAt { get; init; }
    public required SpotifyUser User { get; init; }

    /// <summary>
    /// Check if the access token is expired or about to expire.
    /// </summary>
    public bool IsExpired(TimeSpan buffer = default)
    {
        var expiryBuffer = buffer == default ? TimeSpan.FromMinutes(5) : buffer;
        return DateTime.UtcNow.Add(expiryBuffer) >= ExpiresAt;
    }
}

/// <summary>
/// Spotify user information.
/// </summary>
public sealed record SpotifyUser
{
    public required string Id { get; init; }
    public required string DisplayName { get; init; }
    public string? Email { get; init; }
    public string? ImageUrl { get; init; }
}

/// <summary>
/// Session information returned to the client (without sensitive tokens).
/// </summary>
public sealed record SessionInfo
{
    public required string SessionId { get; init; }
    public required SpotifyUser User { get; init; }
    public required DateTime ExpiresAt { get; init; }
}
