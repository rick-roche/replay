using RePlay.Server.Models;
using RePlay.Server.Services;
using Xunit;

namespace RePlay.Server.Tests.Services;

public class PlaylistCreationTests
{
    [Fact]
    public async Task CreatePlaylistAsync_WithValidRequest_ReturnsPlaylistCreationResponse()
    {
        // Arrange
        var mockHttpClientHandler = new MockHttpClientHandler();
        mockHttpClientHandler.RegisterResponse(
            "https://api.spotify.com/v1/users/userid123/playlists",
            HttpMethod.Post,
            new StringContent("""
                {
                  "id": "playlistid123",
                  "uri": "spotify:playlist:playlistid123",
                  "external_urls": {
                    "spotify": "https://open.spotify.com/playlist/playlistid123"
                  }
                }
                """));

        mockHttpClientHandler.RegisterResponse(
            "https://api.spotify.com/v1/playlists/playlistid123/tracks",
            HttpMethod.Post,
            new StringContent("""
                {
                  "snapshot_id": "snapshot123"
                }
                """));

        var httpClient = new HttpClient(mockHttpClientHandler);
        var service = new SpotifyMatchingService(httpClient);

        var request = new PlaylistCreationRequest
        {
            Name = "Test Playlist",
            Description = "A test playlist",
            IsPublic = true,
            TrackUris = new List<string>
            {
                "spotify:track:track1",
                "spotify:track:track2"
            }
        };

        // Act
        var response = await service.CreatePlaylistAsync(
            request,
            "access_token_123",
            "userid123");

        // Assert
        Assert.NotNull(response);
        Assert.Equal("playlistid123", response.PlaylistId);
        Assert.Equal("spotify:playlist:playlistid123", response.Uri);
        Assert.Equal("https://open.spotify.com/playlist/playlistid123", response.Url);
        Assert.Equal(2, response.TracksAdded);
    }

    [Fact]
    public async Task CreatePlaylistAsync_WithEmptyTrackUris_ThrowsInvalidOperationException()
    {
        // Arrange
        var httpClient = new HttpClient(new MockHttpClientHandler());
        var service = new SpotifyMatchingService(httpClient);

        var request = new PlaylistCreationRequest
        {
            Name = "Test Playlist",
            Description = "A test playlist",
            IsPublic = true,
            TrackUris = new List<string>()
        };

        // Act & Assert
        await Assert.ThrowsAsync<InvalidOperationException>(
            () => service.CreatePlaylistAsync(
                request,
                "access_token_123",
                "userid123"));
    }

    [Fact]
    public async Task CreatePlaylistAsync_WithNullAccessToken_ThrowsArgumentNullException()
    {
        // Arrange
        var httpClient = new HttpClient(new MockHttpClientHandler());
        var service = new SpotifyMatchingService(httpClient);

        var request = new PlaylistCreationRequest
        {
            Name = "Test Playlist",
            Description = "A test playlist",
            IsPublic = true,
            TrackUris = new List<string> { "spotify:track:track1" }
        };

        // Act & Assert
        await Assert.ThrowsAsync<ArgumentNullException>(
            () => service.CreatePlaylistAsync(
                request,
                null!,
                "userid123"));
    }

    [Fact]
    public async Task CreatePlaylistAsync_WithNullUserId_ThrowsArgumentNullException()
    {
        // Arrange
        var httpClient = new HttpClient(new MockHttpClientHandler());
        var service = new SpotifyMatchingService(httpClient);

        var request = new PlaylistCreationRequest
        {
            Name = "Test Playlist",
            Description = "A test playlist",
            IsPublic = true,
            TrackUris = new List<string> { "spotify:track:track1" }
        };

        // Act & Assert
        await Assert.ThrowsAsync<ArgumentNullException>(
            () => service.CreatePlaylistAsync(
                request,
                "access_token_123",
                null!));
    }

    [Fact]
    public async Task CreatePlaylistAsync_WithLargeTrackList_BatchesAddsCorrectly()
    {
        // Arrange
        var mockHttpClientHandler = new MockHttpClientHandler();
        mockHttpClientHandler.RegisterResponse(
            "https://api.spotify.com/v1/users/userid123/playlists",
            HttpMethod.Post,
            new StringContent("""
                {
                  "id": "playlistid123",
                  "uri": "spotify:playlist:playlistid123",
                  "external_urls": {
                    "spotify": "https://open.spotify.com/playlist/playlistid123"
                  }
                }
                """));

        mockHttpClientHandler.RegisterResponse(
            "https://api.spotify.com/v1/playlists/playlistid123/tracks",
            HttpMethod.Post,
            new StringContent("""
                {
                  "snapshot_id": "snapshot123"
                }
                """));

        var httpClient = new HttpClient(mockHttpClientHandler);
        var service = new SpotifyMatchingService(httpClient);

        // Create a list of 250 tracks to test batching (2 batches)
        var trackUris = Enumerable.Range(1, 250)
            .Select(i => $"spotify:track:track{i}")
            .ToList();

        var request = new PlaylistCreationRequest
        {
            Name = "Large Playlist",
            Description = "Playlist with 250 tracks",
            IsPublic = false,
            TrackUris = trackUris
        };

        // Act
        var response = await service.CreatePlaylistAsync(
            request,
            "access_token_123",
            "userid123");

        // Assert
        Assert.NotNull(response);
        Assert.Equal("playlistid123", response.PlaylistId);
        Assert.Equal(250, response.TracksAdded);
        // Should have been called twice (100 + 100 + 50)
        Assert.Equal(3, mockHttpClientHandler.GetRequestCount("https://api.spotify.com/v1/playlists/playlistid123/tracks"));
    }
}

/// <summary>
/// Mock HTTP client handler for testing Spotify API calls without making actual HTTP requests.
/// </summary>
public class MockHttpClientHandler : HttpClientHandler
{
    private readonly Dictionary<string, (HttpMethod method, HttpContent response)> _responses = new();
    private readonly Dictionary<string, int> _requestCounts = new();

    public void RegisterResponse(string url, HttpMethod method, HttpContent response)
    {
        _responses[$"{method} {url}"] = (method, response);
    }

    protected override async Task<HttpResponseMessage> SendAsync(
        HttpRequestMessage request,
        CancellationToken cancellationToken)
    {
        var key = $"{request.Method} {request.RequestUri}";

        // Track request count
        if (_requestCounts.ContainsKey(request.RequestUri?.ToString() ?? string.Empty))
        {
            _requestCounts[request.RequestUri?.ToString() ?? string.Empty]++;
        }
        else
        {
            _requestCounts[request.RequestUri?.ToString() ?? string.Empty] = 1;
        }

        if (_responses.TryGetValue(key, out var response))
        {
            return new HttpResponseMessage(System.Net.HttpStatusCode.OK)
            {
                Content = response.response
            };
        }

        // Generic success response for unregistered requests
        return new HttpResponseMessage(System.Net.HttpStatusCode.OK)
        {
            Content = new StringContent("{}")
        };
    }

    public int GetRequestCount(string url)
    {
        return _requestCounts.TryGetValue(url, out var count) ? count : 0;
    }
}

