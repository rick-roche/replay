using System.Net;
using System.Text.Json;
using RePlay.Server.Models;
using RePlay.Server.Services;

namespace RePlay.Server.Tests.Services;

public class SpotifySearchTests : IDisposable
{
    private readonly HttpClient _httpClient;
    private readonly SpotifyMatchingService _service;
    private readonly MockHttpMessageHandler _mockHandler;

    public SpotifySearchTests()
    {
        _mockHandler = new MockHttpMessageHandler();
        _httpClient = new HttpClient(_mockHandler);
        _service = new SpotifyMatchingService(_httpClient);
    }

    [Fact]
    public async Task SearchTracksAsync_ReturnsTopTenAndMapsFields()
    {
        // Arrange
        var spotifyResponse = new
        {
            tracks = new
            {
                items = Enumerable.Range(1, 15).Select(i => new
                {
                    id = $"id{i}",
                    name = $"Name {i}",
                    uri = $"spotify:track:id{i}",
                    artists = new[] { new { name = $"Artist {i}" } },
                    album = new { name = $"Album {i}" }
                }).ToArray()
            }
        };
        _mockHandler.SetResponse(JsonSerializer.Serialize(spotifyResponse), HttpStatusCode.OK);

        // Act
        var results = await _service.SearchTracksAsync("track:foo artist:bar", "token");

        // Assert
        Assert.Equal(10, results.Count);
        Assert.Equal("id1", results[0].Id);
        Assert.Equal("Name 1", results[0].Name);
        Assert.Equal("Artist 1", results[0].Artist);
        Assert.Equal("Album 1", results[0].Album);
        Assert.Equal("spotify:track:id1", results[0].Uri);
    }

    [Fact]
    public async Task SearchTracksAsync_EmptyQuery_ReturnsEmpty()
    {
        var results = await _service.SearchTracksAsync("", "token");
        Assert.Empty(results);
    }

    [Fact]
    public async Task SearchTracksAsync_NonSuccessStatus_ThrowsProviderFailure()
    {
        // Arrange
        _mockHandler.SetResponse("{}", HttpStatusCode.Unauthorized);

        // Act
        var error = await Assert.ThrowsAsync<SpotifyApiException>(() => _service.SearchTracksAsync("track:foo", "token"));
        Assert.Equal(HttpStatusCode.Unauthorized, error.StatusCode);
    }

    public void Dispose()
    {
        _httpClient?.Dispose();
        _mockHandler?.Dispose();
    }

    private sealed class MockHttpMessageHandler : HttpMessageHandler
    {
        private string _response = string.Empty;
        private HttpStatusCode _statusCode = HttpStatusCode.OK;

        public void SetResponse(string response, HttpStatusCode statusCode)
        {
            _response = response;
            _statusCode = statusCode;
        }

        protected override Task<HttpResponseMessage> SendAsync(
            HttpRequestMessage request,
            CancellationToken cancellationToken)
        {
            return Task.FromResult(new HttpResponseMessage
            {
                StatusCode = _statusCode,
                Content = new StringContent(_response)
            });
        }
    }
}
