namespace RePlay.Server.Models;

/// <summary>
/// Represents a track from a Discogs release.
/// </summary>
public sealed record DiscogsTrack
{
    public required string Name { get; init; }
    public required string Artist { get; init; }
    public string? Album { get; init; }
    public int? ReleaseYear { get; init; }
}

/// <summary>
/// Represents a release (album) from a Discogs collection.
/// </summary>
public sealed record DiscogsRelease
{
    public required int Id { get; init; }
    public required string Title { get; init; }
    public required string Artist { get; init; }
    public int? Year { get; init; }
    public string? Format { get; init; }
    public string? DateAdded { get; init; }
    public required List<DiscogsTrack> Tracks { get; init; } = [];
}

/// <summary>
/// Response containing Discogs collection data fetch results.
/// </summary>
public sealed record DiscogsDataResponse
{
    public required List<DiscogsRelease> Releases { get; init; } = [];
    public required List<DiscogsTrack> Tracks { get; init; } = [];
    public int TotalReleases { get; init; }
    public int TotalTracks { get; init; }
}
