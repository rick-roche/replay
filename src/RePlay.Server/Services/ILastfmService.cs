using System.Net.Http.Json;
using System.Text.Json;
using System.Text.Json.Serialization;
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
}

/// <summary>
/// Implementation of Last.fm API service.
/// </summary>
public sealed class LastfmService : ILastfmService
{
    private readonly HttpClient _httpClient;
    private readonly LastfmOptions _options;

    public LastfmService(HttpClient httpClient, IOptions<LastfmOptions> options)
    {
        _httpClient = httpClient;
        _options = options.Value;

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

            var user = new LastfmUser
            {
                Username = userElement.GetProperty("name").GetString() ?? username,
                PlayCount = int.TryParse(playcountStr, out var count) ? count : 0,
                Registered = userElement.GetProperty("registered").GetProperty("unixtime").GetString() ?? ""
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
            var method = filter.DataType switch
            {
                LastfmDataType.Tracks => "user.getTopTracks",
                LastfmDataType.Albums => "user.getTopAlbums",
                LastfmDataType.Artists => "user.getTopArtists",
                _ => throw new ArgumentException("Invalid data type", nameof(filter))
            };

            var period = ConvertTimePeriodToLastfmPeriod(filter.TimePeriod);
            var limit = Math.Min(filter.MaxResults, 500); // Last.fm API has a max of 500

            var parameters = new Dictionary<string, string>
            {
                { "user", username },
                { "period", period },
                { "limit", limit.ToString() },
                { "extended", "1" }
            };

            var url = BuildUrl(method, parameters);
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

            if (filter.DataType == LastfmDataType.Tracks && root.TryGetProperty("toptracks", out var tracksElement))
            {
                result = result with { Tracks = ParseTracks(tracksElement) };
            }
            else if (filter.DataType == LastfmDataType.Albums && root.TryGetProperty("topalbums", out var albumsElement))
            {
                result = result with { Albums = ParseAlbums(albumsElement) };
            }
            else if (filter.DataType == LastfmDataType.Artists && root.TryGetProperty("topartists", out var artistsElement))
            {
                result = result with { Artists = ParseArtists(artistsElement) };
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
                        ? (artistElement.TryGetProperty("name", out var artistName) 
                            ? artistName.GetString() ?? "Unknown"
                            : artistElement.GetString() ?? "Unknown")
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
                        ? (artistElement.TryGetProperty("name", out var artistName)
                            ? artistName.GetString() ?? "Unknown"
                            : artistElement.GetString() ?? "Unknown")
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
}

/// <summary>
/// Last.fm user information.
/// </summary>
public sealed record LastfmUser
{
    public required string Username { get; init; }
    public required int PlayCount { get; init; }
    public required string Registered { get; init; }
}
