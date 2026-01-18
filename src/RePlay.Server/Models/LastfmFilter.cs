namespace RePlay.Server.Models;

/// <summary>
/// Filter options for Last.fm data queries.
/// </summary>
public sealed record LastfmFilter
{
    /// <summary>
    /// Type of data to fetch: tracks, albums, or artists.
    /// </summary>
    public required LastfmDataType DataType { get; init; }

    /// <summary>
    /// Time period for the query.
    /// </summary>
    public required LastfmTimePeriod TimePeriod { get; init; }

    /// <summary>
    /// Custom start date (ISO 8601 format) - required if TimePeriod is Custom.
    /// </summary>
    public string? CustomStartDate { get; init; }

    /// <summary>
    /// Custom end date (ISO 8601 format) - required if TimePeriod is Custom.
    /// </summary>
    public string? CustomEndDate { get; init; }

    /// <summary>
    /// Maximum number of items to fetch.
    /// </summary>
    public int MaxResults { get; init; } = 50;
}

/// <summary>
/// Type of data to fetch from Last.fm.
/// </summary>
public enum LastfmDataType
{
    Tracks,
    Albums,
    Artists
}

/// <summary>
/// Predefined time periods for Last.fm queries.
/// </summary>
public enum LastfmTimePeriod
{
    Last7Days,
    Last1Month,
    Last3Months,
    Last6Months,
    Last12Months,
    Overall,
    Custom
}
