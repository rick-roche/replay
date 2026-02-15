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

    public async Task<DiscogsDataResponse?> GetCollectionAsync(string usernameOrCollectionId, DiscogsFilter filter, CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(usernameOrCollectionId))
        {
            throw new ArgumentException("Username or collection ID is required", nameof(usernameOrCollectionId));
        }

        if (filter == null)
        {
            throw new ArgumentNullException(nameof(filter));
        }

        // Get profile to determine username
        var profile = await GetProfileAsync(usernameOrCollectionId, cancellationToken);
        if (profile == null)
        {
            return null;
        }

        // Fetch collection releases with pagination
        var allReleases = new List<DiscogsCollectionItem>();
        int page = 1;
        int totalPages = 1;

        while (page <= totalPages)
        {
            var collectionResponse = await TryGetCollectionAsync(profile.Username, page, cancellationToken);
            if (collectionResponse == null)
            {
                return null;
            }

            allReleases.AddRange(collectionResponse.Releases);
            totalPages = collectionResponse.Pagination.Pages;
            page++;
        }

        // Apply filters and extract tracks
        var filteredReleases = ApplyFilters(allReleases, filter).ToList();
        var tracks = new List<DiscogsTrack>();
        var releases = new List<DiscogsRelease>();

        foreach (var release in filteredReleases)
        {
            if (release.BasicInformation == null)
            {
                continue;
            }

            // Create release object from basic info
            var basicInfo = release.BasicInformation;
            var primaryArtist = basicInfo.Artists?.FirstOrDefault()?.Name ?? "Unknown";
            var format = basicInfo.Formats?.FirstOrDefault()?.Name;
            var dateAddedYear = string.IsNullOrEmpty(release.DateAdded) ? null : (int?)DateTime.Parse(release.DateAdded).Year;

            var releaseObj = new DiscogsRelease
            {
                Id = basicInfo.Id,
                Title = basicInfo.Title ?? "Unknown",
                Artist = primaryArtist,
                Year = basicInfo.Year,
                Format = format,
                DateAdded = release.DateAdded,
                Tracks = []
            };

            // Extract tracks from the release
            var releaseDetail = await TryGetReleaseAsync(release.BasicInformation.Id, cancellationToken);
            if (releaseDetail != null)
            {
                var releaseTracks = ExtractTracksFromRelease(releaseDetail);
                releaseObj = releaseObj with { Tracks = releaseTracks };
                tracks.AddRange(releaseTracks);
                releases.Add(releaseObj);

                if (tracks.Count >= filter.MaxTracks)
                {
                    tracks = tracks.Take(filter.MaxTracks).ToList();
                    break;
                }
            }
        }

        return new DiscogsDataResponse
        {
            Releases = releases,
            Tracks = tracks,
            TotalReleases = releases.Count,
            TotalTracks = tracks.Count
        };
    }

    public async Task<NormalizedDataResponse?> GetCollectionNormalizedAsync(string usernameOrCollectionId, DiscogsFilter filter, CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(usernameOrCollectionId))
        {
            throw new ArgumentException("Username or collection ID is required", nameof(usernameOrCollectionId));
        }

        if (filter == null)
        {
            throw new ArgumentNullException(nameof(filter));
        }

        var data = await GetCollectionAsync(usernameOrCollectionId, filter, cancellationToken);
        if (data == null)
        {
            return null;
        }

        var normalizedTracks = data.Tracks.Select(track => new NormalizedTrack
        {
            Name = track.Name,
            Artist = track.Artist,
            Album = track.Album,
            Source = "discogs",
            SourceMetadata = new Dictionary<string, object?>
            {
                ["year"] = track.ReleaseYear,
                ["format"] = filter.MediaFormat?.ToString()
            }
        }).ToList();

        return new NormalizedDataResponse
        {
            DataType = "Tracks",
            Tracks = normalizedTracks,
            Albums = [],
            Artists = [],
            TotalResults = normalizedTracks.Count,
            Source = "discogs"
        };
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
        return await TryGetCollectionAsync(username, 1, cancellationToken);
    }

    private async Task<DiscogsCollectionResponse?> TryGetCollectionAsync(string username, int page, CancellationToken cancellationToken)
    {
        var collectionResponse = await _httpClient.GetAsync($"users/{username}/collection/folders/0/releases?per_page=100&page={page}", cancellationToken);
        if (!collectionResponse.IsSuccessStatusCode)
        {
            return null;
        }

        await using var collectionStream = await collectionResponse.Content.ReadAsStreamAsync(cancellationToken);
        return await JsonSerializer.DeserializeAsync<DiscogsCollectionResponse>(collectionStream, cancellationToken: cancellationToken);
    }

    private async Task<DiscogsReleaseDetail?> TryGetReleaseAsync(int releaseId, CancellationToken cancellationToken)
    {
        var releaseResponse = await _httpClient.GetAsync($"releases/{releaseId}", cancellationToken);
        if (!releaseResponse.IsSuccessStatusCode)
        {
            return null;
        }

        await using var releaseStream = await releaseResponse.Content.ReadAsStreamAsync(cancellationToken);
        return await JsonSerializer.DeserializeAsync<DiscogsReleaseDetail>(releaseStream, cancellationToken: cancellationToken);
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

    private static IEnumerable<DiscogsCollectionItem> ApplyFilters(IEnumerable<DiscogsCollectionItem> releases, DiscogsFilter filter)
    {
        var filtered = releases.AsEnumerable();

        // Filter by release year
        if (filter.MinReleaseYear.HasValue || filter.MaxReleaseYear.HasValue)
        {
            filtered = filtered.Where(r =>
            {
                var year = r.BasicInformation?.Year;
                if (!year.HasValue)
                {
                    return filter.MinReleaseYear == null && filter.MaxReleaseYear == null;
                }

                bool meetsMin = !filter.MinReleaseYear.HasValue || year >= filter.MinReleaseYear.Value;
                bool meetsMax = !filter.MaxReleaseYear.HasValue || year <= filter.MaxReleaseYear.Value;
                return meetsMin && meetsMax;
            });
        }

        // Filter by media format
        if (filter.MediaFormat.HasValue)
        {
            var formatName = filter.MediaFormat.Value.ToString();
            filtered = filtered.Where(r =>
                r.BasicInformation?.Formats?.Any(f =>
                    f.Name?.Equals(formatName, StringComparison.OrdinalIgnoreCase) ?? false) ?? false);
        }

        // Filter by year added to collection
        if (filter.MinYearAdded.HasValue || filter.MaxYearAdded.HasValue)
        {
            filtered = filtered.Where(r =>
            {
                if (!DateTime.TryParse(r.DateAdded, out var dateAdded))
                {
                    return false;
                }

                var year = dateAdded.Year;
                bool meetsMin = !filter.MinYearAdded.HasValue || year >= filter.MinYearAdded.Value;
                bool meetsMax = !filter.MaxYearAdded.HasValue || year <= filter.MaxYearAdded.Value;
                return meetsMin && meetsMax;
            });
        }

        return filtered;
    }

    private static List<DiscogsTrack> ExtractTracksFromRelease(DiscogsReleaseDetail release)
    {
        if (release?.Tracklist == null || release.Tracklist.Count == 0)
        {
            return [];
        }

        var primaryArtist = release.Artists?.FirstOrDefault()?.Name ?? "Unknown";

        return release.Tracklist
            .Where(track => !string.IsNullOrWhiteSpace(track.Title))
            .Select(track => new DiscogsTrack
            {
                Name = track.Title,
                Artist = track.Artists?.FirstOrDefault()?.Name ?? primaryArtist,
                Album = release.Title,
                ReleaseYear = release.Year
            })
            .ToList();
    }
}
