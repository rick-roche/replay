using System.Net;
using System.Text.Json;
using RePlay.Server.Models;
using RePlay.Server.Services;

namespace RePlay.Server.Tests.Services;

public class SpotifyMatchingReliabilityTests
{
    [Theory]
    [InlineData(401)]
    [InlineData(429)]
    [InlineData(503)]
    public async Task Searches_PreserveUpstreamFailureStatus(int status)
    {
        using var client = new HttpClient(new Handler((_, _) => new((HttpStatusCode)status)));
        var service = new SpotifyMatchingService(client);
        foreach (var search in new Func<Task>[] {
            async () => await service.SearchTracksAsync("query", "secret"),
            async () => await service.SearchAlbumsAsync("query", "secret"),
            async () => await service.SearchArtistsAsync("query", "secret") })
        {
            var error = await Assert.ThrowsAsync<SpotifyApiException>(search);
            Assert.Equal((HttpStatusCode)status, error.StatusCode);
            Assert.Equal("SPOTIFY_API_ERROR", error.ErrorCode);
        }
    }

    [Fact]
    public async Task Searches_UseDevelopmentModeLimit()
    {
        using var client = new HttpClient(new Handler((request, _) =>
        {
            Assert.Contains("limit=10", request.RequestUri!.Query);
            return Json("{}");
        }));
        var service = new SpotifyMatchingService(client);
        await service.SearchTracksAsync("query", "token");
        await service.SearchAlbumsAsync("query", "token");
        await service.SearchArtistsAsync("query", "token");
    }

    [Theory]
    [InlineData(null)]
    [InlineData("https://evil.example/steal")]
    public async Task AlbumTracks_PaginatesByFixedOriginOffset_EvenWithoutNext(string? next)
    {
        var paths = new List<string>();
        using var client = new HttpClient(new Handler((request, call) =>
        {
            Assert.Equal("api.spotify.com", request.RequestUri!.Host);
            paths.Add(request.RequestUri.PathAndQuery);
            if (call == 1) return Json("""{"albums":{"items":[{"id":"album1","name":"Album","uri":"spotify:album:album1","artists":[{"name":"Artist"}]}]}}""");
            var count = call == 2 ? 50 : 1;
            return Json(JsonSerializer.Serialize(new { items = Enumerable.Range(0, count).Select(i => new { id = $"{call}-{i}", name = "Track", uri = $"spotify:track:{call}-{i}", artists = new[] { new { name = "Artist" } } }), next }));
        }));
        var result = await new SpotifyMatchingService(client).MatchAlbumsAsync([new NormalizedAlbum { Name = "Album", Artist = "Artist", Tracks = [], Source = "test", SourceMetadata = [] }], "secret");
        Assert.Equal(51, result.Albums[0].Match!.Tracks.Count);
        Assert.Contains("offset=50", paths[2]);
    }

    [Theory]
    [InlineData(403)]
    [InlineData(404)]
    public async Task ArtistTopTracks_Unavailable_IsExplicitFailure(int status)
    {
        using var client = new HttpClient(new Handler((_, call) => call == 1
            ? Json("""{"artists":{"items":[{"id":"artist1","name":"Artist","uri":"spotify:artist:artist1"}]}}""")
            : new((HttpStatusCode)status)));
        var error = await Assert.ThrowsAsync<SpotifyApiException>(() => new SpotifyMatchingService(client).MatchArtistsAsync([new NormalizedArtist { Name = "Artist", Source = "test", SourceMetadata = [] }], "token"));
        Assert.Equal((HttpStatusCode)status, error.StatusCode);
        Assert.Equal("SPOTIFY_TOP_TRACKS_UNSUPPORTED", error.ErrorCode);
        Assert.Contains("not supported", error.Message);
    }

    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public async Task Playlist_BatchFailurePreservesConfirmedProgress(bool cancel)
    {
        using var client = new HttpClient(new Handler((request, call) =>
        {
            Assert.Equal(call == 1 ? "/v1/me/playlists" : "/v1/playlists/list1/items", request.RequestUri!.AbsolutePath);
            if (call == 1) return Json("""{"id":"list1","uri":"spotify:playlist:list1","external_urls":{"spotify":"https://open.spotify.com/playlist/list1"}}""");
            if (call == 2) return Json("{}");
            if (cancel) throw new OperationCanceledException();
            return new(HttpStatusCode.ServiceUnavailable);
        }));
        var result = await new SpotifyMatchingService(client).CreatePlaylistAsync(new PlaylistCreationRequest { Name = "Test", IsPublic = false, TrackUris = Enumerable.Range(0, 101).Select(i => $"spotify:track:{i}").ToList() }, "token", "user");
        Assert.Equal("list1", result.PlaylistId);
        Assert.Equal(100, result.TracksAdded);
        Assert.Equal("https://open.spotify.com/playlist/list1", result.Url);
        Assert.False(result.IsComplete);
        Assert.Equal("PLAYLIST_PARTIALLY_CREATED", result.ErrorCode);
        Assert.NotNull(result.ErrorMessage);
    }

