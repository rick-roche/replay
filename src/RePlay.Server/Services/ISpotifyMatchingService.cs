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
}
