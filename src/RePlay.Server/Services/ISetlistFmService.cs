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
    Task<SetlistFmDataResponse> GetUserConcertsAsync(
        string userId,
        SetlistFmFilter filter,
        IReadOnlyCollection<string>? selectedConcertIds = null,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Fetch a paginated list of attended concerts for user selection workflows.
    /// </summary>
    Task<SetlistConcertsResponse> GetUserConcertsPageAsync(
        string userId,
        SetlistFmFilter filter,
        int pageNumber,
        int pageSize,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Fetch attended concerts for a user with specified filters and return normalized data.
    /// </summary>
    Task<NormalizedDataResponse> GetUserConcertsNormalizedAsync(
        string userId,
        SetlistFmFilter filter,
        IReadOnlyCollection<string>? selectedConcertIds = null,
        CancellationToken cancellationToken = default);
}

/// <summary>
/// Implementation of Setlist.fm API service.
/// </summary>
public sealed class SetlistFmService : ISetlistFmService
{
    private const int MaximumConcerts = 100;
    private const int MaximumTracks = 500;
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

    public async Task<SetlistFmDataResponse> GetUserConcertsAsync(
        string userId,
        SetlistFmFilter filter,
        IReadOnlyCollection<string>? selectedConcertIds = null,
        CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(userId))
        {
            throw new ArgumentException("User ID cannot be empty", nameof(userId));
        }

        var concerts = new List<SetlistConcert>();
        var allTracks = new List<SetlistTrack>();
        var seenTracks = new HashSet<string>(); // For deduplication

        var maxConcerts = GetBoundedLimit(filter.MaxConcerts, 10, MaximumConcerts, nameof(filter.MaxConcerts));
        var maxTracks = GetBoundedLimit(filter.MaxTracks, 100, MaximumTracks, nameof(filter.MaxTracks));
        if (selectedConcertIds is { Count: 0 })
        {
            throw new ArgumentException("Selected concert IDs must not be empty when provided", nameof(selectedConcertIds));
        }

        var hasSelectedConcerts = selectedConcertIds is { Count: > 0 };
        var (startDate, endDate) = ParseDateRange(filter);

        if (hasSelectedConcerts)
        {
            var requestedConcertIds = selectedConcertIds!;
            var concertIds = requestedConcertIds
                .Where(id => !string.IsNullOrWhiteSpace(id))
                .Distinct(StringComparer.OrdinalIgnoreCase)
                .ToList();

            if (concertIds.Count != requestedConcertIds.Count || concertIds.Count > maxConcerts)
            {
                throw new ArgumentException($"Select between 1 and {maxConcerts} unique concerts", nameof(selectedConcertIds));
            }

            await ValidateSelectedConcertsAsync(userId, concertIds, cancellationToken).ConfigureAwait(false);

            foreach (var concertId in concertIds)
            {
                var setlistItem = await GetSetlistAsync(concertId, cancellationToken).ConfigureAwait(false);
                if (string.IsNullOrWhiteSpace(setlistItem.Id))
                {
                    throw new InvalidOperationException("Setlist.fm returned a setlist without an ID.");
                }

                AddConcert(setlistItem, concerts, allTracks, seenTracks, maxTracks, startDate, endDate);
            }

            return new SetlistFmDataResponse
            {
                Concerts = concerts,
                Tracks = allTracks,
                TotalConcerts = concerts.Count,
                TotalTracks = allTracks.Count
            };
        }

        var page = 1;
        var fetchedConcerts = 0;
        while (fetchedConcerts < maxConcerts)
        {
            var payload = await GetAttendedPageAsync(userId, page, 20, cancellationToken).ConfigureAwait(false);
            if (payload.Setlist == null || payload.Setlist.Count == 0)
            {
                break;
            }

            foreach (var setlistItem in payload.Setlist)
            {
                if (fetchedConcerts >= maxConcerts)
                {
                    break;
                }

                if (AddConcert(setlistItem, concerts, allTracks, seenTracks, maxTracks, startDate, endDate))
                {
                    fetchedConcerts++;
                }
            }

            // Check if there are more pages
            if (payload.ItemsPerPage <= 0 ||
                payload.Page >= (payload.Total + payload.ItemsPerPage - 1) / payload.ItemsPerPage)
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

    public async Task<SetlistConcertsResponse> GetUserConcertsPageAsync(
        string userId,
        SetlistFmFilter filter,
        int pageNumber,
        int pageSize,
        CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(userId))
        {
            throw new ArgumentException("User ID cannot be empty", nameof(userId));
        }

        if (pageNumber < 1)
        {
            throw new ArgumentException("Page number must be at least 1", nameof(pageNumber));
        }

        if (pageSize < 1 || pageSize > 100)
        {
            throw new ArgumentException("Page size must be between 1 and 100", nameof(pageSize));
        }

        var (startDate, endDate) = ParseDateRange(filter);
        var payload = await GetAttendedPageAsync(userId, pageNumber, pageSize, cancellationToken).ConfigureAwait(false);

        if (payload?.Setlist == null || payload.Setlist.Count == 0)
        {
            return new SetlistConcertsResponse
            {
                Concerts = [],
                TotalConcerts = payload?.Total ?? 0,
                PageNumber = payload?.Page ?? pageNumber,
                PageSize = payload?.ItemsPerPage ?? pageSize,
                HasNextPage = false,
                HasPreviousPage = (payload?.Page ?? pageNumber) > 1
            };
        }

        var concerts = payload.Setlist
            .Where(item => !string.IsNullOrWhiteSpace(item.Id))
            .Where(item => PassesDateRange(item.EventDate, startDate, endDate))
            .Select(item => new SetlistConcert
            {
                Id = item.Id!,
                Artist = item.Artist?.Name ?? "Unknown Artist",
                Date = item.EventDate,
                Venue = item.Venue?.Name,
                City = item.Venue?.City?.Name,
                Country = item.Venue?.City?.Country?.Name,
                Tracks = []
            })
            .ToList();

        var totalPages = payload.ItemsPerPage > 0
            ? (payload.Total + payload.ItemsPerPage - 1) / payload.ItemsPerPage
            : payload.Page;

        return new SetlistConcertsResponse
        {
            Concerts = concerts,
            TotalConcerts = payload.Total,
            PageNumber = payload.Page,
            PageSize = payload.ItemsPerPage,
            HasNextPage = payload.Page < totalPages,
            HasPreviousPage = payload.Page > 1
        };
    }

    public async Task<NormalizedDataResponse> GetUserConcertsNormalizedAsync(
        string userId,
        SetlistFmFilter filter,
        IReadOnlyCollection<string>? selectedConcertIds = null,
        CancellationToken cancellationToken = default)
    {
        var data = await GetUserConcertsAsync(userId, filter, selectedConcertIds, cancellationToken).ConfigureAwait(false);

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

    private async Task<SetlistAttendedResponse> GetAttendedPageAsync(
        string userId,
        int page,
        int pageSize,
        CancellationToken cancellationToken)
    {
        using var request = CreateRequest($"user/{Uri.EscapeDataString(userId)}/attended?p={page}&perPage={pageSize}");
        var response = await _httpClient.SendAsync(request, cancellationToken).ConfigureAwait(false);
        response.EnsureSuccessStatusCode();

        await using var stream = await response.Content.ReadAsStreamAsync(cancellationToken).ConfigureAwait(false);
        return await JsonSerializer.DeserializeAsync<SetlistAttendedResponse>(stream, SerializerOptions, cancellationToken).ConfigureAwait(false)
            ?? throw new InvalidOperationException("Setlist.fm returned an invalid attended concerts response.");
    }

    private async Task<SetlistAttendedResponse.SetlistItem> GetSetlistAsync(string concertId, CancellationToken cancellationToken)
    {
        using var request = CreateRequest($"setlist/{Uri.EscapeDataString(concertId)}");
        var response = await _httpClient.SendAsync(request, cancellationToken).ConfigureAwait(false);
        response.EnsureSuccessStatusCode();

        await using var stream = await response.Content.ReadAsStreamAsync(cancellationToken).ConfigureAwait(false);
        return await JsonSerializer.DeserializeAsync<SetlistAttendedResponse.SetlistItem>(stream, SerializerOptions, cancellationToken).ConfigureAwait(false)
            ?? throw new InvalidOperationException("Setlist.fm returned an invalid setlist response.");
    }

    private async Task ValidateSelectedConcertsAsync(
        string userId,
        IReadOnlyCollection<string> concertIds,
        CancellationToken cancellationToken)
    {
        var remainingIds = concertIds.ToHashSet(StringComparer.OrdinalIgnoreCase);
        var page = 1;

        while (remainingIds.Count > 0)
        {
            var payload = await GetAttendedPageAsync(userId, page, 20, cancellationToken).ConfigureAwait(false);
            foreach (var item in payload.Setlist ?? [])
            {
                if (!string.IsNullOrWhiteSpace(item.Id))
                {
                    remainingIds.Remove(item.Id);
                }
            }

            if (remainingIds.Count == 0 ||
                payload.ItemsPerPage <= 0 ||
                payload.Page >= (payload.Total + payload.ItemsPerPage - 1) / payload.ItemsPerPage)
            {
                break;
            }

            page++;
        }

        if (remainingIds.Count > 0)
        {
            throw new ArgumentException("Selected concerts must belong to the requested Setlist.fm user", nameof(concertIds));
        }
    }

    private static (DateTime? StartDate, DateTime? EndDate) ParseDateRange(SetlistFmFilter filter)
    {
        DateTime? startDate = ParseIsoDate(filter.StartDate, nameof(filter.StartDate));
        DateTime? endDate = ParseIsoDate(filter.EndDate, nameof(filter.EndDate));
        if (startDate > endDate)
        {
            throw new ArgumentException("Start date must be before end date", nameof(filter));
        }

        return (startDate, endDate);
    }

    private static DateTime? ParseIsoDate(string? value, string parameterName)
    {
        if (string.IsNullOrWhiteSpace(value))
        {
            return null;
        }

        if (!DateTime.TryParseExact(value, "yyyy-MM-dd", System.Globalization.CultureInfo.InvariantCulture,
                System.Globalization.DateTimeStyles.None, out var parsed))
        {
            throw new ArgumentException("Dates must use ISO 8601 format (yyyy-MM-dd)", parameterName);
        }

        return parsed;
    }

    private static int GetBoundedLimit(int value, int defaultValue, int maximum, string parameterName)
    {
        var limit = value > 0 ? value : defaultValue;
        if (limit > maximum)
        {
            throw new ArgumentException($"Value must be between 1 and {maximum}", parameterName);
        }

        return limit;
    }

    private static bool AddConcert(
        SetlistAttendedResponse.SetlistItem setlistItem,
        List<SetlistConcert> concerts,
        List<SetlistTrack> allTracks,
        HashSet<string> seenTracks,
        int maxTracks,
        DateTime? startDate,
        DateTime? endDate)
    {
        if (string.IsNullOrWhiteSpace(setlistItem.Id))
        {
            return false;
        }

        if (!PassesDateRange(setlistItem.EventDate, startDate, endDate))
        {
            return false;
        }

        var concertTracks = new List<SetlistTrack>();
        foreach (var set in setlistItem.Sets?.Set ?? [])
        {
            foreach (var song in set.Song ?? [])
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

                var trackKey = $"{track.Artist}|{track.Name}".ToLowerInvariant();
                if (allTracks.Count < maxTracks && seenTracks.Add(trackKey))
                {
                    allTracks.Add(track);
                }
            }
        }

        concerts.Add(new SetlistConcert
        {
            Id = setlistItem.Id,
            Artist = setlistItem.Artist?.Name ?? "Unknown Artist",
            Date = setlistItem.EventDate,
            Venue = setlistItem.Venue?.Name,
            City = setlistItem.Venue?.City?.Name,
            Country = setlistItem.Venue?.City?.Country?.Name,
            Tracks = concertTracks
        });
        return true;
    }

    private static bool PassesDateRange(string? eventDate, DateTime? startDate, DateTime? endDate)
    {
        if (string.IsNullOrWhiteSpace(eventDate))
        {
            return true;
        }

        if (!DateTime.TryParseExact(
                eventDate,
                "dd-MM-yyyy",
                System.Globalization.CultureInfo.InvariantCulture,
                System.Globalization.DateTimeStyles.None,
                out var concertDate))
        {
            return true;
        }

        if (startDate.HasValue && concertDate < startDate.Value)
        {
            return false;
        }

        if (endDate.HasValue && concertDate > endDate.Value)
        {
            return false;
        }

        return true;
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
