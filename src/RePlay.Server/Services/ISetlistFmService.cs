using System.Net;
using System.Net.Http.Headers;
using System.Text.Json;
using System.Text.Json.Serialization;
using Microsoft.Extensions.Options;
using RePlay.Server.Configuration;
using RePlay.Server.Models;

namespace RePlay.Server.Services;

/// <summary>
/// Service for Setlist.fm API interactions.
/// </summary>
public interface ISetlistFmService
{
    /// <summary>
    /// Validate a Setlist.fm username or ID and fetch profile information.
    /// </summary>
    Task<SetlistUser?> GetUserAsync(string usernameOrId, CancellationToken cancellationToken = default);

    /// <summary>
    /// Fetch attended concerts for a user with specified filters and return raw concert data.
    /// </summary>
    Task<SetlistFmDataResponse> GetUserConcertsAsync(string userId, SetlistFmFilter filter, CancellationToken cancellationToken = default);

    /// <summary>
    /// Fetch attended concerts for a user with specified filters and return normalized data.
    /// </summary>
    Task<NormalizedDataResponse> GetUserConcertsNormalizedAsync(string userId, SetlistFmFilter filter, CancellationToken cancellationToken = default);
}

/// <summary>
/// Implementation of Setlist.fm API service.
/// </summary>
public sealed class SetlistFmService : ISetlistFmService
{
    private static readonly JsonSerializerOptions SerializerOptions = new();

    private readonly HttpClient _httpClient;
    private readonly SetlistFmOptions _options;

    public SetlistFmService(HttpClient httpClient, IOptions<SetlistFmOptions> options)
    {
        _httpClient = httpClient;
        _options = options.Value;

        if (string.IsNullOrWhiteSpace(_options.ApiKey))
        {
            throw new InvalidOperationException(
                "Setlist.fm API key is not configured. Please set 'SetlistFm:ApiKey' in appsettings.");
        }

        if (_httpClient.BaseAddress == null)
        {
            _httpClient.BaseAddress = new Uri(_options.ApiUrl.TrimEnd('/') + "/");
        }
    }

    public async Task<SetlistUser?> GetUserAsync(string usernameOrId, CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(usernameOrId))
        {
            throw new ArgumentException("Username or ID cannot be empty", nameof(usernameOrId));
        }

        using var request = CreateRequest($"user/{Uri.EscapeDataString(usernameOrId)}");
        var response = await _httpClient.SendAsync(request, cancellationToken).ConfigureAwait(false);

        if (response.StatusCode == HttpStatusCode.NotFound)
        {
            return null;
        }

        if (!response.IsSuccessStatusCode)
        {
            return null;
        }

        await using var stream = await response.Content.ReadAsStreamAsync(cancellationToken).ConfigureAwait(false);
        var payload = await JsonSerializer.DeserializeAsync<SetlistUserResponse>(stream, SerializerOptions, cancellationToken)
            .ConfigureAwait(false);

        if (payload?.UserId is null)
        {
            return null;
        }

        // Fetch attended concerts count from the attended endpoint
        var attendedCount = await GetAttendedConcertsCountAsync(payload.UserId, cancellationToken).ConfigureAwait(false);

