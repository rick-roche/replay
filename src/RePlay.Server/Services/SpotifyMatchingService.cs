using System.Globalization;
using System.Net.Http.Headers;
using System.Text;
using System.Text.Json;
using System.Text.Json.Serialization;
using System.Text.RegularExpressions;
using RePlay.Server.Models;

namespace RePlay.Server.Services;

/// <summary>
/// Service for matching normalized tracks to Spotify using the Search API.
/// </summary>
public sealed partial class SpotifyMatchingService : ISpotifyMatchingService
{
    private const string SearchEndpoint = "https://api.spotify.com/v1/search";
    private const int ExactMatchConfidence = 100;
    private const int NormalizedMatchConfidence = 90;
    private const int AlbumMatchConfidence = 85;
    private const int FuzzyMatchConfidence = 70;
    private const double FuzzyMatchThreshold = 0.80; // 80% similarity

    private readonly HttpClient _httpClient;

    public SpotifyMatchingService(HttpClient httpClient)
    {
        _httpClient = httpClient;
    }

    public async Task<MatchedDataResponse> MatchTracksAsync(
        IReadOnlyList<NormalizedTrack> tracks,
        string accessToken,
        CancellationToken cancellationToken = default)
    {
        var matchedTracks = new List<MatchedTrack>();

        foreach (var track in tracks)
        {
            var match = await MatchSingleTrackAsync(track, accessToken, cancellationToken);
            matchedTracks.Add(new MatchedTrack
            {
                SourceTrack = track,
                Match = match
            });
        }

        return new MatchedDataResponse
        {
            Tracks = matchedTracks
        };
    }

    public async Task<IReadOnlyList<SpotifyTrack>> SearchTracksAsync(
        string query,
        string accessToken,
        CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(query))
        {
            return Array.Empty<SpotifyTrack>();
        }

        var searchResults = await SearchSpotifyAsync(query, accessToken, cancellationToken);
        
