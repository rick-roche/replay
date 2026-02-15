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

    [Fact]
    public async Task MatchAlbumsAsync_WithExactAlbumMatch_ReturnsHighConfidence()
    {
        // Arrange
        var albums = new List<NormalizedAlbum>
        {
            new()
            {
                Name = "Rumours",
                Artist = "Fleetwood Mac",
                Tracks = new List<NormalizedTrack>(),
                Source = "lastfm",
                SourceMetadata = new Dictionary<string, object?>()
            }
        };

        // Album search response
        var albumSearchResponse = new
        {
            albums = new
            {
                items = new[]
                {
                    new
                    {
                        id = "albumId123",
                        name = "Rumours",
                        uri = "spotify:album:albumId123",
                        release_date = "1977-02-04",
                        total_tracks = 40,
                        artists = new[] { new { name = "Fleetwood Mac" } }
                    }
                }
            }
        };

        // Album tracks response
        var albumTracksResponse = new
        {
            items = new[]
            {
                new
                {
                    id = "trackId1",
                    name = "Dreams",
                    uri = "spotify:track:trackId1",
                    artists = new[] { new { name = "Fleetwood Mac" } },
                    album = new { name = "Rumours" }
                },
                new
                {
                    id = "trackId2",
                    name = "The Chain",
                    uri = "spotify:track:trackId2",
                    artists = new[] { new { name = "Fleetwood Mac" } },
                    album = new { name = "Rumours" }
                }
            }
        };

        _mockHandler.SetResponses(
            JsonSerializer.Serialize(albumSearchResponse),
            JsonSerializer.Serialize(albumTracksResponse)
        );

        // Act
        var result = await _service.MatchAlbumsAsync(albums, "test_access_token");

        // Assert
        Assert.Single(result.Albums);
        Assert.True(result.Albums[0].IsMatched);
        Assert.NotNull(result.Albums[0].Match);
        Assert.Equal("albumId123", result.Albums[0].Match!.SpotifyId);
        Assert.Equal(100, result.Albums[0].Match!.Confidence);
        Assert.Equal(MatchMethod.Exact, result.Albums[0].Match!.Method);
    }

    [Fact]
    public async Task MatchArtistsAsync_WithExactArtistMatch_ReturnsHighConfidence()
    {
        // Arrange
        var artists = new List<NormalizedArtist>
        {
            new()
            {
                Name = "The Beatles",
                Source = "lastfm",
                SourceMetadata = new Dictionary<string, object?>()
            }
        };

        // Artist search response
        var artistSearchResponse = new
        {
            artists = new
            {
                items = new[]
                {
                    new
                    {
                        id = "artistId123",
                        name = "The Beatles",
                        uri = "spotify:artist:artistId123",
                        genres = new[] { "rock", "pop" }
                    }
                }
            }
        };

        // Top tracks response
        var topTracksResponse = new
        {
            tracks = new[]
            {
                new
                {
                    id = "trackId1",
                    name = "Hey Jude",
                    uri = "spotify:track:trackId1",
                    artists = new[] { new { name = "The Beatles" } },
                    album = new { name = "Hey Jude" }
                },
                new
                {
                    id = "trackId2",
                    name = "Let It Be",
                    uri = "spotify:track:trackId2",
                    artists = new[] { new { name = "The Beatles" } },
                    album = new { name = "Let It Be" }
                }
            }
        };

        _mockHandler.SetResponses(
            JsonSerializer.Serialize(artistSearchResponse),
            JsonSerializer.Serialize(topTracksResponse)
        );

        // Act
        var result = await _service.MatchArtistsAsync(artists, "test_access_token");

        // Assert
        Assert.Single(result.Artists);
        Assert.True(result.Artists[0].IsMatched);
        Assert.NotNull(result.Artists[0].Match);
        Assert.Equal("artistId123", result.Artists[0].Match!.SpotifyId);
        Assert.Equal(100, result.Artists[0].Match!.Confidence);
        Assert.Equal(MatchMethod.Exact, result.Artists[0].Match!.Method);
    }

    [Fact]
    public async Task MatchAlbumsAsync_WithNoMatches_ReturnsUnmatched()
    {
        // Arrange
        var albums = new List<NormalizedAlbum>
        {
            new()
            {
                Name = "Unknown Album",
                Artist = "Unknown Artist",
                Tracks = new List<NormalizedTrack>(),
                Source = "lastfm",
                SourceMetadata = new Dictionary<string, object?>()
            }
        };

        var spotifyResponse = new
        {
            albums = new
            {
                items = Array.Empty<object>()
            }
        };

        _mockHandler.SetResponse(JsonSerializer.Serialize(spotifyResponse));

        // Act
        var result = await _service.MatchAlbumsAsync(albums, "test_access_token");

        // Assert
        Assert.Single(result.Albums);
        Assert.False(result.Albums[0].IsMatched);
        Assert.Null(result.Albums[0].Match);
        Assert.Equal(0, result.MatchedCount);
        Assert.Equal(1, result.UnmatchedCount);
    }

    [Fact]
    public async Task MatchArtistsAsync_WithNormalizedMatch_ReturnsGoodConfidence()
    {
        // Arrange
        var artists = new List<NormalizedArtist>
        {
            new()
            {
                Name = "the beatles",
                Source = "lastfm",
                SourceMetadata = new Dictionary<string, object?>()
            }
        };

        // Artist search response
        var artistSearchResponse = new
        {
            artists = new
            {
                items = new[]
                {
                    new
                    {
                        id = "artistId123",
                        name = "The Beatles",
                        uri = "spotify:artist:artistId123",
                        genres = new[] { "rock", "pop" }
                    }
                }
            }
        };

        // Top tracks response
        var topTracksResponse = new
        {
            tracks = new[]
            {
                new
                {
                    id = "trackId1",
                    name = "Hey Jude",
                    uri = "spotify:track:trackId1",
                    artists = new[] { new { name = "The Beatles" } },
                    album = new { name = "Hey Jude" }
                }
            }
        };

        _mockHandler.SetResponses(
            JsonSerializer.Serialize(artistSearchResponse),
            JsonSerializer.Serialize(topTracksResponse)
        );

        // Act
        var result = await _service.MatchArtistsAsync(artists, "test_access_token");

        // Assert
        Assert.Single(result.Artists);
        Assert.True(result.Artists[0].IsMatched);
        Assert.Equal(90, result.Artists[0].Match!.Confidence);
        Assert.Equal(MatchMethod.Normalized, result.Artists[0].Match!.Method);
    }

    public void Dispose()
    {
        _httpClient?.Dispose();
        _mockHandler?.Dispose();
    }

    private sealed class MockHttpMessageHandler : HttpMessageHandler
    {
        private string _response = string.Empty;
        private readonly Queue<string> _responses = new();

        public void SetResponse(string response)
        {
            _response = response;
            _responses.Clear();
        }

        public void SetResponses(params string[] responses)
        {
            _responses.Clear();
            foreach (var response in responses)
            {
                _responses.Enqueue(response);
            }
        }

        protected override Task<HttpResponseMessage> SendAsync(
            HttpRequestMessage request,
            CancellationToken cancellationToken)
        {
            var responseContent = _responses.Count > 0 ? _responses.Dequeue() : _response;
            
            return Task.FromResult(new HttpResponseMessage
            {
                StatusCode = HttpStatusCode.OK,
                Content = new StringContent(responseContent)
            });
        }
    }
}
