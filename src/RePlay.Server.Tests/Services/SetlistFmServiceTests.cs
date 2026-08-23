using FluentAssertions;
using Microsoft.Extensions.Options;
using RePlay.Server.Configuration;
using RePlay.Server.Models;
using RePlay.Server.Services;
using System.Net;

namespace RePlay.Server.Tests.Services;

public sealed class SetlistFmServiceTests
{
    private readonly TestHttpMessageHandler _handler;
    private readonly HttpClient _httpClient;
    private readonly IOptions<SetlistFmOptions> _options;
    private readonly SetlistFmService _service;

    public SetlistFmServiceTests()
    {
        _handler = new TestHttpMessageHandler();
        _httpClient = new HttpClient(_handler);
        _options = Options.Create(new SetlistFmOptions
        {
            ApiKey = "test-key",
            ApiUrl = "https://api.setlist.fm/rest/1.0",
            UserAgent = "RePlay.Tests"
        });
        _service = new SetlistFmService(_httpClient, _options);
    }

    [Fact]
    public async Task GetUserAsync_ReturnsUser_WhenFound()
    {
        var userPayload = """
            {
                "userId": "exampleUser",
                "fullname": "Example Person",
                "url": "https://www.setlist.fm/user/exampleUser"
            }
            """;

        var attendedPayload = """
            {
                "setlist": [],
                "total": 42,
                "page": 1,
                "itemsPerPage": 1
            }
            """;

        // First call returns user profile
        _handler.Enqueue(new HttpResponseMessage
        {
            StatusCode = HttpStatusCode.OK,
            Content = new StringContent(userPayload)
        });

        // Second call returns attended concerts count
        _handler.Enqueue(new HttpResponseMessage
        {
            StatusCode = HttpStatusCode.OK,
            Content = new StringContent(attendedPayload)
        });

        var user = await _service.GetUserAsync("exampleUser");

        user.Should().NotBeNull();
        user!.UserId.Should().Be("exampleUser");
        user.DisplayName.Should().Be("Example Person");
        user.AttendedConcerts.Should().Be(42);
    }

    [Fact]
    public async Task GetUserAsync_ReturnsNull_WhenNotFound()
    {
        _handler.Enqueue(new HttpResponseMessage
        {
            StatusCode = HttpStatusCode.NotFound
        });

        var user = await _service.GetUserAsync("missing");
        user.Should().BeNull();
    }

    [Fact]
    public async Task GetUserAsync_ReturnsNull_WhenPayloadInvalid()
    {
        var payload = """
            {
                "fullName": "No Id"
            }
            """;

        _handler.Enqueue(new HttpResponseMessage
        {
            StatusCode = HttpStatusCode.OK,
            Content = new StringContent(payload)
        });

        var user = await _service.GetUserAsync("nope");
        user.Should().BeNull();
    }

    [Fact]
    public async Task GetUserAsync_Throws_WhenUsernameMissing()
    {
        await FluentActions.Invoking(() => _service.GetUserAsync(""))
            .Should()
            .ThrowAsync<ArgumentException>();
    }

    [Fact]
    public async Task GetUserConcertsAsync_FetchesSelectedConcertsDirectly_AndHonorsTrackLimit()
    {
        var setlistPayload = """
            {
              "id": "concert-1",
              "eventDate": "20-01-2024",
              "artist": { "name": "Example Artist" },
              "sets": {
                "set": [{
                  "song": [{ "name": "First Song" }, { "name": "Second Song" }]
                }]
              }
            }
            """;
        _handler.Enqueue(new HttpResponseMessage(HttpStatusCode.OK) { Content = new StringContent(setlistPayload) });

        var result = await _service.GetUserConcertsAsync(
            "exampleUser",
            new SetlistFmFilter { MaxConcerts = 2, MaxTracks = 1 },
            ["concert-1"]);

        result.Concerts.Should().ContainSingle(concert => concert.Id == "concert-1");
        result.Tracks.Should().ContainSingle(track => track.Name == "First Song");
    }