        return searchResults
            .Take(10) // Spotify Development Mode search limit
            .Select(track => new SpotifyTrack
            {
                Id = track.Id,
                Name = track.Name,
                Artist = string.Join(", ", track.Artists.Select(a => a.Name)),
                Album = track.Album?.Name,
                Uri = track.Uri
            })
            .ToList();
    }

    public async Task<MatchedAlbumsResponse> MatchAlbumsAsync(
        IReadOnlyList<NormalizedAlbum> albums,
        string accessToken,
        CancellationToken cancellationToken = default)
    {
        var matchedAlbums = new List<MatchedAlbum>();

        foreach (var album in albums)
        {
            var match = await MatchSingleAlbumAsync(album, accessToken, cancellationToken);
            matchedAlbums.Add(new MatchedAlbum
            {
                SourceAlbum = album,
                Match = match
            });
        }

        return new MatchedAlbumsResponse
        {
            Albums = matchedAlbums
        };
    }

    public async Task<MatchedArtistsResponse> MatchArtistsAsync(
        IReadOnlyList<NormalizedArtist> artists,
        string accessToken,
        CancellationToken cancellationToken = default)
    {
        var matchedArtists = new List<MatchedArtist>();

        foreach (var artist in artists)
        {
            var match = await MatchSingleArtistAsync(artist, accessToken, cancellationToken);
            matchedArtists.Add(new MatchedArtist
            {
                SourceArtist = artist,
                Match = match
            });
        }

        return new MatchedArtistsResponse
        {
            Artists = matchedArtists
        };
    }

    public async Task<IReadOnlyList<SpotifyAlbumInfo>> SearchAlbumsAsync(
        string query,
        string accessToken,
        CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(query))
        {
            return Array.Empty<SpotifyAlbumInfo>();
        }

        var searchResults = await SearchSpotifyAlbumsAsync(query, accessToken, cancellationToken);
        
        return searchResults
            .Take(5) // Limit to top 5 results
            .Select(album => new SpotifyAlbumInfo
            {
                Id = album.Id,
                Name = album.Name,
                Artist = string.Join(", ", album.Artists.Select(a => a.Name)),
                ReleaseDate = album.ReleaseDate,
                Uri = album.Uri,
                TotalTracks = album.TotalTracks
            })
            .ToList();
    }

    public async Task<IReadOnlyList<SpotifyArtistInfo>> SearchArtistsAsync(
        string query,
        string accessToken,
        CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(query))
        {
            return Array.Empty<SpotifyArtistInfo>();
        }

        var searchResults = await SearchSpotifyArtistsAsync(query, accessToken, cancellationToken);
        
        return searchResults
            .Take(5) // Limit to top 5 results
            .Select(artist => new SpotifyArtistInfo
            {
                Id = artist.Id,
                Name = artist.Name,
                Uri = artist.Uri,
                Genres = artist.Genres?.ToList() ?? []
            })
            .ToList();
    }

    public async Task<PlaylistCreationResponse> CreatePlaylistAsync(
        PlaylistCreationRequest request,
        string accessToken,
        string userId,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(request);
        ArgumentException.ThrowIfNullOrWhiteSpace(accessToken);
        ArgumentException.ThrowIfNullOrWhiteSpace(userId);

        if (request.TrackUris.Count == 0)
        {
            throw new InvalidOperationException("At least one track URI is required to create a playlist.");
        }

        // Step 1: Create the playlist
        var createPlaylistUrl = "https://api.spotify.com/v1/me/playlists";
        var createPlaylistPayload = new
        {
            name = request.Name,
            description = request.Description,
            @public = request.IsPublic
        };

        using var createRequest = new HttpRequestMessage(HttpMethod.Post, createPlaylistUrl)
        {
            Content = new StringContent(
                JsonSerializer.Serialize(createPlaylistPayload),
                Encoding.UTF8,
                "application/json")
        };
        createRequest.Headers.Authorization = new AuthenticationHeaderValue("Bearer", accessToken);

        const string unconfirmedCode = "PLAYLIST_CREATION_UNCONFIRMED";
        const string unconfirmedMessage = "Spotify may have created the playlist. Check your Spotify library before trying again.";
        string? playlistId;
        string? playlistUri;
        string? playlistUrl;

        // Before sending, cancellation is known not to have caused a write. After sending,
        // an interrupted or unreadable response cannot prove that Spotify did not commit it.
        cancellationToken.ThrowIfCancellationRequested();
        try
        {
            using var createResponse = await _httpClient.SendAsync(createRequest, cancellationToken);
            if ((int)createResponse.StatusCode >= 500)
            {
                throw new SpotifyApiException(System.Net.HttpStatusCode.BadGateway, unconfirmedCode, unconfirmedMessage);
            }
            if (!createResponse.IsSuccessStatusCode)
            {
                throw new SpotifyApiException(createResponse.StatusCode);
            }

            var createResponseBody = await createResponse.Content.ReadAsStringAsync(cancellationToken);
            var playlistJson = JsonSerializer.Deserialize<JsonElement>(createResponseBody);
            playlistId = playlistJson.GetProperty("id").GetString();
            playlistUri = playlistJson.GetProperty("uri").GetString();
            playlistUrl = playlistJson.GetProperty("external_urls").GetProperty("spotify").GetString();
            if (string.IsNullOrWhiteSpace(playlistId))
            {
                throw new SpotifyApiException(System.Net.HttpStatusCode.BadGateway, unconfirmedCode, unconfirmedMessage);
            }
        }
        catch (Exception ex) when (ex is not SpotifyApiException && ex is
            HttpRequestException or OperationCanceledException or Polly.ExecutionRejectedException or
            JsonException or KeyNotFoundException or InvalidOperationException)
        {
            throw new SpotifyApiException(System.Net.HttpStatusCode.BadGateway, unconfirmedCode, unconfirmedMessage);
        }

        // Step 2: Add tracks to the playlist (Spotify allows up to 100 tracks per request)
        var addTracksUrl = $"https://api.spotify.com/v1/playlists/{System.Uri.EscapeDataString(playlistId)}/items";
        var tracksAdded = 0;
        var result = new PlaylistCreationResponse
        {
            PlaylistId = playlistId,
            Uri = playlistUri ?? $"spotify:playlist:{playlistId}",
            Url = playlistUrl ?? $"https://open.spotify.com/playlist/{playlistId}",
            TracksAdded = 0
        };

        try
        {
            for (int i = 0; i < request.TrackUris.Count; i += 100)
            {
                var batch = request.TrackUris.Skip(i).Take(100).ToList();
                using var addTracksRequest = new HttpRequestMessage(HttpMethod.Post, addTracksUrl)
                {
                    Content = new StringContent(
                        JsonSerializer.Serialize(new { uris = batch }),
                        Encoding.UTF8,
                        "application/json")
                };
                addTracksRequest.Headers.Authorization = new AuthenticationHeaderValue("Bearer", accessToken);

                using var addTracksResponse = await _httpClient.SendAsync(addTracksRequest, cancellationToken);
                if (!addTracksResponse.IsSuccessStatusCode)
                {
                    throw new SpotifyApiException(addTracksResponse.StatusCode);
                }
                tracksAdded += batch.Count;
            }
        }
        catch (Exception ex) when (ex is HttpRequestException or OperationCanceledException or Polly.ExecutionRejectedException)
        {
            // A timed-out write may have applied: expose confirmed progress, never retry blindly.
            return result with
            {
                TracksAdded = tracksAdded,
                IsComplete = false,
                ErrorCode = "PLAYLIST_PARTIALLY_CREATED",
                ErrorMessage = "The playlist was created, but not all track additions were confirmed. Open it in Spotify before retrying."
            };
        }

        return result with { TracksAdded = tracksAdded };
    }

    private async Task<SpotifyMatch?> MatchSingleTrackAsync(
        NormalizedTrack track,
        string accessToken,
        CancellationToken cancellationToken)
    {
        // Build search query: track + artist
        var query = $"track:{track.Name} artist:{track.Artist}";
        var searchResults = await SearchSpotifyAsync(query, accessToken, cancellationToken);

        if (searchResults.Count == 0)
        {
            return null; // No matches found
        }

        // Try matching strategies in order of confidence
        // If album context is available, try album-based matching before normalized
        if (!string.IsNullOrWhiteSpace(track.Album))
        {
            return TryExactMatch(track, searchResults)
                ?? TryAlbumBasedMatch(track, searchResults)
                ?? TryNormalizedMatch(track, searchResults)
                ?? TryFuzzyMatch(track, searchResults);
        }

        return TryExactMatch(track, searchResults)
            ?? TryNormalizedMatch(track, searchResults)
            ?? TryFuzzyMatch(track, searchResults);
    }

    private SpotifyMatch? TryExactMatch(
        NormalizedTrack track,
        IReadOnlyList<SpotifySearchTrack> searchResults)
    {
        var result = searchResults.FirstOrDefault(sr =>
            sr.Name.Equals(track.Name, StringComparison.Ordinal) &&
            sr.Artists.Any(a => a.Name.Equals(track.Artist, StringComparison.Ordinal)));

        return result is not null
            ? CreateMatch(result, ExactMatchConfidence, MatchMethod.Exact)
            : null;
    }

    private SpotifyMatch? TryNormalizedMatch(
        NormalizedTrack track,
        IReadOnlyList<SpotifySearchTrack> searchResults)
    {
        var normalizedTrackName = Normalize(track.Name);
        var normalizedArtistName = Normalize(track.Artist);

        var result = searchResults.FirstOrDefault(sr =>
            Normalize(sr.Name) == normalizedTrackName &&
            sr.Artists.Any(a => Normalize(a.Name) == normalizedArtistName));

        return result is not null
            ? CreateMatch(result, NormalizedMatchConfidence, MatchMethod.Normalized)
            : null;
    }

    private SpotifyMatch? TryAlbumBasedMatch(
        NormalizedTrack track,
        IReadOnlyList<SpotifySearchTrack> searchResults)
    {
        if (string.IsNullOrWhiteSpace(track.Album))
        {
            return null; // No album context available
        }

        var normalizedTrackName = Normalize(track.Name);
        var normalizedArtistName = Normalize(track.Artist);
        var normalizedAlbumName = Normalize(track.Album);

        var result = searchResults.FirstOrDefault(sr =>
            Normalize(sr.Name) == normalizedTrackName &&
            sr.Artists.Any(a => Normalize(a.Name) == normalizedArtistName) &&
            Normalize(sr.Album.Name) == normalizedAlbumName);

        return result is not null
            ? CreateMatch(result, AlbumMatchConfidence, MatchMethod.AlbumBased)
            : null;
    }

    private SpotifyMatch? TryFuzzyMatch(
        NormalizedTrack track,
        IReadOnlyList<SpotifySearchTrack> searchResults)
    {
        var normalizedTrackName = Normalize(track.Name);
        var normalizedArtistName = Normalize(track.Artist);

        foreach (var result in searchResults)
        {
            var trackSimilarity = CalculateSimilarity(normalizedTrackName, Normalize(result.Name));
            var artistSimilarity = result.Artists
                .Select(a => CalculateSimilarity(normalizedArtistName, Normalize(a.Name)))
                .Max();

            // Both track and artist must meet threshold
            if (trackSimilarity >= FuzzyMatchThreshold && artistSimilarity >= FuzzyMatchThreshold)
            {
                return CreateMatch(result, FuzzyMatchConfidence, MatchMethod.Fuzzy);
            }
        }

        return null;
    }

    private async Task<IReadOnlyList<SpotifySearchTrack>> SearchSpotifyAsync(
        string query,
        string accessToken,
        CancellationToken cancellationToken)
    {
        var encodedQuery = Uri.EscapeDataString(query);
        var url = $"{SearchEndpoint}?q={encodedQuery}&type=track&limit=10";

        using var request = new HttpRequestMessage(HttpMethod.Get, url);
        request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", accessToken);
        
        using var response = await _httpClient.SendAsync(request, cancellationToken);

        if (!response.IsSuccessStatusCode)
        {
            throw new SpotifyApiException(response.StatusCode);
        }

        var json = await response.Content.ReadAsStringAsync(cancellationToken);
        var searchResponse = JsonSerializer.Deserialize<SpotifySearchResponse>(json);

        return searchResponse?.Tracks?.Items ?? Array.Empty<SpotifySearchTrack>();
    }

    private async Task<SpotifyAlbumMatch?> MatchSingleAlbumAsync(
        NormalizedAlbum album,
        string accessToken,
        CancellationToken cancellationToken)
    {
        // Build search query: album + artist
        var query = $"album:{album.Name} artist:{album.Artist}";
        var searchResults = await SearchSpotifyAlbumsAsync(query, accessToken, cancellationToken);

        if (searchResults.Count == 0)
        {
            return null; // No matches found
        }

        // Try matching strategies in order of confidence
        var matchedSpotifyAlbum = TryExactAlbumMatch(album, searchResults)
            ?? TryNormalizedAlbumMatch(album, searchResults)
            ?? TryFuzzyAlbumMatch(album, searchResults);

        if (matchedSpotifyAlbum == null)
        {
            return null;
        }

        // Fetch tracks for matched album
        var tracks = await FetchAlbumTracksAsync(matchedSpotifyAlbum.Id, accessToken, cancellationToken);

        return new SpotifyAlbumMatch
        {
            SpotifyId = matchedSpotifyAlbum.Id,
            Name = matchedSpotifyAlbum.Name,
            Artist = string.Join(", ", matchedSpotifyAlbum.Artists.Select(a => a.Name)),
            Uri = matchedSpotifyAlbum.Uri,
            Tracks = tracks,
            Confidence = matchedSpotifyAlbum.Confidence,
            Method = matchedSpotifyAlbum.Method
        };
    }

    private async Task<SpotifyArtistMatch?> MatchSingleArtistAsync(
        NormalizedArtist artist,
        string accessToken,
        CancellationToken cancellationToken)
    {
        // Build search query: artist name
        var query = $"artist:{artist.Name}";
        var searchResults = await SearchSpotifyArtistsAsync(query, accessToken, cancellationToken);

        if (searchResults.Count == 0)
        {
            return null; // No matches found
        }

        // Try matching strategies in order of confidence
        var matchedSpotifyArtist = TryExactArtistMatch(artist, searchResults)
            ?? TryNormalizedArtistMatch(artist, searchResults)
            ?? TryFuzzyArtistMatch(artist, searchResults);

        if (matchedSpotifyArtist == null)
        {
            return null;
        }

        // Fetch top tracks for matched artist
        var topTracks = await FetchArtistTopTracksAsync(matchedSpotifyArtist.Id, accessToken, cancellationToken);

        return new SpotifyArtistMatch
        {
            SpotifyId = matchedSpotifyArtist.Id,
            Name = matchedSpotifyArtist.Name,
            Uri = matchedSpotifyArtist.Uri,
            TopTracks = topTracks,
            Confidence = matchedSpotifyArtist.Confidence,
            Method = matchedSpotifyArtist.Method
        };
    }

    private SpotifyAlbumData? TryExactAlbumMatch(
        NormalizedAlbum album,
        IReadOnlyList<SpotifyAlbumData> searchResults)
    {
        var result = searchResults.FirstOrDefault(sr =>
            sr.Name.Equals(album.Name, StringComparison.Ordinal) &&
            sr.Artists.Any(a => a.Name.Equals(album.Artist, StringComparison.Ordinal)));

        if (result != null)
        {
            return result with { Confidence = ExactMatchConfidence, Method = MatchMethod.Exact };
        }
        return null;
    }

    private SpotifyAlbumData? TryNormalizedAlbumMatch(
        NormalizedAlbum album,
        IReadOnlyList<SpotifyAlbumData> searchResults)
    {
        var normalizedAlbumName = Normalize(album.Name);
        var normalizedArtistName = Normalize(album.Artist);

        var result = searchResults.FirstOrDefault(sr =>
            Normalize(sr.Name) == normalizedAlbumName &&
            sr.Artists.Any(a => Normalize(a.Name) == normalizedArtistName));

        if (result != null)
        {
            return result with { Confidence = NormalizedMatchConfidence, Method = MatchMethod.Normalized };
        }
        return null;
    }

    private SpotifyAlbumData? TryFuzzyAlbumMatch(
        NormalizedAlbum album,
        IReadOnlyList<SpotifyAlbumData> searchResults)
    {
        var normalizedAlbumName = Normalize(album.Name);
        var normalizedArtistName = Normalize(album.Artist);

        foreach (var result in searchResults)
        {
            var albumSimilarity = CalculateSimilarity(normalizedAlbumName, Normalize(result.Name));
            var artistSimilarity = result.Artists
                .Select(a => CalculateSimilarity(normalizedArtistName, Normalize(a.Name)))
                .Max();

            // Both album and artist must meet threshold
            if (albumSimilarity >= FuzzyMatchThreshold && artistSimilarity >= FuzzyMatchThreshold)
            {
                return result with { Confidence = FuzzyMatchConfidence, Method = MatchMethod.Fuzzy };
            }
        }

        return null;
    }

    private SpotifyArtistData? TryExactArtistMatch(
        NormalizedArtist artist,
        IReadOnlyList<SpotifyArtistData> searchResults)
    {
        var result = searchResults.FirstOrDefault(sr =>
            sr.Name.Equals(artist.Name, StringComparison.Ordinal));

        if (result != null)
        {
            return result with { Confidence = ExactMatchConfidence, Method = MatchMethod.Exact };
        }
        return null;
    }

    private SpotifyArtistData? TryNormalizedArtistMatch(
        NormalizedArtist artist,
        IReadOnlyList<SpotifyArtistData> searchResults)
    {
        var normalizedArtistName = Normalize(artist.Name);

        var result = searchResults.FirstOrDefault(sr =>
            Normalize(sr.Name) == normalizedArtistName);

        if (result != null)
        {
            return result with { Confidence = NormalizedMatchConfidence, Method = MatchMethod.Normalized };
        }
        return null;
    }

    private SpotifyArtistData? TryFuzzyArtistMatch(
        NormalizedArtist artist,
        IReadOnlyList<SpotifyArtistData> searchResults)
    {
        var normalizedArtistName = Normalize(artist.Name);

        foreach (var result in searchResults)
        {
            var artistSimilarity = CalculateSimilarity(normalizedArtistName, Normalize(result.Name));

            // Artist must meet threshold
            if (artistSimilarity >= FuzzyMatchThreshold)
            {
                return result with { Confidence = FuzzyMatchConfidence, Method = MatchMethod.Fuzzy };
            }
        }

        return null;
    }

    private async Task<List<SpotifyMatch>> FetchAlbumTracksAsync(
        string albumId,
        string accessToken,
        CancellationToken cancellationToken)
    {
        var tracks = new List<SpotifyMatch>();
        const int pageSize = 50;
        const int maxPages = 100;
        for (var page = 0; page < maxPages; page++)
        {
            // Ignore provider-supplied next URLs: bearer tokens must only reach Spotify.
            var url = $"https://api.spotify.com/v1/albums/{Uri.EscapeDataString(albumId)}/tracks?limit={pageSize}&offset={page * pageSize}";
            using var request = new HttpRequestMessage(HttpMethod.Get, url);
            request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", accessToken);
            using var response = await _httpClient.SendAsync(request, cancellationToken);
            if (!response.IsSuccessStatusCode)
            {
                throw new SpotifyApiException(response.StatusCode);
            }
            var json = await response.Content.ReadAsStringAsync(cancellationToken);
            var tracksResponse = JsonSerializer.Deserialize<SpotifyAlbumTracksResponse>(json);
            var items = tracksResponse?.Items ?? [];
            foreach (var track in items)
            {
                tracks.Add(new SpotifyMatch
                {
                    SpotifyId = track.Id,
                    Name = track.Name,
                    Artist = string.Join(", ", track.Artists.Select(a => a.Name)),
                    Album = track.Album?.Name,
                    Uri = track.Uri,
                    Confidence = 100,
                    Method = MatchMethod.Exact
                });
            }
            if (items.Length < pageSize)
            {
                return tracks;
            }
        }

        throw new SpotifyApiException(System.Net.HttpStatusCode.BadGateway, "SPOTIFY_PAGINATION_LIMIT", "Spotify album exceeded the supported pagination limit.");
    }

    private async Task<List<SpotifyMatch>> FetchArtistTopTracksAsync(
        string artistId,
        string accessToken,
        CancellationToken cancellationToken)
    {
        var url = $"https://api.spotify.com/v1/artists/{artistId}/top-tracks?market=US";

        using var request = new HttpRequestMessage(HttpMethod.Get, url);
        request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", accessToken);
        
        using var response = await _httpClient.SendAsync(request, cancellationToken);

        if (!response.IsSuccessStatusCode)
        {
            if (response.StatusCode is System.Net.HttpStatusCode.Forbidden or System.Net.HttpStatusCode.NotFound)
            {
                throw new SpotifyApiException(response.StatusCode, "SPOTIFY_TOP_TRACKS_UNSUPPORTED", "Artist top tracks are not supported by this Spotify application's access mode.");
            }
            throw new SpotifyApiException(response.StatusCode);
        }

        var json = await response.Content.ReadAsStringAsync(cancellationToken);
        var topTracksResponse = JsonSerializer.Deserialize<SpotifyArtistTopTracksResponse>(json);

        var tracks = new List<SpotifyMatch>();
        if (topTracksResponse?.Tracks != null)
        {
            foreach (var track in topTracksResponse.Tracks)
            {
                tracks.Add(new SpotifyMatch
                {
                    SpotifyId = track.Id,
                    Name = track.Name,
                    Artist = string.Join(", ", track.Artists.Select(a => a.Name)),
                    Album = track.Album?.Name,
                    Uri = track.Uri,
                    Confidence = 100,
                    Method = MatchMethod.Exact
                });
            }
        }

        return tracks;
    }

    private async Task<IReadOnlyList<SpotifyAlbumData>> SearchSpotifyAlbumsAsync(
        string query,
        string accessToken,
        CancellationToken cancellationToken)
    {
        var encodedQuery = Uri.EscapeDataString(query);
        var url = $"{SearchEndpoint}?q={encodedQuery}&type=album&limit=10";

        using var request = new HttpRequestMessage(HttpMethod.Get, url);
        request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", accessToken);
        
        using var response = await _httpClient.SendAsync(request, cancellationToken);

        if (!response.IsSuccessStatusCode)
        {
            throw new SpotifyApiException(response.StatusCode);
        }

        var json = await response.Content.ReadAsStringAsync(cancellationToken);
        var searchResponse = JsonSerializer.Deserialize<SpotifyAlbumSearchResponse>(json);

        return searchResponse?.Albums?.Items ?? Array.Empty<SpotifyAlbumData>();
    }

    private async Task<IReadOnlyList<SpotifyArtistData>> SearchSpotifyArtistsAsync(
        string query,
        string accessToken,
        CancellationToken cancellationToken)
    {
        var encodedQuery = Uri.EscapeDataString(query);
        var url = $"{SearchEndpoint}?q={encodedQuery}&type=artist&limit=10";

        using var request = new HttpRequestMessage(HttpMethod.Get, url);
        request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", accessToken);
        
        using var response = await _httpClient.SendAsync(request, cancellationToken);

        if (!response.IsSuccessStatusCode)
        {
            throw new SpotifyApiException(response.StatusCode);
        }

        var json = await response.Content.ReadAsStringAsync(cancellationToken);
        var searchResponse = JsonSerializer.Deserialize<SpotifyArtistSearchResponse>(json);

        return searchResponse?.Artists?.Items ?? Array.Empty<SpotifyArtistData>();
    }

    private static SpotifyMatch CreateMatch(
        SpotifySearchTrack track,
        int confidence,
        MatchMethod method)
    {
        return new SpotifyMatch
        {
            SpotifyId = track.Id,
            Name = track.Name,
            Artist = string.Join(", ", track.Artists.Select(a => a.Name)),
            Album = track.Album.Name,
            Uri = track.Uri,
            Confidence = confidence,
            Method = method
        };
    }

    /// <summary>
    /// Normalizes a string for matching: lowercase, remove punctuation, remove diacritics.
    /// </summary>
    private static string Normalize(string input)
    {
        if (string.IsNullOrWhiteSpace(input))
        {
            return string.Empty;
        }

        // Remove diacritics
        var normalized = input.Normalize(NormalizationForm.FormD);
        var stringBuilder = new StringBuilder();

        foreach (var c in normalized)
        {
            var category = CharUnicodeInfo.GetUnicodeCategory(c);
            if (category != UnicodeCategory.NonSpacingMark)
            {
                stringBuilder.Append(c);
            }
        }

        var result = stringBuilder.ToString().Normalize(NormalizationForm.FormC);

        // Remove punctuation and convert to lowercase
        result = PunctuationRegex().Replace(result, " ");
        result = WhitespaceRegex().Replace(result, " ").Trim().ToLowerInvariant();

        return result;
    }

    /// <summary>
    /// Calculates Levenshtein distance-based similarity between two strings.
    /// Returns a value between 0 (completely different) and 1 (identical).
    /// </summary>
    private static double CalculateSimilarity(string s1, string s2)
    {
        if (s1 == s2) return 1.0;
        if (string.IsNullOrEmpty(s1) || string.IsNullOrEmpty(s2)) return 0.0;

        var distance = LevenshteinDistance(s1, s2);
        var maxLength = Math.Max(s1.Length, s2.Length);

        return 1.0 - (double)distance / maxLength;
    }

    /// <summary>
    /// Computes the Levenshtein distance between two strings.
    /// </summary>
    private static int LevenshteinDistance(string s1, string s2)
    {
        var d = new int[s1.Length + 1, s2.Length + 1];

        for (var i = 0; i <= s1.Length; i++)
        {
            d[i, 0] = i;
        }

        for (var j = 0; j <= s2.Length; j++)
        {
            d[0, j] = j;
        }

        for (var j = 1; j <= s2.Length; j++)
        {
            for (var i = 1; i <= s1.Length; i++)
            {
                var cost = s1[i - 1] == s2[j - 1] ? 0 : 1;
                d[i, j] = Math.Min(
                    Math.Min(d[i - 1, j] + 1, d[i, j - 1] + 1),
                    d[i - 1, j - 1] + cost);
            }
        }

        return d[s1.Length, s2.Length];
    }

    [GeneratedRegex(@"[^\w\s]")]
    private static partial Regex PunctuationRegex();

    [GeneratedRegex(@"\s+")]
    private static partial Regex WhitespaceRegex();

    // DTOs for Spotify Search API
    private sealed record SpotifySearchResponse(
        [property: JsonPropertyName("tracks")] SpotifyTracksResponse? Tracks
    );

    private sealed record SpotifyTracksResponse(
        [property: JsonPropertyName("items")] SpotifySearchTrack[] Items
    );

    private sealed record SpotifySearchTrack(
        [property: JsonPropertyName("id")] string Id,
        [property: JsonPropertyName("name")] string Name,
        [property: JsonPropertyName("uri")] string Uri,
        [property: JsonPropertyName("artists")] SpotifyArtist[] Artists,
        [property: JsonPropertyName("album")] SpotifyAlbum Album
    );

    private sealed record SpotifyArtist(
        [property: JsonPropertyName("name")] string Name
    );

    private sealed record SpotifyAlbum(
        [property: JsonPropertyName("name")] string Name
    );

    // DTOs for Album Search
    private sealed record SpotifyAlbumSearchResponse(
        [property: JsonPropertyName("albums")] SpotifyAlbumsResponse? Albums
    );

    private sealed record SpotifyAlbumsResponse(
        [property: JsonPropertyName("items")] SpotifyAlbumData[] Items
    );

    private sealed record SpotifyAlbumData(
        [property: JsonPropertyName("id")] string Id,
        [property: JsonPropertyName("name")] string Name,
        [property: JsonPropertyName("uri")] string Uri,
        [property: JsonPropertyName("release_date")] string? ReleaseDate,
        [property: JsonPropertyName("total_tracks")] int TotalTracks,
        [property: JsonPropertyName("artists")] SpotifyArtistDto[] Artists,
        int Confidence = 0,
        MatchMethod Method = MatchMethod.Exact
    );

    private sealed record SpotifyAlbumTracksResponse(
        [property: JsonPropertyName("items")] SpotifyTrackData[] Items
    );

    private sealed record SpotifyTrackData(
        [property: JsonPropertyName("id")] string Id,
        [property: JsonPropertyName("name")] string Name,
        [property: JsonPropertyName("uri")] string Uri,
        [property: JsonPropertyName("artists")] SpotifyArtistDto[] Artists,
        [property: JsonPropertyName("album")] SpotifyAlbumDto? Album = null
    );

    private sealed record SpotifyArtistDto(
        [property: JsonPropertyName("name")] string Name,
        [property: JsonPropertyName("id")] string? Id = null,
        [property: JsonPropertyName("uri")] string? Uri = null,
        [property: JsonPropertyName("genres")] string[]? Genres = null
    );

    // DTOs for Artist Search
    private sealed record SpotifyArtistSearchResponse(
        [property: JsonPropertyName("artists")] SpotifyArtistsResponse? Artists
    );

    private sealed record SpotifyArtistsResponse(
        [property: JsonPropertyName("items")] SpotifyArtistData[] Items
    );

    private sealed record SpotifyArtistData(
        [property: JsonPropertyName("id")] string Id,
        [property: JsonPropertyName("name")] string Name,
        [property: JsonPropertyName("uri")] string Uri,
        [property: JsonPropertyName("genres")] string[]? Genres = null,
        int Confidence = 0,
        MatchMethod Method = MatchMethod.Exact
    );

    private sealed record SpotifyArtistTopTracksResponse(
        [property: JsonPropertyName("tracks")] SpotifyTrackData[] Tracks
    );

    private sealed record SpotifyAlbumDto(
        [property: JsonPropertyName("name")] string Name
    );
}
