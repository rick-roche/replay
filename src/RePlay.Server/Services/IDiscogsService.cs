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
}
