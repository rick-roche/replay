namespace RePlay.Server.Models;

/// <summary>
/// Filter options for Setlist.fm concert data queries.
/// </summary>
public sealed record SetlistFmFilter
{
    /// <summary>
    /// Custom start date (ISO 8601 format) for filtering concerts.
    /// If not specified, fetches all concerts.
    /// </summary>
    public string? StartDate { get; init; }

    /// <summary>
    /// Custom end date (ISO 8601 format) for filtering concerts.
    /// If not specified, fetches up to the current date.
    /// </summary>
    public string? EndDate { get; init; }

    /// <summary>
    /// Maximum number of concerts to fetch.
    /// </summary>
    public int MaxConcerts { get; init; } = 10;

    /// <summary>
    /// Maximum number of tracks in final playlist (deduplicated).
    /// </summary>
    public int MaxTracks { get; init; } = 100;
}
