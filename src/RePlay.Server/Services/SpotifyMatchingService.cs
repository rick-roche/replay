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
            return Array.Empty<SpotifySearchTrack>();
        }

        var json = await response.Content.ReadAsStringAsync(cancellationToken);
        var searchResponse = JsonSerializer.Deserialize<SpotifySearchResponse>(json);

        return searchResponse?.Tracks?.Items ?? Array.Empty<SpotifySearchTrack>();
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
}
