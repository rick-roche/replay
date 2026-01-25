using System.Net;
using System.Text.Json;
using RePlay.Server.Models;
using RePlay.Server.Services;

namespace RePlay.Server.Tests.Services;

public class SpotifyMatchingServiceTests : IDisposable
{
    private readonly HttpClient _httpClient;
    private readonly SpotifyMatchingService _service;
    private readonly MockHttpMessageHandler _mockHandler;

    public SpotifyMatchingServiceTests()
    {
        _mockHandler = new MockHttpMessageHandler();
        _httpClient = new HttpClient(_mockHandler);
        _service = new SpotifyMatchingService(_httpClient);
    }

    [Fact]
    public async Task MatchTracksAsync_WithExactMatch_ReturnsHighConfidence()
    {
        // Arrange
        var tracks = new List<NormalizedTrack>
        {
            new()
            {
                Name = "Bohemian Rhapsody",
                Artist = "Queen",
                Album = "A Night at the Opera",
                Source = "lastfm",
                SourceMetadata = new Dictionary<string, object?> { ["playcount"] = 42 }
            }
        };

        var spotifyResponse = new
        {
            tracks = new
            {
                items = new[]
                {
                    new
                    {
                        id = "spotify123",
                        name = "Bohemian Rhapsody",
                        uri = "spotify:track:spotify123",
                        artists = new[] { new { name = "Queen" } },
                        album = new { name = "A Night at the Opera" }
                    }
                }
            }
        };

        _mockHandler.SetResponse(JsonSerializer.Serialize(spotifyResponse));

        // Act
        var result = await _service.MatchTracksAsync(tracks, "test_access_token");

        // Assert
        Assert.Single(result.Tracks);
        Assert.True(result.Tracks[0].IsMatched);
        Assert.NotNull(result.Tracks[0].Match);
        Assert.Equal("spotify123", result.Tracks[0].Match!.SpotifyId);
        Assert.Equal(100, result.Tracks[0].Match!.Confidence); // Exact match
        Assert.Equal(MatchMethod.Exact, result.Tracks[0].Match!.Method);
        Assert.Equal(1, result.MatchedCount);
        Assert.Equal(0, result.UnmatchedCount);
    }

    [Fact]
    public async Task MatchTracksAsync_WithNormalizedMatch_ReturnsGoodConfidence()
    {
        // Arrange
        var tracks = new List<NormalizedTrack>
        {
            new()
            {
                Name = "don't stop believin'",
                Artist = "JOURNEY",
                Album = null,
                Source = "lastfm",
                SourceMetadata = new Dictionary<string, object?>()
            }
        };

        var spotifyResponse = new
        {
            tracks = new
            {
                items = new[]
                {
                    new
                    {
                        id = "spotify456",
                        name = "Don't Stop Believin'",
                        uri = "spotify:track:spotify456",
                        artists = new[] { new { name = "Journey" } },
                        album = new { name = "Escape" }
                    }
                }
            }
        };

        _mockHandler.SetResponse(JsonSerializer.Serialize(spotifyResponse));

        // Act
        var result = await _service.MatchTracksAsync(tracks, "test_access_token");

        // Assert
        Assert.Single(result.Tracks);
        Assert.True(result.Tracks[0].IsMatched);
        Assert.Equal(90, result.Tracks[0].Match!.Confidence); // Normalized match
        Assert.Equal(MatchMethod.Normalized, result.Tracks[0].Match!.Method);
    }

    [Fact]
    public async Task MatchTracksAsync_WithAlbumContext_UsesAlbumBasedMatch()
    {
        // Arrange
        var tracks = new List<NormalizedTrack>
        {
            new()
            {
                Name = "blackbird",
                Artist = "the beatles",
                Album = "the white album",
                Source = "lastfm",
                SourceMetadata = new Dictionary<string, object?>()
            }
        };

        var spotifyResponse = new
        {
            tracks = new
            {
                items = new[]
                {
                    new
                    {
                        id = "spotify789",
                        name = "Blackbird",
                        uri = "spotify:track:spotify789",
                        artists = new[] { new { name = "The Beatles" } },
                        album = new { name = "The White Album" }
                    }
                }
            }
        };

        _mockHandler.SetResponse(JsonSerializer.Serialize(spotifyResponse));

        // Act
        var result = await _service.MatchTracksAsync(tracks, "test_access_token");

        // Assert
        Assert.Single(result.Tracks);
        Assert.True(result.Tracks[0].IsMatched);
        Assert.Equal(85, result.Tracks[0].Match!.Confidence); // Album-based match
        Assert.Equal(MatchMethod.AlbumBased, result.Tracks[0].Match!.Method);
    }

