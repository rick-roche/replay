namespace RePlay.Server.Models;

/// <summary>
/// Represents a Spotify track returned from search results.
/// </summary>
public sealed class SpotifyTrack
{
    /// <summary>
    /// Spotify track ID.
    /// </summary>
    public required string Id { get; init; }

    /// <summary>
    /// Track name.
    /// </summary>
    public required string Name { get; init; }

    /// <summary>
    /// Primary artist name.
    /// </summary>
    public required string Artist { get; init; }

    /// <summary>
    /// Album name.
    /// </summary>
    public string? Album { get; init; }

    /// <summary>
    /// Spotify URI (spotify:track:xyz).
    /// </summary>
    public required string Uri { get; init; }
}
