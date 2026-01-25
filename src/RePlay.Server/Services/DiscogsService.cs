using System.Net.Http.Headers;
using System.Text.Json;
using System.Text.RegularExpressions;
using Microsoft.Extensions.Options;
using RePlay.Server.Configuration;
using RePlay.Server.Models;

namespace RePlay.Server.Services;

/// <summary>
/// Discogs API implementation.
/// </summary>
public sealed class DiscogsService : IDiscogsService
{
    private readonly HttpClient _httpClient;

    public DiscogsService(HttpClient httpClient, IOptions<DiscogsOptions> options)
    {
        _httpClient = httpClient;
        var discogsOptions = options.Value;

        _httpClient.BaseAddress = new Uri(discogsOptions.ApiUrl);
        _httpClient.DefaultRequestHeaders.UserAgent.Clear();
        _httpClient.DefaultRequestHeaders.UserAgent.Add(new ProductInfoHeaderValue("RePlay", "1.0"));
        _httpClient.DefaultRequestHeaders.Authorization =
            new AuthenticationHeaderValue("Discogs", $"key={discogsOptions.ConsumerKey}, secret={discogsOptions.ConsumerSecret}");
    }

    public async Task<DiscogsProfile?> GetProfileAsync(string usernameOrCollectionId, CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(usernameOrCollectionId))
        {
            throw new ArgumentException("Username or collection ID is required", nameof(usernameOrCollectionId));
        }

        var profile = await TryGetProfileAsync("users", usernameOrCollectionId, cancellationToken);
        if (profile != null)
        {
            var collection = await TryGetCollectionAsync(profile.Username, cancellationToken);
            if (collection == null)
            {
                return null;
            }

            return CreateProfile(profile, collection);
        }

        // If not found as username, try collection ID
        profile = await TryGetCollectionProfileAsync(usernameOrCollectionId, cancellationToken);
        if (profile == null)
        {
            return null;
        }

        var finalCollection = await TryGetCollectionAsync(profile.Username, cancellationToken);
        if (finalCollection == null)
        {
            return null;
        }

        return CreateProfile(profile, finalCollection);
    }

    private async Task<DiscogsUserResponse?> TryGetProfileAsync(string path, string identifier, CancellationToken cancellationToken)
    {
        var userResponse = await _httpClient.GetAsync($"{path}/{identifier}", cancellationToken);
        if (!userResponse.IsSuccessStatusCode)
        {
            return null;
        }

        await using var userStream = await userResponse.Content.ReadAsStreamAsync(cancellationToken);
        return await JsonSerializer.DeserializeAsync<DiscogsUserResponse>(userStream, cancellationToken: cancellationToken);
    }

    private async Task<DiscogsCollectionResponse?> TryGetCollectionAsync(string username, CancellationToken cancellationToken)
    {
        var collectionResponse = await _httpClient.GetAsync($"users/{username}/collection/folders/0/releases?per_page=1", cancellationToken);
        if (!collectionResponse.IsSuccessStatusCode)
        {
            return null;
        }

        await using var collectionStream = await collectionResponse.Content.ReadAsStreamAsync(cancellationToken);
        return await JsonSerializer.DeserializeAsync<DiscogsCollectionResponse>(collectionStream, cancellationToken: cancellationToken);
    }

    private async Task<DiscogsUserResponse?> TryGetCollectionProfileAsync(string collectionUrlOrId, CancellationToken cancellationToken)
    {
        // Discogs collection URLs often contain /users/{username}/collection
        if (collectionUrlOrId.Contains("discogs.com", StringComparison.OrdinalIgnoreCase))
        {
            var match = Regex.Match(collectionUrlOrId, @"users/(?<username>[^/]+)/collection", RegexOptions.IgnoreCase);
            if (match.Success)
            {
                var username = match.Groups["username"].Value;
                return await TryGetProfileAsync("users", username, cancellationToken);
            }
        }

        // If not a URL, attempt to fetch as username fallback
        return await TryGetProfileAsync("users", collectionUrlOrId, cancellationToken);
    }

    private static DiscogsProfile CreateProfile(DiscogsUserResponse userProfile, DiscogsCollectionResponse collection)
    {
        if (userProfile is null || collection is null)
        {
            throw new ArgumentNullException(userProfile is null ? nameof(userProfile) : nameof(collection));
        }

        return new DiscogsProfile
        {
            Username = userProfile.Username,
            CollectionUrl = $"https://www.discogs.com/user/{userProfile.Username}/collection",
            ReleaseCount = collection.Pagination.Items
        };
    }
}
