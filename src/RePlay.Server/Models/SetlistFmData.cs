namespace RePlay.Server.Models;

/// <summary>
/// Represents a track from a Setlist.fm concert.
/// </summary>
public sealed record SetlistTrack
{
    public required string Name { get; init; }
    public required string Artist { get; init; }
    public string? ConcertDate { get; init; }
    public string? Venue { get; init; }
    public string? City { get; init; }
    public string? Country { get; init; }
}

/// <summary>
/// Represents a concert from Setlist.fm.
/// </summary>
public sealed record SetlistConcert
{
    public required string Id { get; init; }
    public required string Artist { get; init; }
    public string? Date { get; init; }
    public string? Venue { get; init; }
    public string? City { get; init; }
    public string? Country { get; init; }
    public required List<SetlistTrack> Tracks { get; init; } = [];
}

/// <summary>
/// Response containing Setlist.fm data fetch results.
/// </summary>
public sealed record SetlistFmDataResponse
{
    public required List<SetlistConcert> Concerts { get; init; } = [];
    public required List<SetlistTrack> Tracks { get; init; } = [];
    public int TotalConcerts { get; init; }
    public int TotalTracks { get; init; }
}
