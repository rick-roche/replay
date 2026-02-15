namespace RePlay.Server.Models;

/// <summary>
/// Filter options for Discogs collection queries.
/// </summary>
public sealed record DiscogsFilter
{
    /// <summary>
    /// Minimum release year (inclusive).
    /// </summary>
    public int? MinReleaseYear { get; init; }

    /// <summary>
    /// Maximum release year (inclusive).
    /// </summary>
    public int? MaxReleaseYear { get; init; }

    /// <summary>
    /// Media format to filter by (e.g., Vinyl, CD, Cassette, Digital).
    /// Null means all formats.
    /// </summary>
    public DiscogsMediaFormat? MediaFormat { get; init; }

    /// <summary>
    /// Minimum year the item was added to collection (inclusive).
    /// </summary>
    public int? MinYearAdded { get; init; }

    /// <summary>
    /// Maximum year the item was added to collection (inclusive).
    /// </summary>
    public int? MaxYearAdded { get; init; }

    /// <summary>
    /// Maximum number of tracks to fetch from the collection.
    /// </summary>
    public int MaxTracks { get; init; } = 100;
}

/// <summary>
/// Media format options for Discogs releases.
/// </summary>
public enum DiscogsMediaFormat
{
    Vinyl,
    CD,
    Cassette,
    Digital
}
