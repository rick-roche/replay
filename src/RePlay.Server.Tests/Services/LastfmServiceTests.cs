using FluentAssertions;
using Microsoft.Extensions.Options;
using RePlay.Server.Configuration;
using RePlay.Server.Models;
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

    [Fact]
    public async Task GetUserDataAsync_ShouldReturnTracks_WhenDataTypeIsTracks()
    {
        // Arrange
        var username = "testuser";
        var filter = new LastfmFilter
        {
            DataType = LastfmDataType.Tracks,
            TimePeriod = LastfmTimePeriod.Last12Months,
            MaxResults = 10
        };

        var responseJson = """
            {
                "toptracks": {
                    "track": [
                        {
                            "name": "Test Track 1",
                            "artist": { "name": "Test Artist 1" },
                            "playcount": "50"
                        },
                        {
                            "name": "Test Track 2",
                            "artist": { "name": "Test Artist 2" },
                            "playcount": "30"
                        }
                    ]
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
        var result = await _service.GetUserDataAsync(username, filter);

        // Assert
        result.Should().NotBeNull();
        result!.DataType.Should().Be(LastfmDataType.Tracks);
        result.Tracks.Should().HaveCount(2);
        result.Tracks[0].Name.Should().Be("Test Track 1");
        result.Tracks[0].Artist.Should().Be("Test Artist 1");
        result.Tracks[0].PlayCount.Should().Be(50);
    }

    [Fact]
    public async Task GetUserDataAsync_ShouldReturnAlbums_WhenDataTypeIsAlbums()
    {
        // Arrange
        var username = "testuser";
        var filter = new LastfmFilter
        {
            DataType = LastfmDataType.Albums,
            TimePeriod = LastfmTimePeriod.Last6Months,
            MaxResults = 5
        };

        var responseJson = """
            {
                "topalbums": {
                    "album": [
                        {
                            "name": "Album 1",
                            "artist": { "name": "Artist 1" },
                            "playcount": "25"
                        }
                    ]
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
        var result = await _service.GetUserDataAsync(username, filter);

        // Assert
        result.Should().NotBeNull();
        result!.DataType.Should().Be(LastfmDataType.Albums);
        result.Albums.Should().HaveCount(1);
        result.Albums[0].Name.Should().Be("Album 1");
    }

    [Fact]
    public async Task GetUserDataAsync_ShouldReturnArtists_WhenDataTypeIsArtists()
    {
        // Arrange
        var username = "testuser";
        var filter = new LastfmFilter
        {
            DataType = LastfmDataType.Artists,
            TimePeriod = LastfmTimePeriod.Overall,
            MaxResults = 20
        };

        var responseJson = """
            {
                "topartists": {
                    "artist": [
                        {
                            "name": "Artist 1",
                            "playcount": "100"
                        }
                    ]
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
        var result = await _service.GetUserDataAsync(username, filter);

        // Assert
        result.Should().NotBeNull();
        result!.DataType.Should().Be(LastfmDataType.Artists);
        result.Artists.Should().HaveCount(1);
        result.Artists[0].Name.Should().Be("Artist 1");
        result.Artists[0].PlayCount.Should().Be(100);
    }

    [Fact]
    public async Task GetUserDataAsync_ShouldReturnNull_WhenApiReturnsError()
    {
        // Arrange
        var filter = new LastfmFilter
        {
            DataType = LastfmDataType.Tracks,
            TimePeriod = LastfmTimePeriod.Overall,
            MaxResults = 50
        };

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
        var result = await _service.GetUserDataAsync("nonexistentuser", filter);

        // Assert
        result.Should().BeNull();
    }

    [Fact]
    public async Task GetUserDataAsync_ShouldThrowArgumentException_WhenCustomPeriodMissingDates()
    {
        // Arrange
        var filter = new LastfmFilter
        {
            DataType = LastfmDataType.Tracks,
            TimePeriod = LastfmTimePeriod.Custom,
            MaxResults = 50
            // No custom dates provided
        };

        // Act & Assert
        await _service.Invoking(s => s.GetUserDataAsync("testuser", filter))
            .Should().ThrowAsync<ArgumentException>();
    }

    [Fact]
    public async Task GetUserDataAsync_ShouldThrowArgumentException_WhenUsernameIsEmpty()
    {
        // Arrange
        var filter = new LastfmFilter
        {
            DataType = LastfmDataType.Tracks,
            TimePeriod = LastfmTimePeriod.Overall,
            MaxResults = 50
        };

        // Act & Assert
        await _service.Invoking(s => s.GetUserDataAsync("", filter))
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
