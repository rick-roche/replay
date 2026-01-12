using FluentAssertions;
using Microsoft.Extensions.Options;
using RePlay.Server.Configuration;
using RePlay.Server.Services;
using System.Net;

namespace RePlay.Server.Tests.Services;

public class SpotifyAuthServiceTests
{
    private readonly TestHttpMessageHandler _httpMessageHandler;
    private readonly HttpClient _httpClient;
    private readonly IOptions<SpotifyOptions> _options;
    private readonly SpotifyAuthService _service;

    public SpotifyAuthServiceTests()
    {
        _httpMessageHandler = new TestHttpMessageHandler();
        _httpClient = new HttpClient(_httpMessageHandler);

        _options = Options.Create(new SpotifyOptions
        {
            ClientId = "test-client-id",
            ClientSecret = "test-client-secret",
            RedirectUri = "https://localhost/callback"
        });

        _service = new SpotifyAuthService(_httpClient, _options);
    }

    [Fact]
    public void GetAuthorizationUrl_ShouldGenerateCorrectUrl()
    {
        // Arrange
        var state = "test-state";

        // Act
        var url = _service.GetAuthorizationUrl(state);

        // Assert
        url.Should().StartWith("https://accounts.spotify.com/authorize?");
        url.Should().Contain("client_id=test-client-id");
        url.Should().Contain("response_type=code");
        url.Should().Contain("redirect_uri=https%3A%2F%2Flocalhost%2Fcallback");
        url.Should().Contain("state=test-state");
        url.Should().Contain("scope=playlist-modify-private%20playlist-modify-public%20user-read-email");
    }

    [Fact]
    public async Task ExchangeCodeAsync_ShouldReturnAuthSession_WhenSuccessful()
    {
        // Arrange
        var code = "test-authorization-code";

        var tokenResponse = """
            {
                "access_token": "test-access-token",
                "token_type": "Bearer",
                "expires_in": 3600,
                "refresh_token": "test-refresh-token",
                "scope": "playlist-modify-private"
            }
            """;

        var userProfileResponse = """
            {
                "id": "user123",
                "display_name": "Test User",
                "email": "test@example.com",
                "images": [
                    {
                        "url": "https://example.com/avatar.jpg",
                        "height": 300,
                        "width": 300
                    }
                ]
            }
            """;

        _httpMessageHandler.EnqueueResponse(new HttpResponseMessage(HttpStatusCode.OK)
        {
            Content = new StringContent(tokenResponse)
        });
        _httpMessageHandler.EnqueueResponse(new HttpResponseMessage(HttpStatusCode.OK)
        {
            Content = new StringContent(userProfileResponse)
        });

        // Act
        var session = await _service.ExchangeCodeAsync(code);

        // Assert
        session.Should().NotBeNull();
        session.SessionId.Should().NotBeNullOrEmpty();
        session.AccessToken.Should().Be("test-access-token");
        session.RefreshToken.Should().Be("test-refresh-token");
        session.User.Id.Should().Be("user123");
        session.User.DisplayName.Should().Be("Test User");
        session.User.Email.Should().Be("test@example.com");
        session.User.ImageUrl.Should().Be("https://example.com/avatar.jpg");
        session.ExpiresAt.Should().BeCloseTo(DateTime.UtcNow.AddSeconds(3600), TimeSpan.FromSeconds(5));
    }

    [Fact]
    public async Task RefreshTokenAsync_ShouldReturnUpdatedSession_WhenSuccessful()
    {
        // Arrange
        var refreshToken = "test-refresh-token";

        var tokenResponse = """
            {
                "access_token": "new-access-token",
                "token_type": "Bearer",
                "expires_in": 3600,
                "refresh_token": "new-refresh-token",
                "scope": "playlist-modify-private"
            }
            """;

        var userProfileResponse = """
            {
                "id": "user123",
                "display_name": "Test User",
                "email": "test@example.com",
                "images": []
            }
            """;

        _httpMessageHandler.EnqueueResponse(new HttpResponseMessage(HttpStatusCode.OK)
        {
            Content = new StringContent(tokenResponse)
        });
        _httpMessageHandler.EnqueueResponse(new HttpResponseMessage(HttpStatusCode.OK)
        {
            Content = new StringContent(userProfileResponse)
        });

        // Act
        var session = await _service.RefreshTokenAsync(refreshToken);

        // Assert
        session.Should().NotBeNull();
        session.AccessToken.Should().Be("new-access-token");
        session.RefreshToken.Should().Be("new-refresh-token");
        session.User.Id.Should().Be("user123");
    }

    [Fact]
    public async Task GetUserProfileAsync_ShouldReturnUser_WhenSuccessful()
    {
        // Arrange
        var accessToken = "test-access-token";

        var userProfileResponse = """
            {
                "id": "user123",
                "display_name": "Test User",
                "email": null,
                "images": []
            }
            """;

        _httpMessageHandler.EnqueueResponse(new HttpResponseMessage(HttpStatusCode.OK)
        {
            Content = new StringContent(userProfileResponse)
        });

        // Act
        var user = await _service.GetUserProfileAsync(accessToken);

        // Assert
        user.Should().NotBeNull();
        user.Id.Should().Be("user123");
        user.DisplayName.Should().Be("Test User");
        user.Email.Should().BeNull();
        user.ImageUrl.Should().BeNull();
    }

    [Fact]
    public async Task ExchangeCodeAsync_ShouldThrow_WhenApiReturnsError()
    {
        // Arrange
        var code = "invalid-code";

        _httpMessageHandler.EnqueueResponse(new HttpResponseMessage(HttpStatusCode.BadRequest)
        {
            Content = new StringContent("Invalid authorization code")
        });

        // Act & Assert
        await _service.Invoking(s => s.ExchangeCodeAsync(code))
            .Should().ThrowAsync<HttpRequestException>();
    }

    /// <summary>
    /// Test message handler that allows queueing responses.
    /// </summary>
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
