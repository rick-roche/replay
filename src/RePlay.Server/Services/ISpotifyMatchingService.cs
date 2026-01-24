using RePlay.Server.Models;

namespace RePlay.Server.Services;

/// <summary>
/// Service for matching normalized tracks to Spotify.
/// </summary>
public interface ISpotifyMatchingService
{
    /// <summary>
    /// Matches a list of normalized tracks to Spotify tracks.
    /// </summary>
    /// <param name="tracks">Normalized tracks to match.</param>
    /// <param name="accessToken">Spotify access token for API authentication.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>Matched tracks with confidence scores.</returns>
    Task<MatchedDataResponse> MatchTracksAsync(
        IReadOnlyList<NormalizedTrack> tracks,
        string accessToken,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Searches Spotify for tracks matching the given query.
    /// </summary>
    /// <param name="query">Track name and/or artist name to search for.</param>
    /// <param name="accessToken">Spotify access token for API authentication.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>List of Spotify tracks matching the query (up to 5 results).</returns>
    Task<IReadOnlyList<SpotifyTrack>> SearchTracksAsync(
        string query,
        string accessToken,
        CancellationToken cancellationToken = default);
}
