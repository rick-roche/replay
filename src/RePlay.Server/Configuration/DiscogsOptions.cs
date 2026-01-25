using System.ComponentModel.DataAnnotations;

namespace RePlay.Server.Configuration;

/// <summary>
/// Configuration options for Discogs API access.
/// </summary>
public sealed class DiscogsOptions
{
    public const string SectionName = "Discogs";

    /// <summary>
    /// Discogs API base URL.
    /// </summary>
    [Required]
    public string ApiUrl { get; init; } = "https://api.discogs.com";

    /// <summary>
    /// Discogs consumer key.
    /// </summary>
    [Required]
    public required string ConsumerKey { get; init; }

    /// <summary>
    /// Discogs consumer secret.
    /// </summary>
    [Required]
    public required string ConsumerSecret { get; init; }
}
