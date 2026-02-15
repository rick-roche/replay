using RePlay.Server.Models;

namespace RePlay.Server.Services;

/// <summary>
/// Abstraction over Discogs API interactions.
/// </summary>
public interface IDiscogsService
{
    /// <summary>
    /// Fetch Discogs profile details (username, collection metadata).
    /// </summary>
    Task<DiscogsProfile?> GetProfileAsync(string usernameOrCollectionId, CancellationToken cancellationToken = default);

    /// <summary>
    /// Fetch collection data from a Discogs user with specified filters.
    /// </summary>
    Task<DiscogsDataResponse?> GetCollectionAsync(string usernameOrCollectionId, DiscogsFilter filter, CancellationToken cancellationToken = default);

    /// <summary>
    /// Fetch and normalize collection data from a Discogs user with specified filters.
    /// </summary>
    Task<NormalizedDataResponse?> GetCollectionNormalizedAsync(string usernameOrCollectionId, DiscogsFilter filter, CancellationToken cancellationToken = default);
}
