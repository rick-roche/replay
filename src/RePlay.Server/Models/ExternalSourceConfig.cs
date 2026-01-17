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
    public required bool IsConfigured { get; init; }
}
