using System.Globalization;
using System.Text.Json;
using System.Linq;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using RePlay.Server.Configuration;
using RePlay.Server.Models;

namespace RePlay.Server.Services;

/// <summary>
/// Service for Last.fm API interactions.
/// </summary>
public interface ILastfmService
{
    /// <summary>
    /// Validate a Last.fm username and get user info.
    /// </summary>
    Task<LastfmUser?> GetUserAsync(string username, CancellationToken cancellationToken = default);

    /// <summary>
    /// Fetch Last.fm data (tracks, albums, or artists) for a user with specified filters.
    /// </summary>
    Task<LastfmDataResponse?> GetUserDataAsync(string username, LastfmFilter filter, CancellationToken cancellationToken = default);

    /// <summary>
    /// Fetch Last.fm data and normalize it to a canonical format for matching.
    /// </summary>
    Task<NormalizedDataResponse?> GetUserDataNormalizedAsync(string username, LastfmFilter filter, CancellationToken cancellationToken = default);
}

/// <summary>
/// Implementation of Last.fm API service.
/// </summary>
public sealed class LastfmService : ILastfmService
{
    private readonly HttpClient _httpClient;
    private readonly LastfmOptions _options;
    private readonly ILogger<LastfmService> _logger;

    public LastfmService(HttpClient httpClient, IOptions<LastfmOptions> options, ILogger<LastfmService> logger)
    {
        _httpClient = httpClient;
        _options = options.Value;
        _logger = logger;

        if (string.IsNullOrWhiteSpace(_options.ApiKey))
        {
            throw new InvalidOperationException(
                "Last.fm API key is not configured. Please set 'Lastfm:ApiKey' in appsettings.");
        }
    }

    public async Task<LastfmUser?> GetUserAsync(string username, CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(username))
        {
            throw new ArgumentException("Username cannot be empty", nameof(username));
        }

        var url = BuildUrl("user.getinfo", new Dictionary<string, string>
        {
            { "user", username }
        });

        LogRequest("user.getinfo", new Dictionary<string, string>
        {
            { "user", username }
        });

