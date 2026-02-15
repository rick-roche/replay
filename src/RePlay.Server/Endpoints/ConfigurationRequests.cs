using RePlay.Server.Models;

namespace RePlay.Server.Endpoints;

/// <summary>
/// Request to fetch Last.fm data.
/// </summary>
public sealed record FetchLastfmDataRequest
{
    public required string Username { get; init; }
    public required LastfmFilter Filter { get; init; }
}

/// <summary>
/// Request to fetch Discogs collection data.
/// </summary>
public sealed record FetchDiscogsDataRequest
{
    public required string UsernameOrCollectionId { get; init; }
    public required DiscogsFilter Filter { get; init; }
}

/// <summary>
/// Request to fetch Setlist.fm concert data.
/// </summary>
public sealed record FetchSetlistFmDataRequest
{
    public required string UserId { get; init; }
    public required SetlistFmFilter Filter { get; init; }
}

/// <summary>
/// Request to configure a Setlist.fm profile.
/// </summary>
public sealed record ConfigureSetlistRequest
{
    public required string UsernameOrId { get; init; }
}

/// <summary>
/// Response containing Setlist.fm configuration details.
/// </summary>
public sealed record ConfigureSetlistResponse
{
    public required string UserId { get; init; }
    public required string DisplayName { get; init; }
    public string? ProfileUrl { get; init; }
    public int AttendedConcerts { get; init; }
    public required bool IsConfigured { get; init; }
}
