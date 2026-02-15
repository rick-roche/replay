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
/// Represents a matched Spotify album with its tracks.
/// </summary>
public sealed class SpotifyAlbumMatch
{
    /// <summary>
    /// Spotify album ID.
    /// </summary>
    public required string SpotifyId { get; init; }

    /// <summary>
    /// Album name from Spotify.
    /// </summary>
    public required string Name { get; init; }

    /// <summary>
    /// Artist name from Spotify.
    /// </summary>
    public required string Artist { get; init; }

    /// <summary>
    /// Spotify URI for the album (e.g., spotify:album:xyz).
    /// </summary>
    public required string Uri { get; init; }

    /// <summary>
    /// Tracks from the Spotify album.
    /// </summary>
    public required List<SpotifyMatch> Tracks { get; init; }

    /// <summary>
    /// Match confidence score (0-100).
    /// </summary>
    public required int Confidence { get; init; }

    /// <summary>
    /// Method used to match this album.
    /// </summary>
    public required MatchMethod Method { get; init; }
}

/// <summary>
/// Represents a normalized album with its Spotify match (if found).
/// </summary>
public sealed class MatchedAlbum
{
    /// <summary>
    /// Original normalized album from the source.
    /// </summary>
    public required NormalizedAlbum SourceAlbum { get; init; }

    /// <summary>
    /// Matched Spotify album with tracks, or null if no match found.
    /// </summary>
    public SpotifyAlbumMatch? Match { get; init; }

    /// <summary>
    /// Whether this album was successfully matched.
    /// </summary>
    public bool IsMatched => Match is not null;
}

/// <summary>
/// Represents a matched Spotify artist with top tracks.
/// </summary>
public sealed class SpotifyArtistMatch
{
    /// <summary>
    /// Spotify artist ID.
    /// </summary>
    public required string SpotifyId { get; init; }

    /// <summary>
    /// Artist name from Spotify.
    /// </summary>
    public required string Name { get; init; }

    /// <summary>
    /// Spotify URI for the artist (e.g., spotify:artist:xyz).
    /// </summary>
    public required string Uri { get; init; }

    /// <summary>
    /// Top tracks from the Spotify artist.
    /// </summary>
    public required List<SpotifyMatch> TopTracks { get; init; }

    /// <summary>
    /// Match confidence score (0-100).
    /// </summary>
    public required int Confidence { get; init; }

    /// <summary>
    /// Method used to match this artist.
    /// </summary>
    public required MatchMethod Method { get; init; }
}

/// <summary>
/// Represents a normalized artist with its Spotify match (if found).
/// </summary>
public sealed class MatchedArtist
{
    /// <summary>
    /// Original normalized artist from the source.
    /// </summary>
    public required NormalizedArtist SourceArtist { get; init; }

    /// <summary>
    /// Matched Spotify artist with top tracks, or null if no match found.
    /// </summary>
    public SpotifyArtistMatch? Match { get; init; }

    /// <summary>
    /// Whether this artist was successfully matched.
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

/// <summary>
/// Response containing matched albums.
/// </summary>
public sealed class MatchedAlbumsResponse
{
    /// <summary>
    /// List of albums with their matches.
    /// </summary>
    public required List<MatchedAlbum> Albums { get; init; }

    /// <summary>
    /// Total number of albums attempted to match.
    /// </summary>
    public int TotalAlbums => Albums.Count;

    /// <summary>
    /// Number of successfully matched albums.
    /// </summary>
    public int MatchedCount => Albums.Count(a => a.IsMatched);

    /// <summary>
    /// Number of unmatched albums.
    /// </summary>
    public int UnmatchedCount => Albums.Count(a => !a.IsMatched);

    /// <summary>
    /// Total number of tracks from all matched albums.
    /// </summary>
    public int TotalTracks => Albums
        .Where(a => a.IsMatched)
        .Sum(a => a.Match?.Tracks.Count ?? 0);
}

/// <summary>
/// Response containing matched artists.
/// </summary>
public sealed class MatchedArtistsResponse
{
    /// <summary>
    /// List of artists with their matches.
    /// </summary>
    public required List<MatchedArtist> Artists { get; init; }

    /// <summary>
    /// Total number of artists attempted to match.
    /// </summary>
    public int TotalArtists => Artists.Count;

    /// <summary>
    /// Number of successfully matched artists.
    /// </summary>
    public int MatchedCount => Artists.Count(a => a.IsMatched);

    /// <summary>
    /// Number of unmatched artists.
    /// </summary>
    public int UnmatchedCount => Artists.Count(a => !a.IsMatched);

    /// <summary>
    /// Total number of tracks from all matched artists.
    /// </summary>
    public int TotalTracks => Artists
        .Where(a => a.IsMatched)
        .Sum(a => a.Match?.TopTracks.Count ?? 0);
}
