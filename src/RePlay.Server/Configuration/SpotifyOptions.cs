using System.ComponentModel.DataAnnotations;

namespace RePlay.Server.Configuration;

/// <summary>
/// Spotify OAuth configuration options.
/// </summary>
public sealed class SpotifyOptions
{
    public const string SectionName = "Spotify";

    /// <summary>
    /// Spotify application client ID.
    /// </summary>
    [Required(ErrorMessage = "Spotify ClientId is required")]
    public required string ClientId { get; init; }

    /// <summary>
    /// Spotify application client secret.
    /// </summary>
    [Required(ErrorMessage = "Spotify ClientSecret is required")]
    public required string ClientSecret { get; init; }

    /// <summary>
    /// Redirect URI for OAuth callback.
    /// Must match the URI registered in Spotify app settings.
    /// </summary>
    [Required(ErrorMessage = "Spotify RedirectUri is required")]
    public required string RedirectUri { get; init; }

    /// <summary>
    /// Required OAuth scopes for playlist management.
    /// </summary>
    public string[] Scopes { get; init; } =
    [
        "playlist-modify-private",
        "playlist-modify-public",
        "user-read-email"
    ];
}
