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
