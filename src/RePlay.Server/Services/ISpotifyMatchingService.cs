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

    /// <summary>
    /// Creates a playlist on Spotify and adds tracks to it.
    /// </summary>
    /// <param name="request">Playlist creation request with name, description, visibility, and track URIs.</param>
    /// <param name="accessToken">Spotify access token for API authentication.</param>
    /// <param name="userId">Spotify user ID (from session).</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>Created playlist information with ID, URI, and URL.</returns>
    Task<PlaylistCreationResponse> CreatePlaylistAsync(
        PlaylistCreationRequest request,
        string accessToken,
        string userId,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Matches a list of normalized albums to Spotify albums and retrieves tracks from each album.
    /// </summary>
    /// <param name="albums">Normalized albums to match.</param>
    /// <param name="accessToken">Spotify access token for API authentication.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>Matched albums with their tracks and confidence scores.</returns>
    Task<MatchedAlbumsResponse> MatchAlbumsAsync(
        IReadOnlyList<NormalizedAlbum> albums,
        string accessToken,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Matches a list of normalized artists to Spotify artists and retrieves top tracks from each artist.
    /// </summary>
    /// <param name="artists">Normalized artists to match.</param>
    /// <param name="accessToken">Spotify access token for API authentication.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>Matched artists with their top tracks and confidence scores.</returns>
    Task<MatchedArtistsResponse> MatchArtistsAsync(
        IReadOnlyList<NormalizedArtist> artists,
        string accessToken,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Searches Spotify for albums matching the given query.
    /// </summary>
    /// <param name="query">Album name and/or artist name to search for.</param>
    /// <param name="accessToken">Spotify access token for API authentication.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>List of Spotify albums matching the query (up to 5 results).</returns>
    Task<IReadOnlyList<SpotifyAlbumInfo>> SearchAlbumsAsync(
        string query,
        string accessToken,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Searches Spotify for artists matching the given query.
    /// </summary>
    /// <param name="query">Artist name to search for.</param>
    /// <param name="accessToken">Spotify access token for API authentication.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>List of Spotify artists matching the query (up to 5 results).</returns>
    Task<IReadOnlyList<SpotifyArtistInfo>> SearchArtistsAsync(
        string query,
        string accessToken,
        CancellationToken cancellationToken = default);
}