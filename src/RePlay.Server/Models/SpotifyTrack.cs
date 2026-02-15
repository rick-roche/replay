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

/// <summary>
/// Information about a Spotify album for search results.
/// </summary>
public sealed class SpotifyAlbumInfo
{
    /// <summary>
    /// Spotify album ID.
    /// </summary>
    public required string Id { get; init; }

    /// <summary>
    /// Album name.
    /// </summary>
    public required string Name { get; init; }

    /// <summary>
    /// Primary artist name.
    /// </summary>
    public required string Artist { get; init; }

    /// <summary>
    /// Release date.
    /// </summary>
    public string? ReleaseDate { get; init; }

    /// <summary>
    /// Spotify URI.
    /// </summary>
    public required string Uri { get; init; }

    /// <summary>
    /// Number of tracks in album.
    /// </summary>
    public int TotalTracks { get; init; }
}

/// <summary>
/// Information about a Spotify artist for search results.
/// </summary>
public sealed class SpotifyArtistInfo
{
    /// <summary>
    /// Spotify artist ID.
    /// </summary>
    public required string Id { get; init; }

    /// <summary>
    /// Artist name.
    /// </summary>
    public required string Name { get; init; }

    /// <summary>
    /// Spotify URI.
    /// </summary>
    public required string Uri { get; init; }

    /// <summary>
    /// Artist genres.
    /// </summary>
    public List<string> Genres { get; init; } = [];
}