        try
        {
            var response = await _httpClient.GetAsync(url, cancellationToken);

            var content = await response.Content.ReadAsStringAsync(cancellationToken);
            
            if (string.IsNullOrWhiteSpace(content))
            {
                return null;
            }

            var json = JsonDocument.Parse(content);
            var root = json.RootElement;

            // Check for error in response
            if (root.TryGetProperty("error", out var errorElement))
            {
                // Last.fm API returns error codes for invalid users, etc.
                return null;
            }

            if (!root.TryGetProperty("user", out var userElement))
            {
                return null;
            }

            var playCountElement = userElement.GetProperty("playcount");
            var playcountStr = playCountElement.ValueKind == JsonValueKind.String 
                ? playCountElement.GetString() 
                : playCountElement.GetInt32().ToString();

            var profileUrl = userElement.TryGetProperty("url", out var urlElement)
                ? urlElement.GetString()
                : null;

            var user = new LastfmUser
            {
                Username = userElement.GetProperty("name").GetString() ?? username,
                PlayCount = int.TryParse(playcountStr, out var count) ? count : 0,
                Registered = userElement.GetProperty("registered").GetProperty("unixtime").GetString() ?? string.Empty,
                ProfileUrl = string.IsNullOrWhiteSpace(profileUrl)
                    ? $"https://www.last.fm/user/{Uri.EscapeDataString(username)}"
                    : profileUrl!
            };

            return user;
        }
        catch (Exception)
        {
            // Any parsing error or connection error returns null
            return null;
        }
    }

    public async Task<LastfmDataResponse?> GetUserDataAsync(string username, LastfmFilter filter, CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(username))
        {
            throw new ArgumentException("Username cannot be empty", nameof(username));
        }

        if (filter.TimePeriod == LastfmTimePeriod.Custom && 
            (string.IsNullOrWhiteSpace(filter.CustomStartDate) || string.IsNullOrWhiteSpace(filter.CustomEndDate)))
        {
            throw new ArgumentException("Custom time period requires both start and end dates", nameof(filter));
        }

        try
        {
            var limit = Math.Min(filter.MaxResults, 500); // Last.fm API has a max of 500

            var parameters = new Dictionary<string, string>
            {
                { "user", username },
                { "limit", limit.ToString() }
            };

            string method;

            if (filter.TimePeriod == LastfmTimePeriod.Custom)
            {
                var (from, to) = ConvertCustomDatesToEpoch(filter);

                method = filter.DataType switch
                {
                    LastfmDataType.Tracks => "user.getWeeklyTrackChart",
                    LastfmDataType.Albums => "user.getWeeklyAlbumChart",
                    LastfmDataType.Artists => "user.getWeeklyArtistChart",
                    _ => throw new ArgumentException("Invalid data type", nameof(filter))
                };

                parameters["from"] = from;
                parameters["to"] = to;
            }
            else
            {
                method = filter.DataType switch
                {
                    LastfmDataType.Tracks => "user.getTopTracks",
                    LastfmDataType.Albums => "user.getTopAlbums",
                    LastfmDataType.Artists => "user.getTopArtists",
                    _ => throw new ArgumentException("Invalid data type", nameof(filter))
                };

                var period = ConvertTimePeriodToLastfmPeriod(filter.TimePeriod);
                parameters["period"] = period;
                parameters["extended"] = "1";
            }

            var url = BuildUrl(method, parameters);
            LogRequest(method, parameters);
            var response = await _httpClient.GetAsync(url, cancellationToken);

            var content = await response.Content.ReadAsStringAsync(cancellationToken);

            if (string.IsNullOrWhiteSpace(content))
            {
                return null;
            }

            var json = JsonDocument.Parse(content);
            var root = json.RootElement;

            // Check for error in response
            if (root.TryGetProperty("error", out _))
            {
                return null;
            }

            var result = new LastfmDataResponse
            {
                DataType = filter.DataType,
                Tracks = [],
                Albums = [],
                Artists = []
            };

            if (filter.DataType == LastfmDataType.Tracks &&
                TryGetResultElement(root, "toptracks", "weeklytrackchart", out var tracksElement))
            {
                var tracks = ParseTracks(tracksElement);
                result = result with { Tracks = tracks, TotalResults = tracks.Count };
            }
            else if (filter.DataType == LastfmDataType.Albums &&
                     TryGetResultElement(root, "topalbums", "weeklyalbumchart", out var albumsElement))
            {
                var albums = ParseAlbums(albumsElement);
                result = result with { Albums = albums, TotalResults = albums.Count };
            }
            else if (filter.DataType == LastfmDataType.Artists &&
                     TryGetResultElement(root, "topartists", "weeklyartistchart", out var artistsElement))
            {
                var artists = ParseArtists(artistsElement);
                result = result with { Artists = artists, TotalResults = artists.Count };
            }

            return result;
        }
        catch (Exception)
        {
            return null;
        }
    }

    private string BuildUrl(string method, Dictionary<string, string> parameters)
    {
        var queryParams = new List<string>
        {
            $"method={Uri.EscapeDataString(method)}",
            $"api_key={Uri.EscapeDataString(_options.ApiKey)}",
            "format=json"
        };

        foreach (var (key, value) in parameters)
        {
            queryParams.Add($"{Uri.EscapeDataString(key)}={Uri.EscapeDataString(value)}");
        }

        return $"{_options.ApiUrl}?{string.Join("&", queryParams)}";
    }

    private void LogRequest(string method, IReadOnlyDictionary<string, string> parameters)
    {
        var paramString = string.Join(", ", parameters.Select(kvp => $"{kvp.Key}={kvp.Value}"));
        _logger.LogInformation("Requesting Last.fm API {Method} with parameters: {Parameters}", method, paramString);
    }

    private static (string From, string To) ConvertCustomDatesToEpoch(LastfmFilter filter)
    {
        if (!DateTimeOffset.TryParse(filter.CustomStartDate, CultureInfo.InvariantCulture, DateTimeStyles.AssumeUniversal, out var start))
        {
            throw new ArgumentException("Custom start date must be a valid ISO 8601 date", nameof(filter));
        }

        if (!DateTimeOffset.TryParse(filter.CustomEndDate, CultureInfo.InvariantCulture, DateTimeStyles.AssumeUniversal, out var end))
        {
            throw new ArgumentException("Custom end date must be a valid ISO 8601 date", nameof(filter));
        }

        var from = start.ToUnixTimeSeconds().ToString(CultureInfo.InvariantCulture);
        var to = end.ToUnixTimeSeconds().ToString(CultureInfo.InvariantCulture);

        return (from, to);
    }

    private static bool TryGetResultElement(JsonElement root, string primaryProperty, string fallbackProperty, out JsonElement element)
    {
        if (root.TryGetProperty(primaryProperty, out element))
        {
            return true;
        }

        if (root.TryGetProperty(fallbackProperty, out element))
        {
            return true;
        }

        element = default;
        return false;
    }

    private static string ConvertTimePeriodToLastfmPeriod(LastfmTimePeriod period)
    {
        return period switch
        {
            LastfmTimePeriod.Last7Days => "7day",
            LastfmTimePeriod.Last1Month => "1month",
            LastfmTimePeriod.Last3Months => "3month",
            LastfmTimePeriod.Last6Months => "6month",
            LastfmTimePeriod.Last12Months => "12month",
            LastfmTimePeriod.Overall => "overall",
            LastfmTimePeriod.Custom => "overall", // Fallback - custom filtering happens client-side
            _ => "overall"
        };
    }

    private static string GetArtistName(JsonElement artistElement)
    {
        if (artistElement.TryGetProperty("name", out var artistName) && artistName.ValueKind is JsonValueKind.String)
        {
            return artistName.GetString() ?? "Unknown";
        }

        if (artistElement.TryGetProperty("#text", out var artistText) && artistText.ValueKind is JsonValueKind.String)
        {
            return artistText.GetString() ?? "Unknown";
        }

        if (artistElement.ValueKind is JsonValueKind.String)
        {
            return artistElement.GetString() ?? "Unknown";
        }

        return "Unknown";
    }

    private static List<LastfmTrack> ParseTracks(JsonElement tracksElement)
    {
        var tracks = new List<LastfmTrack>();

        if (tracksElement.TryGetProperty("track", out var trackArray))
        {
            if (trackArray.ValueKind == JsonValueKind.Array)
            {
                foreach (var track in trackArray.EnumerateArray())
                {
                    var name = track.GetProperty("name").GetString() ?? "Unknown";
                    var artist = track.TryGetProperty("artist", out var artistElement)
                        ? GetArtistName(artistElement)
                        : "Unknown";
                    var playCount = int.TryParse(
                        track.GetProperty("playcount").GetString() ?? "0",
                        out var count) ? count : 0;

                    tracks.Add(new LastfmTrack
                    {
                        Name = name,
                        Artist = artist,
                        PlayCount = playCount
                    });
                }
            }
        }

        return tracks;
    }

    private static List<LastfmAlbum> ParseAlbums(JsonElement albumsElement)
    {
        var albums = new List<LastfmAlbum>();

        if (albumsElement.TryGetProperty("album", out var albumArray))
        {
            if (albumArray.ValueKind == JsonValueKind.Array)
            {
                foreach (var album in albumArray.EnumerateArray())
                {
                    var name = album.GetProperty("name").GetString() ?? "Unknown";
                    var artist = album.TryGetProperty("artist", out var artistElement)
                        ? GetArtistName(artistElement)
                        : "Unknown";
                    var playCount = int.TryParse(
                        album.GetProperty("playcount").GetString() ?? "0",
                        out var count) ? count : 0;

                    albums.Add(new LastfmAlbum
                    {
                        Name = name,
                        Artist = artist,
                        PlayCount = playCount
                    });
                }
            }
        }

        return albums;
    }

    private static List<LastfmArtist> ParseArtists(JsonElement artistsElement)
    {
        var artists = new List<LastfmArtist>();

        if (artistsElement.TryGetProperty("artist", out var artistArray))
        {
            if (artistArray.ValueKind == JsonValueKind.Array)
            {
                foreach (var artist in artistArray.EnumerateArray())
                {
                    var name = artist.GetProperty("name").GetString() ?? "Unknown";
                    var playCount = int.TryParse(
                        artist.GetProperty("playcount").GetString() ?? "0",
                        out var count) ? count : 0;

                    artists.Add(new LastfmArtist
                    {
                        Name = name,
                        PlayCount = playCount
                    });
                }
            }
        }

        return artists;
    }

    public async Task<NormalizedDataResponse?> GetUserDataNormalizedAsync(string username, LastfmFilter filter, CancellationToken cancellationToken = default)
    {
        // Fetch the source data
        var sourceData = await GetUserDataAsync(username, filter, cancellationToken);
        if (sourceData == null)
        {
            return null;
        }

        // Convert to normalized format, preserving all source metadata
        var response = new NormalizedDataResponse
        {
            DataType = filter.DataType.ToString(),
            Source = "lastfm",
            TotalResults = sourceData.TotalResults,
            Tracks = [],
            Albums = [],
            Artists = []
        };

        if (filter.DataType == LastfmDataType.Tracks)
        {
            response = response with
            {
                Tracks = sourceData.Tracks.Select(t => new NormalizedTrack
                {
                    Name = t.Name,
                    Artist = t.Artist,
                    Album = t.Album,
                    Source = "lastfm",
                    SourceMetadata = new Dictionary<string, object?> { { "playCount", t.PlayCount } }
                }).ToList()
            };
        }
        else if (filter.DataType == LastfmDataType.Albums)
        {
            response = response with
            {
                Albums = sourceData.Albums.Select(a => new NormalizedAlbum
                {
                    Name = a.Name,
                    Artist = a.Artist,
                    Tracks = [],
                    Source = "lastfm",
                    SourceMetadata = new Dictionary<string, object?> { { "playCount", a.PlayCount } }
                }).ToList()
            };
        }
        else if (filter.DataType == LastfmDataType.Artists)
        {
            response = response with
            {
                Artists = sourceData.Artists.Select(a => new NormalizedArtist
                {
                    Name = a.Name,
                    Source = "lastfm",
                    SourceMetadata = new Dictionary<string, object?> { { "playCount", a.PlayCount } }
                }).ToList()
            };
        }

        return response;
    }
}

/// <summary>
/// Last.fm user information.
/// </summary>
public sealed record LastfmUser
{
    public required string Username { get; init; }
    public required int PlayCount { get; init; }
    public required string Registered { get; init; }
    public required string ProfileUrl { get; init; }
}