    [Fact]
    public async Task GetUserConcertsAsync_RejectsSelectionsBeyondConfiguredConcertLimit()
    {
        await FluentActions.Invoking(() => _service.GetUserConcertsAsync(
                "exampleUser",
                new SetlistFmFilter { MaxConcerts = 1 },
                ["concert-1", "concert-2"]))
            .Should()
            .ThrowAsync<ArgumentException>();
    }

    [Fact]
    public async Task GetUserConcertsAsync_RejectsDuplicateOrBlankSelectedConcertIds()
    {
        await FluentActions.Invoking(() => _service.GetUserConcertsAsync(
                "exampleUser",
                new SetlistFmFilter { MaxConcerts = 2 },
                ["concert-1", "CONCERT-1"]))
            .Should()
            .ThrowAsync<ArgumentException>();

        await FluentActions.Invoking(() => _service.GetUserConcertsAsync(
                "exampleUser",
                new SetlistFmFilter { MaxConcerts = 2 },
                ["concert-1", " "]))
            .Should()
            .ThrowAsync<ArgumentException>();
    }

    [Fact]
    public async Task GetUserConcertsAsync_RejectsInvalidFilterBoundsAndDates()
    {
        await FluentActions.Invoking(() => _service.GetUserConcertsAsync(
                "exampleUser",
                new SetlistFmFilter { MaxTracks = 501 }))
            .Should()
            .ThrowAsync<ArgumentException>();

        await FluentActions.Invoking(() => _service.GetUserConcertsAsync(
                "exampleUser",
                new SetlistFmFilter { StartDate = "20-01-2024" }))
            .Should()
            .ThrowAsync<ArgumentException>();

        await FluentActions.Invoking(() => _service.GetUserConcertsAsync(
                "exampleUser",
                new SetlistFmFilter { StartDate = "2024-02-01", EndDate = "2024-01-01" }))
            .Should()
            .ThrowAsync<ArgumentException>();
    }

    [Fact]
    public async Task GetUserConcertsPageAsync_ThrowsWhenProviderRequestFails()
    {
        _handler.Enqueue(new HttpResponseMessage(HttpStatusCode.TooManyRequests));

        await FluentActions.Invoking(() => _service.GetUserConcertsPageAsync(
                "exampleUser",
                new SetlistFmFilter(),
                pageNumber: 1,
                pageSize: 20))
            .Should()
            .ThrowAsync<HttpRequestException>();
    }

    [Fact]
    public async Task GetUserConcertsPageAsync_SkipsConcertsWithoutIds()
    {
        var attendedPayload = """
            {
              "setlist": [
                { "artist": { "name": "Missing ID" } },
                { "id": "concert-1", "artist": { "name": "Example Artist" } }
              ],
              "total": 2,
              "page": 1,
              "itemsPerPage": 20
            }
            """;
        _handler.Enqueue(new HttpResponseMessage(HttpStatusCode.OK) { Content = new StringContent(attendedPayload) });

        var result = await _service.GetUserConcertsPageAsync("exampleUser", new SetlistFmFilter(), 1, 20);

        result.Concerts.Should().ContainSingle(concert => concert.Id == "concert-1");
    }

    [Fact]
    public async Task GetUserConcertsAsync_RejectsSelectedSetlistWithoutId()
    {
        _handler.Enqueue(new HttpResponseMessage(HttpStatusCode.OK)
        {
            Content = new StringContent("""{ "artist": { "name": "Missing ID" } }""")
        });

        await FluentActions.Invoking(() => _service.GetUserConcertsAsync(
                "exampleUser",
                new SetlistFmFilter(),
                ["concert-1"]))
            .Should()
            .ThrowAsync<InvalidOperationException>();
    }

    private sealed class TestHttpMessageHandler : HttpMessageHandler
    {
        private readonly Queue<HttpResponseMessage> _responses = new();

        public void Enqueue(HttpResponseMessage response) => _responses.Enqueue(response);

        protected override Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken)
        {
            if (_responses.Count == 0)
            {
                throw new InvalidOperationException("No responses queued.");
            }

            return Task.FromResult(_responses.Dequeue());
        }
    }
}
