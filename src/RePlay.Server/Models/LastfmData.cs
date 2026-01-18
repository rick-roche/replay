namespace RePlay.Server.Models;

/// <summary>
/// Represents a track from Last.fm.
/// </summary>
public sealed record LastfmTrack
{
    public required string Name { get; init; }
    public required string Artist { get; init; }
    public string? Album { get; init; }
    public int PlayCount { get; init; }
}

/// <summary>
/// Represents an album from Last.fm.
/// </summary>
public sealed record LastfmAlbum
{
    public required string Name { get; init; }
    public required string Artist { get; init; }
    public int PlayCount { get; init; }
}

/// <summary>
/// Represents an artist from Last.fm.
/// </summary>
public sealed record LastfmArtist
{
    public required string Name { get; init; }
    public int PlayCount { get; init; }
}

/// <summary>
/// Response containing Last.fm data fetch results.
/// </summary>
public sealed record LastfmDataResponse
{
    public required LastfmDataType DataType { get; init; }
    public required List<LastfmTrack> Tracks { get; init; } = [];
    public required List<LastfmAlbum> Albums { get; init; } = [];
    public required List<LastfmArtist> Artists { get; init; } = [];
    public int TotalResults { get; init; }
}
