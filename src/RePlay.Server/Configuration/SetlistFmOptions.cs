namespace RePlay.Server.Configuration;

/// <summary>
/// Configuration options for Setlist.fm API integration.
/// </summary>
public sealed record SetlistFmOptions
{
    public const string SectionName = "SetlistFm";

    /// <summary>
    /// Setlist.fm API key required for all requests.
    /// </summary>
    public required string ApiKey { get; init; }

    /// <summary>
    /// Base URL for the Setlist.fm REST API.
    /// </summary>
    public string ApiUrl { get; init; } = "https://api.setlist.fm/rest/1.0";

    /// <summary>
    /// User agent string sent with each request as required by Setlist.fm policies.
    /// </summary>
    public string UserAgent { get; init; } = "RePlay/1.0 (+https://github.com/rick-roche/replay)";
}
