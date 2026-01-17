using FluentAssertions;
using Microsoft.Extensions.Options;
using RePlay.Server.Configuration;
using RePlay.Server.Services;
using System.Net;

namespace RePlay.Server.Tests.Services;

public class LastfmServiceTests
{
    private readonly TestHttpMessageHandler _httpMessageHandler;
    private readonly HttpClient _httpClient;
    private readonly IOptions<LastfmOptions> _options;
    private readonly LastfmService _service;

    public LastfmServiceTests()
    {
        _httpMessageHandler = new TestHttpMessageHandler();
        _httpClient = new HttpClient(_httpMessageHandler);

        _options = Options.Create(new LastfmOptions
        {
            ApiKey = "test-api-key",
            ApiUrl = "https://www.last.fm/2.0/"
        });

        _service = new LastfmService(_httpClient, _options);
    }

    [Fact]
    public async Task GetUserAsync_ShouldReturnUser_WhenUserExists()
    {
        // Arrange
        var username = "testuser";
        var responseJson = """
            {
                "user": {
                    "name": "testuser",
                    "playcount": "42000",
                    "registered": {
                        "unixtime": "1234567890"
                    }
                }
            }
            """;

        var response = new HttpResponseMessage
        {
            StatusCode = HttpStatusCode.OK,
            Content = new StringContent(responseJson)
        };
        _httpMessageHandler.EnqueueResponse(response);

        // Act
        var user = await _service.GetUserAsync(username);

        // Assert
        user.Should().NotBeNull();
        user!.Username.Should().Be("testuser");
        user.PlayCount.Should().Be(42000);
    }

    [Fact]
    public async Task GetUserAsync_ShouldReturnNull_WhenUserNotFound()
    {
        // Arrange
        var responseJson = """
            {
                "error": 6,
                "message": "User not found"
            }
            """;

        var response = new HttpResponseMessage
        {
            StatusCode = HttpStatusCode.OK,
            Content = new StringContent(responseJson)
        };
        _httpMessageHandler.EnqueueResponse(response);

        // Act
        var user = await _service.GetUserAsync("nonexistentuser");

        // Assert
        user.Should().BeNull();
    }

    [Fact]
    public async Task GetUserAsync_ShouldThrowArgumentException_WhenUsernameIsEmpty()
    {
        // Act & Assert
        await _service.Invoking(s => s.GetUserAsync(""))
            .Should().ThrowAsync<ArgumentException>();
    }

    private sealed class TestHttpMessageHandler : HttpMessageHandler
    {
        private readonly Queue<HttpResponseMessage> _responses = new();

        public void EnqueueResponse(HttpResponseMessage response)
        {
            _responses.Enqueue(response);
        }

        protected override Task<HttpResponseMessage> SendAsync(
            HttpRequestMessage request,
            CancellationToken cancellationToken)
        {
            if (_responses.Count == 0)
            {
                throw new InvalidOperationException("No responses queued");
            }

            return Task.FromResult(_responses.Dequeue());
        }
    }
}
