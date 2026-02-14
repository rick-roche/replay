using System.Net;
using System.Text.Json;
using FluentAssertions;
using Microsoft.Extensions.Options;
using RePlay.Server.Configuration;
using RePlay.Server.Models;
using RePlay.Server.Services;

namespace RePlay.Server.Tests.Services;

public class DiscogsServiceBasicTests : IDisposable
{
    private readonly MockHttpHandler _httpHandler;
    private readonly HttpClient _httpClient;
    private readonly DiscogsService _service;

    public DiscogsServiceBasicTests()
    {
        _httpHandler = new MockHttpHandler();
        _httpClient = new HttpClient(_httpHandler);

        var options = Options.Create(new DiscogsOptions
        {
            ApiUrl = "https://api.discogs.com",
            ConsumerKey = "test-key",
            ConsumerSecret = "test-secret"
        });

        _service = new DiscogsService(_httpClient, options);
    }

    public void Dispose()
    {
        _httpClient?.Dispose();
    }

    [Fact]
    public async Task GetProfileAsync_WithValidUsername_ReturnsProfile()
    {
        var userJson = JsonSerializer.Serialize(new DiscogsUserResponse { Username = "testuser" });
        var collectionJson = JsonSerializer.Serialize(new DiscogsCollectionResponse
        {
            Pagination = new DiscogsPagination { Items = 10, Pages = 1, Page = 1, Per_Page = 100 },
            Releases = []
        });

        _httpHandler.EnqueueResponse(userJson);
        _httpHandler.EnqueueResponse(collectionJson);

        var result = await _service.GetProfileAsync("testuser");

        result.Should().NotBeNull();
        result!.Username.Should().Be("testuser");
    }

    [Fact]
    public async Task GetProfileAsync_WithNullUsername_Throws()
    {
        await Assert.ThrowsAsync<ArgumentException>(() => _service.GetProfileAsync(null!));
    }

    [Fact]
    public async Task GetProfileAsync_WithEmptyUsername_Throws()
    {
        await Assert.ThrowsAsync<ArgumentException>(() => _service.GetProfileAsync(""));
    }

    [Fact]
    public async Task GetCollectionAsync_WithNullFilter_Throws()
    {
        await Assert.ThrowsAsync<ArgumentNullException>(() => _service.GetCollectionAsync("user", null!));
    }

    [Fact]
    public async Task GetCollectionAsync_WithEmptyUsername_Throws()
    {
        var filter = new DiscogsFilter { MaxTracks = 100 };
        await Assert.ThrowsAsync<ArgumentException>(() => _service.GetCollectionAsync("", filter));
    }

    private sealed class MockHttpHandler : HttpMessageHandler
    {
        private readonly Queue<string> _responses = new();

        public void EnqueueResponse(string json) => _responses.Enqueue(json);

        protected override Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken)
        {
            if (_responses.Count == 0)
            {
                return Task.FromResult(new HttpResponseMessage { StatusCode = HttpStatusCode.NotFound });
            }

            var json = _responses.Dequeue();
            return Task.FromResult(new HttpResponseMessage
            {
                StatusCode = HttpStatusCode.OK,
                Content = new StringContent(json)
            });
        }
    }
}
