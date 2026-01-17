namespace RePlay.Server.Configuration;

/// <summary>
/// Configuration options for Last.fm API integration.
/// </summary>
public sealed record LastfmOptions
{
    public const string SectionName = "Lastfm";

    /// <summary>
    /// Last.fm API key
    /// </summary>
    public required string ApiKey { get; init; }

    /// <summary>
    /// Last.fm API base URL
    /// </summary>
    public string ApiUrl { get; init; } = "https://ws.audioscrobbler.com/2.0/";
}
