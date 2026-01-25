using FluentAssertions;
using Microsoft.Extensions.Options;
using RePlay.Server.Configuration;
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
        var payload = """
            {
                "userId": "exampleUser",
                "fullName": "Example Person",
                "url": "https://www.setlist.fm/user/exampleUser",
                "stats": {
                    "attended": 42
                }
            }
            """;

        _handler.Enqueue(new HttpResponseMessage
        {
            StatusCode = HttpStatusCode.OK,
            Content = new StringContent(payload)
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
