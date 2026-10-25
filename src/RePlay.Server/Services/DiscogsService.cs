using System.Net.Http.Headers;
using System.Net;
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

        var username = NormalizeUsername(usernameOrCollectionId);
        var profile = await TryGetProfileAsync("users", username, cancellationToken);
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

        if (filter.MaxTracks is < 1 or > 500)
        {
            throw new ArgumentException("MaxTracks must be between 1 and 500", nameof(filter));
        }

        // Resolve the user without fetching and then discarding the first collection page.
        var username = NormalizeUsername(usernameOrCollectionId);
        var profile = await TryGetProfileAsync("users", username, cancellationToken);
        if (profile == null)
        {
            return null;
        }

        // Fetch collection releases with pagination
        var tracks = new List<DiscogsTrack>();
        var releases = new List<DiscogsRelease>();
        int page = 1;
        int totalPages = 1;

        while (page <= totalPages)
        {
            if (page > 100)
            {
                throw new InvalidOperationException("Discogs collection scan reached the 100-page limit. Narrow the collection filters or request fewer tracks.");
            }

            var collectionResponse = await TryGetCollectionAsync(profile.Username, page, cancellationToken);
            if (collectionResponse == null)
            {
                return null;
            }

            totalPages = collectionResponse.Pagination.Pages;
            page++;

            foreach (var release in ApplyFilters(collectionResponse.Releases, filter))
            {
                if (release.BasicInformation == null)
                {
                    continue;
                }

                // Create release object from basic info
                var basicInfo = release.BasicInformation;
                var primaryArtist = basicInfo.Artists?.FirstOrDefault()?.Name ?? "Unknown";
                var format = basicInfo.Formats?.FirstOrDefault()?.Name;

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

            if (tracks.Count >= filter.MaxTracks)
            {
                break;
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
        using var request = new HttpRequestMessage(HttpMethod.Get, $"{path}/{Uri.EscapeDataString(NormalizeUsername(identifier))}");
        using var userResponse = await _httpClient.SendAsync(request, cancellationToken);
        if (userResponse.StatusCode == HttpStatusCode.NotFound)
        {
            return null;
        }
        userResponse.EnsureSuccessStatusCode();

        await using var userStream = await userResponse.Content.ReadAsStreamAsync(cancellationToken);
        return await JsonSerializer.DeserializeAsync<DiscogsUserResponse>(userStream, cancellationToken: cancellationToken);
    }

    private async Task<DiscogsCollectionResponse?> TryGetCollectionAsync(string username, CancellationToken cancellationToken)
    {
        return await TryGetCollectionAsync(username, 1, cancellationToken);
    }

    private async Task<DiscogsCollectionResponse?> TryGetCollectionAsync(string username, int page, CancellationToken cancellationToken)
    {
        using var request = new HttpRequestMessage(HttpMethod.Get, $"users/{Uri.EscapeDataString(NormalizeUsername(username))}/collection/folders/0/releases?per_page=100&page={page}");
        using var collectionResponse = await _httpClient.SendAsync(request, cancellationToken);
        if (collectionResponse.StatusCode == HttpStatusCode.NotFound)
        {
            return null;
        }
        collectionResponse.EnsureSuccessStatusCode();

        await using var collectionStream = await collectionResponse.Content.ReadAsStreamAsync(cancellationToken);
        return await JsonSerializer.DeserializeAsync<DiscogsCollectionResponse>(collectionStream, cancellationToken: cancellationToken);
    }

    private async Task<DiscogsReleaseDetail?> TryGetReleaseAsync(int releaseId, CancellationToken cancellationToken)
    {
        if (releaseId <= 0)
        {
            throw new JsonException("Discogs returned an invalid release ID.");
        }
        using var request = new HttpRequestMessage(HttpMethod.Get, $"releases/{releaseId}");
        using var releaseResponse = await _httpClient.SendAsync(request, cancellationToken);
        if (releaseResponse.StatusCode == HttpStatusCode.NotFound)
        {
            return null;
        }
        releaseResponse.EnsureSuccessStatusCode();

        await using var releaseStream = await releaseResponse.Content.ReadAsStreamAsync(cancellationToken);
        return await JsonSerializer.DeserializeAsync<DiscogsReleaseDetail>(releaseStream, cancellationToken: cancellationToken);
    }

    private static string NormalizeUsername(string value)
    {
        var username = value.Trim();
        // Uri canonicalizes dot segments; reject traversal before that normalization.
        var decoded = Uri.UnescapeDataString(username);
        if (decoded.Contains('\\') || decoded.Split('/').Any(segment => segment is "." or ".."))
        {
            throw new ArgumentException("Invalid Discogs collection path", nameof(value));
        }
        if (Uri.TryCreate(username, UriKind.Absolute, out var uri))
        {
            var segments = uri.AbsolutePath.Trim('/').Split('/');
            if ((uri.Scheme != Uri.UriSchemeHttps && uri.Scheme != Uri.UriSchemeHttp) ||
                (uri.Host != "www.discogs.com" && uri.Host != "discogs.com") ||
                !uri.IsDefaultPort || uri.UserInfo.Length > 0 || uri.Query.Length > 0 || uri.Fragment.Length > 0 ||
                segments.Length != 3 || (segments[0] != "user" && segments[0] != "users") || segments[2] != "collection")
            {
                throw new ArgumentException("Use a Discogs username or a discogs.com/user/username/collection URL", nameof(value));
            }
            username = Uri.UnescapeDataString(segments[1]);
        }

        if (username.Length is < 1 or > 100 || username is "." or ".." ||
            !Regex.IsMatch(username, @"\A[A-Za-z0-9_.-]+\z"))
        {
            throw new ArgumentException("Invalid Discogs username", nameof(value));
        }
        return username;
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
                Name = track.Title!,
                Artist = track.Artists?.FirstOrDefault()?.Name ?? primaryArtist,
                Album = release.Title,
                ReleaseYear = release.Year
            })
            .ToList();
    }
}
