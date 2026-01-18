namespace RePlay.Server.Models;

/// <summary>
/// Normalized track representation for consistent matching across all sources.
/// Preserves all original source data while providing a canonical structure.
/// </summary>
public sealed record NormalizedTrack
{
    /// <summary>
    /// Track name.
    /// </summary>
    public required string Name { get; init; }

    /// <summary>
    /// Primary artist name.
    /// </summary>
    public required string Artist { get; init; }

    /// <summary>
    /// Album name (if available).
    /// </summary>
    public string? Album { get; init; }

    /// <summary>
    /// Source-specific metadata preserved as-is.
    /// For Last.fm: { "playCount": 42 }
    /// For Discogs: { "year": 2020, "format": "Vinyl" }
    /// For Setlist.fm: { "concertDate": "2023-05-15", "venue": "..." }
    /// </summary>
    public required Dictionary<string, object?> SourceMetadata { get; init; }

    /// <summary>
    /// The source this track came from (lastfm, discogs, setlistfm).
    /// </summary>
    public required string Source { get; init; }
}

/// <summary>
/// Normalized album representation for consistent matching across all sources.
/// </summary>
public sealed record NormalizedAlbum
{
    /// <summary>
    /// Album name.
    /// </summary>
    public required string Name { get; init; }

    /// <summary>
    /// Primary artist name.
    /// </summary>
    public required string Artist { get; init; }

    /// <summary>
    /// List of normalized tracks on the album (in order if available).
    /// </summary>
    public required List<NormalizedTrack> Tracks { get; init; }

    /// <summary>
    /// Source-specific metadata preserved as-is.
    /// For Last.fm: { "playCount": 25 }
    /// For Discogs: { "year": 2020, "format": "Vinyl", "added": "2024-01-15" }
    /// </summary>
    public required Dictionary<string, object?> SourceMetadata { get; init; }

    /// <summary>
    /// The source this album came from.
    /// </summary>
    public required string Source { get; init; }
}

/// <summary>
/// Normalized artist representation for consistent matching across all sources.
/// </summary>
public sealed record NormalizedArtist
{
    /// <summary>
    /// Artist name.
    /// </summary>
    public required string Name { get; init; }

    /// <summary>
    /// Source-specific metadata preserved as-is.
    /// For Last.fm: { "playCount": 100 }
    /// For Discogs: { "albums": 15 }
    /// </summary>
    public required Dictionary<string, object?> SourceMetadata { get; init; }

    /// <summary>
    /// The source this artist came from.
    /// </summary>
    public required string Source { get; init; }
}

/// <summary>
/// Normalized response containing data from any source in a canonical format.
/// Used internally for matching; can be converted to source-specific responses.
/// </summary>
public sealed record NormalizedDataResponse
{
    /// <summary>
    /// Type of data: Tracks, Albums, or Artists.
    /// </summary>
    public required string DataType { get; init; }

    /// <summary>
    /// Normalized tracks (if DataType is "Tracks").
    /// </summary>
    public required List<NormalizedTrack> Tracks { get; init; } = [];

    /// <summary>
    /// Normalized albums (if DataType is "Albums").
    /// </summary>
    public required List<NormalizedAlbum> Albums { get; init; } = [];

    /// <summary>
    /// Normalized artists (if DataType is "Artists").
    /// </summary>
    public required List<NormalizedArtist> Artists { get; init; } = [];

    /// <summary>
    /// Total items found in source.
    /// </summary>
    public int TotalResults { get; init; }

    /// <summary>
    /// The source these results came from.
    /// </summary>
    public required string Source { get; init; }
}