        return new SetlistUser
        {
            UserId = payload.UserId,
            DisplayName = string.IsNullOrWhiteSpace(payload.FullName) ? payload.UserId : payload.FullName,
            Url = payload.Url,
            AttendedConcerts = attendedCount
        };
    }

    public async Task<SetlistFmDataResponse> GetUserConcertsAsync(string userId, SetlistFmFilter filter, CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(userId))
        {
            throw new ArgumentException("User ID cannot be empty", nameof(userId));
        }

        var concerts = new List<SetlistConcert>();
        var allTracks = new List<SetlistTrack>();
        var seenTracks = new HashSet<string>(); // For deduplication

        var page = 1;
        var fetchedConcerts = 0;
        var maxConcerts = filter.MaxConcerts > 0 ? filter.MaxConcerts : 10;

        DateTime? startDate = null;
        DateTime? endDate = null;

        if (!string.IsNullOrWhiteSpace(filter.StartDate))
        {
            if (!DateTime.TryParse(filter.StartDate, out var parsedStart))
            {
                throw new ArgumentException("Invalid start date format", nameof(filter.StartDate));
            }
            startDate = parsedStart;
        }

        if (!string.IsNullOrWhiteSpace(filter.EndDate))
        {
            if (!DateTime.TryParse(filter.EndDate, out var parsedEnd))
            {
                throw new ArgumentException("Invalid end date format", nameof(filter.EndDate));
            }
            endDate = parsedEnd;
        }

        while (fetchedConcerts < maxConcerts)
        {
            using var request = CreateRequest($"user/{Uri.EscapeDataString(userId)}/attended?p={page}");
            var response = await _httpClient.SendAsync(request, cancellationToken).ConfigureAwait(false);

            if (!response.IsSuccessStatusCode)
            {
                break;
            }

            await using var stream = await response.Content.ReadAsStreamAsync(cancellationToken).ConfigureAwait(false);
            var payload = await JsonSerializer.DeserializeAsync<SetlistAttendedResponse>(stream, SerializerOptions, cancellationToken)
                .ConfigureAwait(false);

            if (payload?.Setlist == null || payload.Setlist.Count == 0)
            {
                break;
            }

            foreach (var setlistItem in payload.Setlist)
            {
                if (fetchedConcerts >= maxConcerts)
                {
                    break;
                }

                // Parse concert date and apply date filters
                DateTime? concertDate = null;
                if (!string.IsNullOrWhiteSpace(setlistItem.EventDate))
                {
                    if (DateTime.TryParseExact(setlistItem.EventDate, "dd-MM-yyyy", 
                        System.Globalization.CultureInfo.InvariantCulture, 
                        System.Globalization.DateTimeStyles.None, out var parsed))
                    {
                        concertDate = parsed;
                    }
                }

                // Apply date range filtering
                if (startDate.HasValue && concertDate.HasValue && concertDate < startDate)
                {
                    continue;
                }

                if (endDate.HasValue && concertDate.HasValue && concertDate > endDate)
                {
                    continue;
                }

                var concertTracks = new List<SetlistTrack>();

                if (setlistItem.Sets?.Set != null)
                {
                    foreach (var set in setlistItem.Sets.Set)
                    {
                        if (set.Song != null)
                        {
                            foreach (var song in set.Song)
                            {
                                if (string.IsNullOrWhiteSpace(song.Name))
                                {
                                    continue;
                                }

                                var track = new SetlistTrack
                                {
                                    Name = song.Name,
                                    Artist = setlistItem.Artist?.Name ?? "Unknown Artist",
                                    ConcertDate = setlistItem.EventDate,
                                    Venue = setlistItem.Venue?.Name,
                                    City = setlistItem.Venue?.City?.Name,
                                    Country = setlistItem.Venue?.City?.Country?.Name
                                };

                                concertTracks.Add(track);

                                // Deduplicate tracks: artist + track name (case-insensitive)
                                var trackKey = $"{track.Artist}|{track.Name}".ToLowerInvariant();
                                if (!seenTracks.Contains(trackKey) && allTracks.Count < filter.MaxTracks)
                                {
                                    seenTracks.Add(trackKey);
                                    allTracks.Add(track);
                                }
                            }
                        }
                    }
                }

                concerts.Add(new SetlistConcert
                {
                    Id = setlistItem.Id ?? Guid.NewGuid().ToString(),
                    Artist = setlistItem.Artist?.Name ?? "Unknown Artist",
                    Date = setlistItem.EventDate,
                    Venue = setlistItem.Venue?.Name,
                    City = setlistItem.Venue?.City?.Name,
                    Country = setlistItem.Venue?.City?.Country?.Name,
                    Tracks = concertTracks
                });

                fetchedConcerts++;
            }

            // Check if there are more pages
            if (payload.Page >= (payload.Total + payload.ItemsPerPage - 1) / payload.ItemsPerPage)
            {
                break;
            }

            page++;
        }

        return new SetlistFmDataResponse
        {
            Concerts = concerts,
            Tracks = allTracks,
            TotalConcerts = concerts.Count,
            TotalTracks = allTracks.Count
        };
    }

    public async Task<NormalizedDataResponse> GetUserConcertsNormalizedAsync(string userId, SetlistFmFilter filter, CancellationToken cancellationToken = default)
    {
        var data = await GetUserConcertsAsync(userId, filter, cancellationToken).ConfigureAwait(false);

        var normalizedTracks = data.Tracks.Select(track => new NormalizedTrack
        {
            Name = track.Name,
            Artist = track.Artist,
            Album = null, // Setlist.fm doesn't provide album info
            Source = "setlistfm",
            SourceMetadata = new Dictionary<string, object?>
            {
                ["concertDate"] = track.ConcertDate,
                ["venue"] = track.Venue,
                ["city"] = track.City,
                ["country"] = track.Country
            }
        }).ToList();

        return new NormalizedDataResponse
        {
            DataType = "Tracks",
            Tracks = normalizedTracks,
            Albums = [],
            Artists = [],
            TotalResults = normalizedTracks.Count,
            Source = "setlistfm"
        };
    }

    private async Task<int> GetAttendedConcertsCountAsync(string userId, CancellationToken cancellationToken)
    {
        try
        {
            using var request = CreateRequest($"user/{Uri.EscapeDataString(userId)}/attended?perPage=1");
            var response = await _httpClient.SendAsync(request, cancellationToken).ConfigureAwait(false);

            if (!response.IsSuccessStatusCode)
            {
                return 0;
            }

            await using var stream = await response.Content.ReadAsStreamAsync(cancellationToken).ConfigureAwait(false);
            var payload = await JsonSerializer.DeserializeAsync<SetlistAttendedResponse>(stream, SerializerOptions, cancellationToken)
                .ConfigureAwait(false);

            return payload?.Total ?? 0;
        }
        catch
        {
            // If attended endpoint fails, return 0
            return 0;
        }
    }

    private HttpRequestMessage CreateRequest(string relativePath)
    {
        var request = new HttpRequestMessage(HttpMethod.Get, relativePath);
        request.Headers.Accept.Add(new MediaTypeWithQualityHeaderValue("application/json"));
        request.Headers.UserAgent.ParseAdd(_options.UserAgent);
        request.Headers.Add("x-api-key", _options.ApiKey);
        return request;
    }

    private sealed record SetlistUserResponse
    {
        [JsonPropertyName("userId")]
        public string? UserId { get; init; }

        [JsonPropertyName("fullname")]
        public string? FullName { get; init; }

        [JsonPropertyName("url")]
        public string? Url { get; init; }
    }

    private sealed record SetlistAttendedResponse
    {
        [JsonPropertyName("setlist")]
        public List<SetlistItem>? Setlist { get; init; }

        [JsonPropertyName("total")]
        public int Total { get; init; }

        [JsonPropertyName("page")]
        public int Page { get; init; }

        [JsonPropertyName("itemsPerPage")]
        public int ItemsPerPage { get; init; }

        public sealed record SetlistItem
        {
            [JsonPropertyName("id")]
            public string? Id { get; init; }

            [JsonPropertyName("eventDate")]
            public string? EventDate { get; init; }

            [JsonPropertyName("artist")]
            public ArtistInfo? Artist { get; init; }

            [JsonPropertyName("venue")]
            public VenueInfo? Venue { get; init; }

            [JsonPropertyName("sets")]
            public SetsInfo? Sets { get; init; }
        }

        public sealed record ArtistInfo
        {
            [JsonPropertyName("name")]
            public string? Name { get; init; }
        }

        public sealed record VenueInfo
        {
            [JsonPropertyName("name")]
            public string? Name { get; init; }

            [JsonPropertyName("city")]
            public CityInfo? City { get; init; }
        }

        public sealed record CityInfo
        {
            [JsonPropertyName("name")]
            public string? Name { get; init; }

            [JsonPropertyName("country")]
            public CountryInfo? Country { get; init; }
        }

        public sealed record CountryInfo
        {
            [JsonPropertyName("name")]
            public string? Name { get; init; }
        }

        public sealed record SetsInfo
        {
            [JsonPropertyName("set")]
            public List<SetInfo>? Set { get; init; }
        }

        public sealed record SetInfo
        {
            [JsonPropertyName("song")]
            public List<SongInfo>? Song { get; init; }
        }

        public sealed record SongInfo
        {
            [JsonPropertyName("name")]
            public string? Name { get; init; }
        }
    }
}