    [Fact]
    public async Task MatchTracksAsync_WithFuzzyMatch_ReturnsFairConfidence()
    {
        // Arrange
        var tracks = new List<NormalizedTrack>
        {
            new()
            {
                Name = "Smells Like Teen Spirit",
                Artist = "Nirvana",
                Album = null,
                Source = "lastfm",
                SourceMetadata = new Dictionary<string, object?>()
            }
        };

        var spotifyResponse = new
        {
            tracks = new
            {
                items = new[]
                {
                    new
                    {
                        id = "spotifyABC",
                        name = "Smells Like Teens Spirit", // Slight typo
                        uri = "spotify:track:spotifyABC",
                        artists = new[] { new { name = "Nirvana" } },
                        album = new { name = "Nevermind" }
                    }
                }
            }
        };

        _mockHandler.SetResponse(JsonSerializer.Serialize(spotifyResponse));

        // Act
        var result = await _service.MatchTracksAsync(tracks, "test_access_token");

        // Assert
        Assert.Single(result.Tracks);
        Assert.True(result.Tracks[0].IsMatched);
        Assert.Equal(70, result.Tracks[0].Match!.Confidence); // Fuzzy match
        Assert.Equal(MatchMethod.Fuzzy, result.Tracks[0].Match!.Method);
    }

    [Fact]
    public async Task MatchTracksAsync_WithNoMatches_ReturnsUnmatched()
    {
        // Arrange
        var tracks = new List<NormalizedTrack>
        {
            new()
            {
                Name = "Unknown Track",
                Artist = "Unknown Artist",
                Album = null,
                Source = "lastfm",
                SourceMetadata = new Dictionary<string, object?>()
            }
        };

        var spotifyResponse = new
        {
            tracks = new
            {
                items = Array.Empty<object>()
            }
        };

        _mockHandler.SetResponse(JsonSerializer.Serialize(spotifyResponse));

        // Act
        var result = await _service.MatchTracksAsync(tracks, "test_access_token");

        // Assert
        Assert.Single(result.Tracks);
        Assert.False(result.Tracks[0].IsMatched);
        Assert.Null(result.Tracks[0].Match);
        Assert.Equal(0, result.MatchedCount);
        Assert.Equal(1, result.UnmatchedCount);
    }

    [Fact]
    public async Task MatchTracksAsync_WithMultipleTracks_ProcessesAll()
    {
        // Arrange
        var tracks = new List<NormalizedTrack>
        {
            new()
            {
                Name = "Track 1",
                Artist = "Artist 1",
                Album = null,
                Source = "lastfm",
                SourceMetadata = new Dictionary<string, object?>()
            },
            new()
            {
                Name = "Track 2",
                Artist = "Artist 2",
                Album = null,
                Source = "lastfm",
                SourceMetadata = new Dictionary<string, object?>()
            }
        };

        var spotifyResponse = new
        {
            tracks = new
            {
                items = new[]
                {
                    new
                    {
                        id = "id1",
                        name = "Track 1",
                        uri = "spotify:track:id1",
                        artists = new[] { new { name = "Artist 1" } },
                        album = new { name = "Album 1" }
                    }
                }
            }
        };

        _mockHandler.SetResponse(JsonSerializer.Serialize(spotifyResponse));

        // Act
        var result = await _service.MatchTracksAsync(tracks, "test_access_token");

        // Assert
        Assert.Equal(2, result.Tracks.Count);
        Assert.Equal(2, result.TotalTracks);
    }

    public void Dispose()
    {
        _httpClient?.Dispose();
        _mockHandler?.Dispose();
    }

    private sealed class MockHttpMessageHandler : HttpMessageHandler
    {
        private string _response = string.Empty;

        public void SetResponse(string response)
        {
            _response = response;
        }

        protected override Task<HttpResponseMessage> SendAsync(
            HttpRequestMessage request,
            CancellationToken cancellationToken)
        {
            return Task.FromResult(new HttpResponseMessage
            {
                StatusCode = HttpStatusCode.OK,
                Content = new StringContent(_response)
            });
        }
    }
}
