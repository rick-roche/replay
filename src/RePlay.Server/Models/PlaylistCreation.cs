namespace RePlay.Server.Models;

/// <summary>
/// Request to create a playlist on Spotify with matched tracks.
/// </summary>
public sealed record PlaylistCreationRequest
{
    /// <summary>
    /// Playlist name.
    /// </summary>
    public required string Name { get; init; }

    /// <summary>
    /// Playlist description.
    /// </summary>
    public string? Description { get; init; }

    /// <summary>
    /// Whether the playlist is public.
    /// </summary>
    public required bool IsPublic { get; init; }

    /// <summary>
    /// List of Spotify track URIs to add to the playlist.
    /// Format: spotify:track:ID
    /// </summary>
    public required List<string> TrackUris { get; init; }
}

/// <summary>
/// Response containing the created playlist information.
/// </summary>
public sealed record PlaylistCreationResponse
{
    /// <summary>
    /// Spotify playlist ID.
    /// </summary>
    public required string PlaylistId { get; init; }

    /// <summary>
    /// Spotify URI for the playlist.
    /// Format: spotify:playlist:ID
    /// </summary>
    public required string Uri { get; init; }

    /// <summary>
    /// Web URL to open the playlist in Spotify.
    /// </summary>
    public required string Url { get; init; }

    /// <summary>
    /// Number of tracks added to the playlist.
    /// </summary>
    public required int TracksAdded { get; init; }
}