    [Theory]
    [InlineData(401)]
    [InlineData(429)]
    [InlineData(503)]
    public async Task AlbumTracks_LaterPageFailureDoesNotReturnTruncatedSuccess(int status)
    {
        using var client = new HttpClient(new Handler((_, call) => call switch
        {
            1 => Json("""{"albums":{"items":[{"id":"album1","name":"Album","uri":"spotify:album:album1","artists":[{"name":"Artist"}]}]}}"""),
            2 => Json(JsonSerializer.Serialize(new { items = Enumerable.Range(0, 50).Select(i => new { id = $"{i}", name = "Track", uri = $"spotify:track:{i}", artists = new[] { new { name = "Artist" } } }) })),
            _ => new((HttpStatusCode)status)
        }));
        var error = await Assert.ThrowsAsync<SpotifyApiException>(() => new SpotifyMatchingService(client).MatchAlbumsAsync([new NormalizedAlbum { Name = "Album", Artist = "Artist", Tracks = [], Source = "test", SourceMetadata = [] }], "secret"));
        Assert.Equal((HttpStatusCode)status, error.StatusCode);
    }

    [Fact]
    public async Task AlbumTracks_UnendingFullPagesFailAtBoundRatherThanTruncate()
    {
        var calls = 0;
        using var client = new HttpClient(new Handler((_, call) =>
        {
            calls++;
            return call == 1
                ? Json("""{"albums":{"items":[{"id":"album1","name":"Album","uri":"spotify:album:album1","artists":[{"name":"Artist"}]}]}}""")
                : Json(JsonSerializer.Serialize(new { items = Enumerable.Range(0, 50).Select(i => new { id = $"{i}", name = "Track", uri = $"spotify:track:{i}", artists = new[] { new { name = "Artist" } } }) }));
        }));
        var error = await Assert.ThrowsAsync<SpotifyApiException>(() => new SpotifyMatchingService(client).MatchAlbumsAsync([new NormalizedAlbum { Name = "Album", Artist = "Artist", Tracks = [], Source = "test", SourceMetadata = [] }], "secret"));
        Assert.Equal("SPOTIFY_PAGINATION_LIMIT", error.ErrorCode);
        Assert.Equal(101, calls);
    }

    [Theory]
    [InlineData(400)]
    [InlineData(401)]
    [InlineData(403)]
    [InlineData(429)]
    public async Task Playlist_CreateFailureHasStatusAndDoesNotAppend(int status)
    {
        var calls = 0;
        using var client = new HttpClient(new Handler((_, _) =>
        {
            calls++;
            return new((HttpStatusCode)status);
        }));
        var error = await Assert.ThrowsAsync<SpotifyApiException>(() => new SpotifyMatchingService(client).CreatePlaylistAsync(new PlaylistCreationRequest { Name = "Test", IsPublic = false, TrackUris = ["spotify:track:1"] }, "secret", "user"));
        Assert.Equal((HttpStatusCode)status, error.StatusCode);
        Assert.Equal("SPOTIFY_API_ERROR", error.ErrorCode);
        Assert.Equal(1, calls);
    }

