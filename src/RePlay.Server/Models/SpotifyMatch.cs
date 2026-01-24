namespace RePlay.Server.Models;

/// <summary>
/// Represents a matched Spotify track for a normalized source track.
/// </summary>
public sealed class SpotifyMatch
{
    /// <summary>
    /// Spotify track ID.
    /// </summary>
    public required string SpotifyId { get; init; }

    /// <summary>
    /// Track name from Spotify.
    /// </summary>
    public required string Name { get; init; }

    /// <summary>
    /// Artist name from Spotify.
    /// </summary>
    public required string Artist { get; init; }

    /// <summary>
    /// Album name from Spotify.
    /// </summary>
    public string? Album { get; init; }

    /// <summary>
    /// Spotify URI for the track (e.g., spotify:track:xyz).
    /// </summary>
    public required string Uri { get; init; }

    /// <summary>
    /// Match confidence score (0-100).
    /// </summary>
    public required int Confidence { get; init; }

    /// <summary>
    /// Method used to match this track.
    /// </summary>
    public required MatchMethod Method { get; init; }
}

/// <summary>
/// Represents a normalized track with its Spotify match (if found).
/// </summary>
public sealed class MatchedTrack
{
    /// <summary>
    /// Original normalized track from the source.
    /// </summary>
    public required NormalizedTrack SourceTrack { get; init; }

    /// <summary>
    /// Matched Spotify track, or null if no match found.
    /// </summary>
    public SpotifyMatch? Match { get; init; }

    /// <summary>
    /// Whether this track was successfully matched.
    /// </summary>
    public bool IsMatched => Match is not null;
}

/// <summary>
/// Method used to match a track to Spotify.
/// </summary>
public enum MatchMethod
{
    /// <summary>
    /// Exact string match (artist + track).
    /// </summary>
    Exact,

    /// <summary>
    /// Normalized match (case-insensitive, punctuation removed).
    /// </summary>
    Normalized,

    /// <summary>
    /// Fuzzy match using string similarity.
    /// </summary>
    Fuzzy,

    /// <summary>
    /// Match using album context to disambiguate.
    /// </summary>
    AlbumBased
}

/// <summary>
/// Response containing matched tracks.
/// </summary>
public sealed class MatchedDataResponse
{
    /// <summary>
    /// List of tracks with their matches.
    /// </summary>
    public required List<MatchedTrack> Tracks { get; init; }

    /// <summary>
    /// Total number of tracks attempted to match.
    /// </summary>
    public int TotalTracks => Tracks.Count;

    /// <summary>
    /// Number of successfully matched tracks.
    /// </summary>
    public int MatchedCount => Tracks.Count(t => t.IsMatched);

    /// <summary>
    /// Number of unmatched tracks.
    /// </summary>
    public int UnmatchedCount => Tracks.Count(t => !t.IsMatched);
}
