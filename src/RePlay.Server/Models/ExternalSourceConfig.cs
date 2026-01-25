namespace RePlay.Server.Models;

/// <summary>
/// Represents a user's configuration for an external data source.
/// </summary>
public sealed record ExternalSourceConfig
{
    public required string Source { get; init; }
    public required string ConfigValue { get; init; } // username, collection ID, etc.
    public required DateTime ConfiguredAt { get; init; }
}

/// <summary>
/// Request to configure a Last.fm profile.
/// </summary>
public sealed record ConfigureLastfmRequest
{
    public required string Username { get; init; }
}

/// <summary>
/// Response containing Last.fm configuration.
/// </summary>
public sealed record ConfigureLastfmResponse
{
    public required string Username { get; init; }
    public required int PlayCount { get; init; }
    public required string ProfileUrl { get; init; }
    public required bool IsConfigured { get; init; }
}

/// <summary>
/// Request to configure a Discogs profile.
/// </summary>
public sealed record ConfigureDiscogsRequest
{
    public required string UsernameOrCollectionId { get; init; }
}

/// <summary>
/// Response containing Discogs configuration details.
/// </summary>
public sealed record ConfigureDiscogsResponse
{
    public required string Username { get; init; }
    public required string CollectionUrl { get; init; }
    public required int ReleaseCount { get; init; }
    public required bool IsConfigured { get; init; }
}

/// <summary>
/// Discogs profile metadata.
/// </summary>
public sealed record DiscogsProfile
{
    public required string Username { get; init; }
    public required string CollectionUrl { get; init; }
    public required int ReleaseCount { get; init; }
}
