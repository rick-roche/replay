using FluentAssertions;
using Microsoft.Extensions.Logging.Abstractions;
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

        _service = new LastfmService(_httpClient, _options, NullLogger<LastfmService>.Instance);
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
    public async Task GetUserDataAsync_ShouldUseWeeklyCharts_ForCustomPeriod()
    {
        // Arrange
        var username = "testuser";
        var filter = new LastfmFilter
        {
            DataType = LastfmDataType.Tracks,
            TimePeriod = LastfmTimePeriod.Custom,
            CustomStartDate = "2024-01-01T00:00:00Z",
            CustomEndDate = "2024-01-08T00:00:00Z",
            MaxResults = 5
        };

        var responseJson = """
            {
                "weeklytrackchart": {
                    "track": [
                        {
                            "name": "Weekly Track",
                            "artist": { "#text": "Weekly Artist" },
                            "playcount": "15"
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
        var expectedFrom = new DateTimeOffset(2024, 1, 1, 0, 0, 0, TimeSpan.Zero).ToUnixTimeSeconds().ToString();
        var expectedTo = new DateTimeOffset(2024, 1, 8, 0, 0, 0, TimeSpan.Zero).ToUnixTimeSeconds().ToString();

        _httpMessageHandler.LastRequestUri.Should().NotBeNull();
        var query = _httpMessageHandler.LastRequestUri!.Query;
        query.Should().Contain("user.getWeeklyTrackChart");
        query.Should().Contain($"from={expectedFrom}");
        query.Should().Contain($"to={expectedTo}");

        result.Should().NotBeNull();
        result!.Tracks.Should().ContainSingle();
        result.Tracks[0].Name.Should().Be("Weekly Track");
        result.Tracks[0].Artist.Should().Be("Weekly Artist");
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

    [Fact]
    public async Task GetUserDataNormalizedAsync_ShouldReturnNormalizedTracks()
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
        var result = await _service.GetUserDataNormalizedAsync(username, filter);

        // Assert
        result.Should().NotBeNull();
        result!.DataType.Should().Be("Tracks");
        result.Source.Should().Be("lastfm");
        result.Tracks.Should().HaveCount(1);
        
        var track = result.Tracks[0];
        track.Name.Should().Be("Test Track 1");
        track.Artist.Should().Be("Test Artist 1");
        track.Source.Should().Be("lastfm");
        track.SourceMetadata.Should().ContainKey("playCount");
        track.SourceMetadata["playCount"].Should().Be(50);
    }

    [Fact]
    public async Task GetUserDataNormalizedAsync_ShouldReturnNormalizedAlbums()
    {
        // Arrange
        var username = "testuser";
        var filter = new LastfmFilter
        {
            DataType = LastfmDataType.Albums,
            TimePeriod = LastfmTimePeriod.Overall,
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
        var result = await _service.GetUserDataNormalizedAsync(username, filter);

        // Assert
        result.Should().NotBeNull();
        result!.DataType.Should().Be("Albums");
        result.Source.Should().Be("lastfm");
        result.Albums.Should().HaveCount(1);
        
        var album = result.Albums[0];
        album.Name.Should().Be("Album 1");
        album.Artist.Should().Be("Artist 1");
        album.Source.Should().Be("lastfm");
        album.SourceMetadata["playCount"].Should().Be(25);
    }

    [Fact]
    public async Task GetUserDataNormalizedAsync_ShouldReturnNormalizedArtists()
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
        var result = await _service.GetUserDataNormalizedAsync(username, filter);

        // Assert
        result.Should().NotBeNull();
        result!.DataType.Should().Be("Artists");
        result.Source.Should().Be("lastfm");
        result.Artists.Should().HaveCount(1);
        
        var artist = result.Artists[0];
        artist.Name.Should().Be("Artist 1");
        artist.Source.Should().Be("lastfm");
        artist.SourceMetadata["playCount"].Should().Be(100);
    }

    [Fact]
    public async Task GetUserDataNormalizedAsync_ShouldReturnNull_WhenSourceDataIsNull()
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
        var result = await _service.GetUserDataNormalizedAsync("nonexistentuser", filter);

        // Assert
        result.Should().BeNull();
    }

    private sealed class TestHttpMessageHandler : HttpMessageHandler
    {
        private readonly Queue<HttpResponseMessage> _responses = new();

        public Uri? LastRequestUri { get; private set; }

        public void EnqueueResponse(HttpResponseMessage response)
        {
            _responses.Enqueue(response);
        }

        protected override Task<HttpResponseMessage> SendAsync(
            HttpRequestMessage request,
            CancellationToken cancellationToken)
        {
            LastRequestUri = request.RequestUri;

            if (_responses.Count == 0)
            {
                throw new InvalidOperationException("No responses queued");
            }

            return Task.FromResult(_responses.Dequeue());
        }
    }
}