    [Theory]
    [InlineData("timeout")]
    [InlineData("transport")]
    [InlineData("cancellation")]
    [InlineData("circuit")]
    [InlineData("serverError")]
    public async Task Playlist_UnconfirmedCreateWarnsAgainstRetrying(string failure)
    {
        var calls = 0;
        using var client = new HttpClient(new Handler((_, _) =>
        {
            calls++;
            return failure switch
            {
                "timeout" => throw new Polly.Timeout.TimeoutRejectedException(),
                "transport" => throw new HttpRequestException("sensitive upstream detail"),
                "cancellation" => throw new OperationCanceledException(),
                "circuit" => throw new Polly.CircuitBreaker.BrokenCircuitException(),
                _ => new(HttpStatusCode.ServiceUnavailable)
            };
        }));
        var error = await Assert.ThrowsAsync<SpotifyApiException>(() => new SpotifyMatchingService(client).CreatePlaylistAsync(new PlaylistCreationRequest { Name = "Test", IsPublic = false, TrackUris = ["spotify:track:1"] }, "secret", "user"));
        Assert.Equal(HttpStatusCode.BadGateway, error.StatusCode);
        Assert.Equal("PLAYLIST_CREATION_UNCONFIRMED", error.ErrorCode);
        Assert.Equal("Spotify may have created the playlist. Check your Spotify library before trying again.", error.Message);
        Assert.Equal(1, calls);
    }

    [Theory]
    [InlineData("not json")]
    [InlineData("{}")]
    [InlineData("null")]
    [InlineData("[]")]
    [InlineData("{\"id\":null}")]
    [InlineData("{\"id\":\"\"}")]
    [InlineData("{\"id\":\"  \"}")]
    [InlineData("{\"id\":123}")]
    public async Task Playlist_UnreadableSuccessfulCreateWarnsAgainstRetrying(string body)
    {
        var calls = 0;
        using var client = new HttpClient(new Handler((_, _) =>
        {
            calls++;
            return Json(body);
        }));
        var error = await Assert.ThrowsAsync<SpotifyApiException>(() => new SpotifyMatchingService(client).CreatePlaylistAsync(new PlaylistCreationRequest { Name = "Test", IsPublic = false, TrackUris = ["spotify:track:1"] }, "secret", "user"));
        Assert.Equal(HttpStatusCode.BadGateway, error.StatusCode);
        Assert.Equal("PLAYLIST_CREATION_UNCONFIRMED", error.ErrorCode);
        Assert.Equal(1, calls);
    }

    [Fact]
    public async Task Playlist_CanceledBeforeCreateDoesNotSendOrReportAnUnconfirmedWrite()
    {
        var calls = 0;
        using var client = new HttpClient(new Handler((_, _) =>
        {
            calls++;
            return Json("{}");
        }));
        using var cancellation = new CancellationTokenSource();
        cancellation.Cancel();
        await Assert.ThrowsAnyAsync<OperationCanceledException>(() => new SpotifyMatchingService(client).CreatePlaylistAsync(new PlaylistCreationRequest { Name = "Test", IsPublic = false, TrackUris = ["spotify:track:1"] }, "secret", "user", cancellation.Token));
        Assert.Equal(0, calls);
    }

    [Fact]
    public async Task Playlist_CircuitRejectsAppend_PreservesCreatedPlaylist()
    {
        using var client = new HttpClient(new Handler((_, call) => call == 1
            ? Json("""{"id":"list1","uri":"spotify:playlist:list1","external_urls":{"spotify":"https://open.spotify.com/playlist/list1"}}""")
            : throw new Polly.CircuitBreaker.BrokenCircuitException()));
        var result = await new SpotifyMatchingService(client).CreatePlaylistAsync(new PlaylistCreationRequest { Name = "Test", IsPublic = false, TrackUris = ["spotify:track:1"] }, "secret", "user");
        Assert.Equal("list1", result.PlaylistId);
        Assert.Equal(0, result.TracksAdded);
        Assert.False(result.IsComplete);
        Assert.Equal("PLAYLIST_PARTIALLY_CREATED", result.ErrorCode);
    }

    private static HttpResponseMessage Json(string json) => new(HttpStatusCode.OK) { Content = new StringContent(json) };

    private sealed class Handler(Func<HttpRequestMessage, int, HttpResponseMessage> send) : HttpMessageHandler
    {
        private int _calls;
        protected override Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken) => Task.FromResult(send(request, ++_calls));
    }
}
