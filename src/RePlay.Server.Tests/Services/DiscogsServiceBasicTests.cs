using System.Net;
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

    [Fact]
    public async Task GetCollectionNormalizedAsync_WithNullFilter_Throws()
    {
        await Assert.ThrowsAsync<ArgumentNullException>(() => _service.GetCollectionNormalizedAsync("user", null!));
    }

    [Fact]
    public async Task GetCollectionNormalizedAsync_WithEmptyUsername_Throws()
    {
        var filter = new DiscogsFilter { MaxTracks = 100 };
        await Assert.ThrowsAsync<ArgumentException>(() => _service.GetCollectionNormalizedAsync("", filter));
    }

    [Theory]
    [InlineData("https://www.discogs.com.evil.test/user/alice/collection")]
    [InlineData("alice/../../admin")]
    [InlineData("alice?secret=value")]
    [InlineData("https://www.discogs.com/user/alice/collection?redirect=evil")]
    [InlineData("https://www.discogs.com/user/alice/extra/../collection")]
    [InlineData("https://www.discogs.com/user/alice/extra/%2e%2e/collection")]
    public async Task GetProfileAsync_RejectsUntrustedOrInjectedIdentifiers(string identifier)
    {
        await Assert.ThrowsAsync<ArgumentException>(() => _service.GetProfileAsync(identifier));
        _httpHandler.Requests.Should().BeEmpty();
    }

    [Fact]
    public async Task GetProfileAsync_NormalizesActualCollectionUrlBeforeRequest()
    {
        _httpHandler.EnqueueResponse("""{"username":"alice"}""");
        _httpHandler.EnqueueResponse("""{"pagination":{"items":4},"releases":[]}""");
        var profile = await _service.GetProfileAsync(" https://www.discogs.com/user/alice/collection ");
        profile!.Username.Should().Be("alice");
        _httpHandler.Requests[0].Should().Be("https://api.discogs.com/users/alice");
    }

    [Theory]
    [InlineData(0)]
    [InlineData(-1)]
    [InlineData(501)]
    public async Task GetCollectionAsync_RejectsUnboundedTrackLimits(int limit)
    {
        await Assert.ThrowsAsync<ArgumentException>(() => _service.GetCollectionAsync("alice", new DiscogsFilter { MaxTracks = limit }));
        _httpHandler.Requests.Should().BeEmpty();
    }

    [Fact]
    public async Task GetCollectionAsync_StopsAfterEnoughTracksWithoutRefetchingFirstPage()
    {
        _httpHandler.EnqueueResponse("""{"username":"alice"}""");
        _httpHandler.EnqueueResponse("""{"pagination":{"pages":1000,"items":100000},"releases":[{"basic_information":{"id":42,"title":"Album","year":2024}}]}""");
        _httpHandler.EnqueueResponse("""{"id":42,"title":"Album","tracklist":[{"title":"Song"}]}""");

        var result = await _service.GetCollectionAsync("alice", new DiscogsFilter { MaxTracks = 1 });

        result!.Tracks.Should().ContainSingle(track => track.Name == "Song");
        _httpHandler.Requests.Should().HaveCount(3);
        _httpHandler.Requests[2].Should().Be("https://api.discogs.com/releases/42");
    }

    [Fact]
    public async Task GetCollectionAsync_ThrowsRatherThanReturningTruncatedScan()
    {
        _httpHandler.EnqueueResponse("""{"username":"alice"}""");
        for (var page = 1; page <= 100; page++)
            _httpHandler.EnqueueResponse("""{"pagination":{"pages":101,"items":101},"releases":[]}""");

        var error = await Assert.ThrowsAsync<InvalidOperationException>(() => _service.GetCollectionAsync("alice", new DiscogsFilter { MaxTracks = 1 }));
        error.Message.Should().Contain("100");
        _httpHandler.Requests.Should().HaveCount(101);
    }

    [Fact]
    public async Task GetCollectionAsync_FiltersEachPageBeforeFetchingDetails()
    {
        _httpHandler.EnqueueResponse("""{"username":"alice"}""");
        _httpHandler.EnqueueResponse("""{"pagination":{"pages":2,"items":2},"releases":[{"basic_information":{"id":1,"year":1999}}]}""");
        _httpHandler.EnqueueResponse("""{"pagination":{"pages":2,"items":2},"releases":[{"basic_information":{"id":2,"year":2024}}]}""");
        _httpHandler.EnqueueResponse("""{"id":2,"title":"Album","tracklist":[{"title":"Song"}]}""");

        var result = await _service.GetCollectionAsync("alice", new DiscogsFilter { MaxTracks = 1, MinReleaseYear = 2020 });

        result!.Tracks.Should().ContainSingle(track => track.Name == "Song");
        _httpHandler.Requests.Should().HaveCount(4);
        _httpHandler.Requests.Should().NotContain("https://api.discogs.com/releases/1");
    }

    [Theory]
    [InlineData(HttpStatusCode.Unauthorized)]
    [InlineData(HttpStatusCode.TooManyRequests)]
    [InlineData(HttpStatusCode.ServiceUnavailable)]
    public async Task GetProfileAsync_PreservesProviderFailureStatus(HttpStatusCode status)
    {
        _httpHandler.Status = status;
        var error = await Assert.ThrowsAsync<HttpRequestException>(() => _service.GetProfileAsync("alice"));
        error.StatusCode.Should().Be(status);
    }

    [Fact]
    public async Task GetCollectionAsync_RejectsInvalidProviderReleaseIds()
    {
        _httpHandler.EnqueueResponse("""{"username":"alice"}""");
        _httpHandler.EnqueueResponse("""{"pagination":{"pages":1,"items":1},"releases":[{"basic_information":{"id":0}}]}""");
        await Assert.ThrowsAsync<System.Text.Json.JsonException>(() => _service.GetCollectionAsync("alice", new DiscogsFilter()));
        _httpHandler.Requests.Should().HaveCount(2);
    }

    [Fact]
    public async Task GetProfileAsync_DisposesOutboundRequestOnProviderFailure()
    {
        await _service.GetProfileAsync("alice");
        await Assert.ThrowsAsync<ObjectDisposedException>(() => _httpHandler.LastRequest!.Content!.ReadAsStringAsync());
    }

    private sealed class MockHttpHandler : HttpMessageHandler
    {
        private readonly Queue<string> _responses = new();
        public List<string> Requests { get; } = [];
        public HttpStatusCode Status { get; set; } = HttpStatusCode.NotFound;
        public HttpRequestMessage? LastRequest { get; private set; }

        public void EnqueueResponse(string json) => _responses.Enqueue(json);

        protected override Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken)
        {
            LastRequest = request;
            // Sentinel content makes ownership/disposal of the outbound request observable.
            request.Content = new StringContent("");
            Requests.Add(request.RequestUri!.ToString());
            if (_responses.Count == 0)
            {
                return Task.FromResult(new HttpResponseMessage { StatusCode = Status });
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
