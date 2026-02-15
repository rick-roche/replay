using System.Reflection;
using FluentAssertions;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Http.HttpResults;
using RePlay.Server.Endpoints;
using RePlay.Server.Models;
using RePlay.Server.Services;

namespace RePlay.Server.Tests.Endpoints;

public class MatchingEndpointsTests
{
    private static MethodInfo GetPrivate(string name)
    {
        var mi = typeof(MatchingEndpoints).GetMethod(name, BindingFlags.NonPublic | BindingFlags.Static);
        mi.Should().NotBeNull();
        return mi!;
    }

    private static async Task<IResult> InvokeAsync(MethodInfo mi, params object?[] args)
    {
        var task = (Task<IResult>)mi.Invoke(null, args)!;
        return await task.ConfigureAwait(false);
    }

    private sealed class FakeSpotifyMatchingService : ISpotifyMatchingService
    {
        public Func<IReadOnlyList<NormalizedTrack>, string, CancellationToken, Task<MatchedDataResponse>>? OnMatchTracksAsync { get; set; }
        public Func<string, string, CancellationToken, Task<IReadOnlyList<SpotifyTrack>>>? OnSearchTracksAsync { get; set; }
        public Func<IReadOnlyList<NormalizedAlbum>, string, CancellationToken, Task<MatchedAlbumsResponse>>? OnMatchAlbumsAsync { get; set; }
        public Func<string, string, CancellationToken, Task<IReadOnlyList<SpotifyAlbumInfo>>>? OnSearchAlbumsAsync { get; set; }
        public Func<IReadOnlyList<NormalizedArtist>, string, CancellationToken, Task<MatchedArtistsResponse>>? OnMatchArtistsAsync { get; set; }
        public Func<string, string, CancellationToken, Task<IReadOnlyList<SpotifyArtistInfo>>>? OnSearchArtistsAsync { get; set; }
        public Func<PlaylistCreationRequest, string, string, CancellationToken, Task<PlaylistCreationResponse>>? OnCreatePlaylistAsync { get; set; }

        public Task<MatchedDataResponse> MatchTracksAsync(IReadOnlyList<NormalizedTrack> tracks, string accessToken, CancellationToken cancellationToken = default)
            => OnMatchTracksAsync?.Invoke(tracks, accessToken, cancellationToken) ?? Task.FromResult(new MatchedDataResponse { Tracks = new List<MatchedTrack>() });

        public Task<IReadOnlyList<SpotifyTrack>> SearchTracksAsync(string query, string accessToken, CancellationToken cancellationToken = default)
            => OnSearchTracksAsync?.Invoke(query, accessToken, cancellationToken) ?? Task.FromResult<IReadOnlyList<SpotifyTrack>>(Array.Empty<SpotifyTrack>());

        public Task<MatchedAlbumsResponse> MatchAlbumsAsync(IReadOnlyList<NormalizedAlbum> albums, string accessToken, CancellationToken cancellationToken = default)
            => OnMatchAlbumsAsync?.Invoke(albums, accessToken, cancellationToken) ?? Task.FromResult(new MatchedAlbumsResponse { Albums = new List<MatchedAlbum>() });

        public Task<IReadOnlyList<SpotifyAlbumInfo>> SearchAlbumsAsync(string query, string accessToken, CancellationToken cancellationToken = default)
            => OnSearchAlbumsAsync?.Invoke(query, accessToken, cancellationToken) ?? Task.FromResult<IReadOnlyList<SpotifyAlbumInfo>>(Array.Empty<SpotifyAlbumInfo>());

        public Task<MatchedArtistsResponse> MatchArtistsAsync(IReadOnlyList<NormalizedArtist> artists, string accessToken, CancellationToken cancellationToken = default)
            => OnMatchArtistsAsync?.Invoke(artists, accessToken, cancellationToken) ?? Task.FromResult(new MatchedArtistsResponse { Artists = new List<MatchedArtist>() });

        public Task<IReadOnlyList<SpotifyArtistInfo>> SearchArtistsAsync(string query, string accessToken, CancellationToken cancellationToken = default)
            => OnSearchArtistsAsync?.Invoke(query, accessToken, cancellationToken) ?? Task.FromResult<IReadOnlyList<SpotifyArtistInfo>>(Array.Empty<SpotifyArtistInfo>());

        public Task<PlaylistCreationResponse> CreatePlaylistAsync(PlaylistCreationRequest request, string accessToken, string userId, CancellationToken cancellationToken = default)
            => OnCreatePlaylistAsync?.Invoke(request, accessToken, userId, cancellationToken) ?? Task.FromResult(new PlaylistCreationResponse { PlaylistId = "id", Uri = "uri", Url = "url", TracksAdded = 0 });
    }

    private static HttpContext ContextWithSessionCookie(string? sessionId = "sid")
    {
        var ctx = new DefaultHttpContext();
        if (sessionId != null)
        {
            ctx.Request.Headers.Append("Cookie", $"replay_session_id={sessionId}");
        }
        return ctx;
    }

    [Fact]
    public async Task MatchTracksAsync_MissingSession_ReturnsUnauthorized()
    {
        var mi = GetPrivate("MatchTracksAsync");
        var ctx = new DefaultHttpContext();
        var result = await InvokeAsync(mi,
            new MatchTracksRequest { Tracks = new List<NormalizedTrack> { new() { Name = "t", Artist = "a", Source = "s", SourceMetadata = new() } } },
            new FakeSpotifyMatchingService(),
            new InMemorySessionStore(),
            ctx,
            (object)CancellationToken.None);

        result.Should().BeOfType<UnauthorizedHttpResult>();
    }

    [Fact]
    public async Task MatchTracksAsync_InvalidRequest_ReturnsBadRequest()
    {
        var mi = GetPrivate("MatchTracksAsync");
        var ctx = ContextWithSessionCookie();
        var store = new InMemorySessionStore();
        store.StoreSession(new AuthSession { SessionId = "sid", AccessToken = "a", RefreshToken = "r", ExpiresAt = DateTime.UtcNow.AddHours(1), User = new SpotifyUser { Id = "id", DisplayName = "name" } });

        var result = await InvokeAsync(mi,
            new MatchTracksRequest { Tracks = new List<NormalizedTrack>() },
            new FakeSpotifyMatchingService(),
            store,
            ctx,
            (object)CancellationToken.None);

        result.Should().BeOfType<BadRequest<ApiError>>();
        ((BadRequest<ApiError>)result).Value!.Code.Should().Be("INVALID_REQUEST");
    }

    [Fact]
    public async Task MatchTracksAsync_Success_ReturnsOk()
    {
        var mi = GetPrivate("MatchTracksAsync");
        var ctx = ContextWithSessionCookie();
        var store = new InMemorySessionStore();
        store.StoreSession(new AuthSession { SessionId = "sid", AccessToken = "a", RefreshToken = "r", ExpiresAt = DateTime.UtcNow.AddHours(1), User = new SpotifyUser { Id = "id", DisplayName = "name" } });
        var fake = new FakeSpotifyMatchingService
        {
            OnMatchTracksAsync = (tracks, token, ct) => Task.FromResult(new MatchedDataResponse
            {
                Tracks = new List<MatchedTrack>
                {
                    new() { SourceTrack = tracks[0], Match = new SpotifyMatch { SpotifyId = "x", Name = "n", Artist = "a", Album = null, Uri = "u", Confidence = 100, Method = MatchMethod.Exact } }
                }
            })
        };

        var request = new MatchTracksRequest { Tracks = new List<NormalizedTrack> { new() { Name = "t", Artist = "a", Source = "s", SourceMetadata = new() } } };

        var result = await InvokeAsync(mi,
            request,
            fake,
            store,
            ctx,
            (object)CancellationToken.None);

        result.Should().BeOfType<Ok<MatchedDataResponse>>();
        var ok = (Ok<MatchedDataResponse>)result;
        ok.Value!.Tracks.Should().HaveCount(1);
        ok.Value!.MatchedCount.Should().Be(1);
    }

    [Fact]
    public async Task SearchTracksAsync_InvalidQuery_ReturnsBadRequest()
    {
        var mi = GetPrivate("SearchTracksAsync");
        var ctx = ContextWithSessionCookie();
        var result = await InvokeAsync(mi,
            (string?)"",
            new FakeSpotifyMatchingService(),
            new InMemorySessionStore(),
            ctx,
            (object)CancellationToken.None);

        result.Should().BeOfType<BadRequest<ApiError>>();
        ((BadRequest<ApiError>)result).Value!.Code.Should().Be("INVALID_QUERY");
    }

    [Fact]
    public async Task SearchTracksAsync_MissingSession_ReturnsUnauthorized()
    {
        var mi = GetPrivate("SearchTracksAsync");
        var ctx = new DefaultHttpContext();
        var result = await InvokeAsync(mi,
            (string?)"query",
            new FakeSpotifyMatchingService(),
            new InMemorySessionStore(),
            ctx,
            (object)CancellationToken.None);

        result.Should().BeOfType<UnauthorizedHttpResult>();
    }

    [Fact]
    public async Task SearchTracksAsync_Success_ReturnsOkWithResults()
    {
        var mi = GetPrivate("SearchTracksAsync");
        var ctx = ContextWithSessionCookie();
        var store = new InMemorySessionStore();
        store.StoreSession(new AuthSession { SessionId = "sid", AccessToken = "a", RefreshToken = "r", ExpiresAt = DateTime.UtcNow.AddHours(1), User = new SpotifyUser { Id = "id", DisplayName = "name" } });
        var fake = new FakeSpotifyMatchingService
        {
            OnSearchTracksAsync = (q, token, ct) => Task.FromResult<IReadOnlyList<SpotifyTrack>>(new List<SpotifyTrack>
            {
                new() { Id = "1", Name = "Track 1", Artist = "Artist 1", Album = "Album 1", Uri = "spotify:track:1" },
                new() { Id = "2", Name = "Track 2", Artist = "Artist 2", Album = "Album 2", Uri = "spotify:track:2" }
            })
        };

        var result = await InvokeAsync(mi,
            (string?)"query",
            fake,
            store,
            ctx,
            (object)CancellationToken.None);

        result.Should().BeOfType<Ok<IReadOnlyList<SpotifyTrack>>>();
        var ok = (Ok<IReadOnlyList<SpotifyTrack>>)result;
        ok.Value!.Should().HaveCount(2);
        ok.Value![0].Name.Should().Be("Track 1");
    }

    [Fact]
    public async Task MatchTracksAsync_NullSessionId_ReturnsUnauthorized()
    {
        var mi = GetPrivate("MatchTracksAsync");
        var ctx = new DefaultHttpContext();
        var result = await InvokeAsync(mi,
            new MatchTracksRequest { Tracks = new List<NormalizedTrack> { new() { Name = "t", Artist = "a", Source = "s", SourceMetadata = new() } } },
            new FakeSpotifyMatchingService(),
            new InMemorySessionStore(),
            ctx,
            (object)CancellationToken.None);

        result.Should().BeOfType<UnauthorizedHttpResult>();
    }

    [Fact]
    public async Task MatchTracksAsync_EmptySessionId_ReturnsUnauthorized()
    {
        var mi = GetPrivate("MatchTracksAsync");
        var ctx = new DefaultHttpContext();
        ctx.Request.Headers.Append("Cookie", "replay_session_id=");
        var result = await InvokeAsync(mi,
            new MatchTracksRequest { Tracks = new List<NormalizedTrack> { new() { Name = "t", Artist = "a", Source = "s", SourceMetadata = new() } } },
            new FakeSpotifyMatchingService(),
            new InMemorySessionStore(),
            ctx,
            (object)CancellationToken.None);

        result.Should().BeOfType<UnauthorizedHttpResult>();
    }

    [Fact]
    public async Task MatchTracksAsync_NullTracks_ReturnsBadRequest()
    {
        var mi = GetPrivate("MatchTracksAsync");
        var ctx = ContextWithSessionCookie();
        var store = new InMemorySessionStore();
        store.StoreSession(new AuthSession { SessionId = "sid", AccessToken = "a", RefreshToken = "r", ExpiresAt = DateTime.UtcNow.AddHours(1), User = new SpotifyUser { Id = "id", DisplayName = "name" } });

        var result = await InvokeAsync(mi,
            new MatchTracksRequest { Tracks = null! },
            new FakeSpotifyMatchingService(),
            store,
            ctx,
            (object)CancellationToken.None);

        result.Should().BeOfType<BadRequest<ApiError>>();
        ((BadRequest<ApiError>)result).Value!.Code.Should().Be("INVALID_REQUEST");
    }

    [Fact]
    public async Task MatchTracksAsync_MultipleValidTracks()
    {
        var mi = GetPrivate("MatchTracksAsync");
        var ctx = ContextWithSessionCookie();
        var store = new InMemorySessionStore();
        store.StoreSession(new AuthSession { SessionId = "sid", AccessToken = "a", RefreshToken = "r", ExpiresAt = DateTime.UtcNow.AddHours(1), User = new SpotifyUser { Id = "id", DisplayName = "name" } });
        var fake = new FakeSpotifyMatchingService
        {
            OnMatchTracksAsync = (tracks, token, ct) => Task.FromResult(new MatchedDataResponse
            {
                Tracks = tracks.Select(t => new MatchedTrack { SourceTrack = t, Match = new SpotifyMatch { SpotifyId = "x", Name = "n", Artist = "a", Album = null, Uri = "u", Confidence = 100, Method = MatchMethod.Exact } }).ToList()
            })
        };

        var request = new MatchTracksRequest
        {
            Tracks = new List<NormalizedTrack>
            {
                new() { Name = "Track 1", Artist = "Artist 1", Source = "source", SourceMetadata = new() },
                new() { Name = "Track 2", Artist = "Artist 2", Source = "source", SourceMetadata = new() },
                new() { Name = "Track 3", Artist = "Artist 3", Source = "source", SourceMetadata = new() }
            }
        };

        var result = await InvokeAsync(mi, request, fake, store, ctx, (object)CancellationToken.None);

        result.Should().BeOfType<Ok<MatchedDataResponse>>();
        var ok = (Ok<MatchedDataResponse>)result;
        ok.Value!.Tracks.Should().HaveCount(3);
    }

    [Fact]
    public async Task MatchTracksAsync_ServiceThrowsHttpRequestException()
    {
        var mi = GetPrivate("MatchTracksAsync");
        var ctx = ContextWithSessionCookie();
        var store = new InMemorySessionStore();
        store.StoreSession(new AuthSession { SessionId = "sid", AccessToken = "a", RefreshToken = "r", ExpiresAt = DateTime.UtcNow.AddHours(1), User = new SpotifyUser { Id = "id", DisplayName = "name" } });
        var fake = new FakeSpotifyMatchingService
        {
            OnMatchTracksAsync = (tracks, token, ct) => throw new HttpRequestException("Spotify API error")
        };

        var request = new MatchTracksRequest { Tracks = new List<NormalizedTrack> { new() { Name = "t", Artist = "a", Source = "s", SourceMetadata = new() } } };

        var result = await InvokeAsync(mi, request, fake, store, ctx, (object)CancellationToken.None);

        result.Should().BeOfType<ProblemHttpResult>();
        var problem = (ProblemHttpResult)result;
        problem.StatusCode.Should().Be(StatusCodes.Status503ServiceUnavailable);
    }

    [Fact]
    public async Task MatchTracksAsync_ServiceThrowsGenericException()
    {
        var mi = GetPrivate("MatchTracksAsync");
        var ctx = ContextWithSessionCookie();
        var store = new InMemorySessionStore();
        store.StoreSession(new AuthSession { SessionId = "sid", AccessToken = "a", RefreshToken = "r", ExpiresAt = DateTime.UtcNow.AddHours(1), User = new SpotifyUser { Id = "id", DisplayName = "name" } });
        var fake = new FakeSpotifyMatchingService
        {
            OnMatchTracksAsync = (tracks, token, ct) => throw new Exception("Unexpected error")
        };

        var request = new MatchTracksRequest { Tracks = new List<NormalizedTrack> { new() { Name = "t", Artist = "a", Source = "s", SourceMetadata = new() } } };

        var result = await InvokeAsync(mi, request, fake, store, ctx, (object)CancellationToken.None);

        result.Should().BeOfType<ProblemHttpResult>();
        var problem = (ProblemHttpResult)result;
        problem.StatusCode.Should().Be(StatusCodes.Status500InternalServerError);
    }

    [Fact]
    public async Task SearchTracksAsync_NullSessionId_ReturnsUnauthorized()
    {
        var mi = GetPrivate("SearchTracksAsync");
        var ctx = new DefaultHttpContext();
        var result = await InvokeAsync(mi,
            (string?)"query",
            new FakeSpotifyMatchingService(),
            new InMemorySessionStore(),
            ctx,
            (object)CancellationToken.None);

        result.Should().BeOfType<UnauthorizedHttpResult>();
    }

    [Fact]
    public async Task SearchTracksAsync_EmptyQuery_ReturnsBadRequest()
    {
        var mi = GetPrivate("SearchTracksAsync");
        var ctx = ContextWithSessionCookie();
        var result = await InvokeAsync(mi,
            (string?)"",
            new FakeSpotifyMatchingService(),
            new InMemorySessionStore(),
            ctx,
            (object)CancellationToken.None);

        result.Should().BeOfType<BadRequest<ApiError>>();
        ((BadRequest<ApiError>)result).Value!.Code.Should().Be("INVALID_QUERY");
    }

    [Fact]
    public async Task SearchTracksAsync_WhitespaceQuery_ReturnsBadRequest()
    {
        var mi = GetPrivate("SearchTracksAsync");
        var ctx = ContextWithSessionCookie();
        var result = await InvokeAsync(mi,
            (string?)"   ",
            new FakeSpotifyMatchingService(),
            new InMemorySessionStore(),
            ctx,
            (object)CancellationToken.None);

        result.Should().BeOfType<BadRequest<ApiError>>();
        ((BadRequest<ApiError>)result).Value!.Code.Should().Be("INVALID_QUERY");
    }

    [Fact]
    public async Task SearchTracksAsync_InvalidSessionId_ReturnsUnauthorized()
    {
        var mi = GetPrivate("SearchTracksAsync");
        var ctx = ContextWithSessionCookie("invalid_session_id");
        var store = new InMemorySessionStore();
        var result = await InvokeAsync(mi,
            (string?)"query",
            new FakeSpotifyMatchingService(),
            store,
            ctx,
            (object)CancellationToken.None);

        result.Should().BeOfType<UnauthorizedHttpResult>();
    }

    [Fact]
    public async Task SearchTracksAsync_EmptyResults()
    {
        var mi = GetPrivate("SearchTracksAsync");
        var ctx = ContextWithSessionCookie();
        var store = new InMemorySessionStore();
        store.StoreSession(new AuthSession { SessionId = "sid", AccessToken = "a", RefreshToken = "r", ExpiresAt = DateTime.UtcNow.AddHours(1), User = new SpotifyUser { Id = "id", DisplayName = "name" } });
        var fake = new FakeSpotifyMatchingService
        {
            OnSearchTracksAsync = (q, token, ct) => Task.FromResult<IReadOnlyList<SpotifyTrack>>(new List<SpotifyTrack>())
        };

        var result = await InvokeAsync(mi,
            (string?)"nonexistent",
            fake,
            store,
            ctx,
            (object)CancellationToken.None);

        result.Should().BeOfType<Ok<IReadOnlyList<SpotifyTrack>>>();
        var ok = (Ok<IReadOnlyList<SpotifyTrack>>)result;
        ok.Value!.Should().HaveCount(0);
    }

    [Fact]
    public async Task SearchTracksAsync_ServiceThrowsHttpRequestException()
    {
        var mi = GetPrivate("SearchTracksAsync");
        var ctx = ContextWithSessionCookie();
        var store = new InMemorySessionStore();
        store.StoreSession(new AuthSession { SessionId = "sid", AccessToken = "a", RefreshToken = "r", ExpiresAt = DateTime.UtcNow.AddHours(1), User = new SpotifyUser { Id = "id", DisplayName = "name" } });
        var fake = new FakeSpotifyMatchingService
        {
            OnSearchTracksAsync = (q, token, ct) => throw new HttpRequestException("Spotify unavailable")
        };

        var result = await InvokeAsync(mi,
            (string?)"query",
            fake,
            store,
            ctx,
            (object)CancellationToken.None);

        result.Should().BeOfType<ProblemHttpResult>();
        var problem = (ProblemHttpResult)result;
        problem.StatusCode.Should().Be(StatusCodes.Status503ServiceUnavailable);
    }

    [Fact]
    public async Task SearchTracksAsync_ServiceThrowsGenericException()
    {
        var mi = GetPrivate("SearchTracksAsync");
        var ctx = ContextWithSessionCookie();
        var store = new InMemorySessionStore();
        store.StoreSession(new AuthSession { SessionId = "sid", AccessToken = "a", RefreshToken = "r", ExpiresAt = DateTime.UtcNow.AddHours(1), User = new SpotifyUser { Id = "id", DisplayName = "name" } });
        var fake = new FakeSpotifyMatchingService
        {
            OnSearchTracksAsync = (q, token, ct) => throw new Exception("Unexpected error")
        };

        var result = await InvokeAsync(mi,
            (string?)"query",
            fake,
            store,
            ctx,
            (object)CancellationToken.None);

        result.Should().BeOfType<ProblemHttpResult>();
        var problem = (ProblemHttpResult)result;
        problem.StatusCode.Should().Be(StatusCodes.Status500InternalServerError);
    }

    [Fact]
    public async Task SearchTracksAsync_LongQueryString()
    {
        var mi = GetPrivate("SearchTracksAsync");
        var ctx = ContextWithSessionCookie();
        var store = new InMemorySessionStore();
        store.StoreSession(new AuthSession { SessionId = "sid", AccessToken = "a", RefreshToken = "r", ExpiresAt = DateTime.UtcNow.AddHours(1), User = new SpotifyUser { Id = "id", DisplayName = "name" } });
        var fake = new FakeSpotifyMatchingService
        {
            OnSearchTracksAsync = (q, token, ct) => Task.FromResult<IReadOnlyList<SpotifyTrack>>(new List<SpotifyTrack>
            {
                new() { Id = "1", Name = "Match", Artist = "Artist", Album = "Album", Uri = "spotify:track:1" }
            })
        };

        var longQuery = new string('a', 500);
        var result = await InvokeAsync(mi,
            (string?)longQuery,
            fake,
            store,
            ctx,
            (object)CancellationToken.None);

        result.Should().BeOfType<Ok<IReadOnlyList<SpotifyTrack>>>();
    }

    [Fact]
    public async Task MatchTracksAsync_PartialMatch()
    {
        var mi = GetPrivate("MatchTracksAsync");
        var ctx = ContextWithSessionCookie();
        var store = new InMemorySessionStore();
        store.StoreSession(new AuthSession { SessionId = "sid", AccessToken = "a", RefreshToken = "r", ExpiresAt = DateTime.UtcNow.AddHours(1), User = new SpotifyUser { Id = "id", DisplayName = "name" } });
        var fake = new FakeSpotifyMatchingService
        {
            OnMatchTracksAsync = (tracks, token, ct) => Task.FromResult(new MatchedDataResponse
            {
                Tracks = new List<MatchedTrack>
                {
                    new() { SourceTrack = tracks[0], Match = new SpotifyMatch { SpotifyId = "x", Name = "n", Artist = "a", Album = null, Uri = "u", Confidence = 100, Method = MatchMethod.Exact } }
                    // Only matching one out of multiple
                }
            })
        };

        var request = new MatchTracksRequest
        {
            Tracks = new List<NormalizedTrack>
            {
                new() { Name = "Track 1", Artist = "Artist 1", Source = "source", SourceMetadata = new() },
                new() { Name = "Track 2", Artist = "Artist 2", Source = "source", SourceMetadata = new() }
            }
        };

        var result = await InvokeAsync(mi, request, fake, store, ctx, (object)CancellationToken.None);

        result.Should().BeOfType<Ok<MatchedDataResponse>>();
        var ok = (Ok<MatchedDataResponse>)result;
        ok.Value!.Tracks.Should().HaveCount(1);
    }

    [Fact]
    public async Task MatchAlbumsAsync_InvalidRequest_ReturnsBadRequest()
    {
        var mi = GetPrivate("MatchAlbumsAsync");
        var ctx = ContextWithSessionCookie();
        var store = new InMemorySessionStore();
        store.StoreSession(new AuthSession { SessionId = "sid", AccessToken = "a", RefreshToken = "r", ExpiresAt = DateTime.UtcNow.AddHours(1), User = new SpotifyUser { Id = "id", DisplayName = "name" } });

        var result = await InvokeAsync(mi,
            new MatchAlbumsRequest { Albums = new List<NormalizedAlbum>() },
            new FakeSpotifyMatchingService(),
            store,
            ctx,
            (object)CancellationToken.None);

        result.Should().BeOfType<BadRequest<ApiError>>();
        ((BadRequest<ApiError>)result).Value!.Code.Should().Be("INVALID_REQUEST");
    }

    [Fact]
    public async Task MatchAlbumsAsync_MissingSession_ReturnsUnauthorized()
    {
        var mi = GetPrivate("MatchAlbumsAsync");
        var ctx = new DefaultHttpContext();
        var result = await InvokeAsync(mi,
            new MatchAlbumsRequest { Albums = new List<NormalizedAlbum> { new() { Name = "a", Artist = "ar", Source = "s", SourceMetadata = new(), Tracks = new() } } },
            new FakeSpotifyMatchingService(),
            new InMemorySessionStore(),
            ctx,
            (object)CancellationToken.None);

        result.Should().BeOfType<UnauthorizedHttpResult>();
    }

    [Fact]
    public async Task MatchAlbumsAsync_Success_ReturnsOk()
    {
        var mi = GetPrivate("MatchAlbumsAsync");
        var ctx = ContextWithSessionCookie();
        var store = new InMemorySessionStore();
        store.StoreSession(new AuthSession { SessionId = "sid", AccessToken = "a", RefreshToken = "r", ExpiresAt = DateTime.UtcNow.AddHours(1), User = new SpotifyUser { Id = "id", DisplayName = "name" } });
        var fake = new FakeSpotifyMatchingService
        {
            OnMatchAlbumsAsync = (albums, token, ct) => Task.FromResult(new MatchedAlbumsResponse
            {
                Albums = new List<MatchedAlbum>
                {
                    new() { SourceAlbum = albums[0], Match = new SpotifyAlbumMatch { SpotifyId = "x", Name = "n", Artist = "a", Uri = "u", Tracks = new(), Confidence = 100, Method = MatchMethod.Exact } }
                }
            })
        };

        var request = new MatchAlbumsRequest { Albums = new List<NormalizedAlbum> { new() { Name = "a", Artist = "ar", Source = "s", SourceMetadata = new(), Tracks = new() } } };

        var result = await InvokeAsync(mi, request, fake, store, ctx, (object)CancellationToken.None);

        result.Should().BeOfType<Ok<MatchedAlbumsResponse>>();
        var ok = (Ok<MatchedAlbumsResponse>)result;
        ok.Value!.Albums.Should().HaveCount(1);
    }

    [Fact]
    public async Task MatchAlbumsAsync_ServiceThrowsHttpRequestException()
    {
        var mi = GetPrivate("MatchAlbumsAsync");
        var ctx = ContextWithSessionCookie();
        var store = new InMemorySessionStore();
        store.StoreSession(new AuthSession { SessionId = "sid", AccessToken = "a", RefreshToken = "r", ExpiresAt = DateTime.UtcNow.AddHours(1), User = new SpotifyUser { Id = "id", DisplayName = "name" } });
        var fake = new FakeSpotifyMatchingService
        {
            OnMatchAlbumsAsync = (albums, token, ct) => throw new HttpRequestException("Spotify API error")
        };

        var request = new MatchAlbumsRequest { Albums = new List<NormalizedAlbum> { new() { Name = "a", Artist = "ar", Source = "s", SourceMetadata = new(), Tracks = new() } } };

        var result = await InvokeAsync(mi, request, fake, store, ctx, (object)CancellationToken.None);

        result.Should().BeOfType<ProblemHttpResult>();
        var problem = (ProblemHttpResult)result;
        problem.StatusCode.Should().Be(StatusCodes.Status503ServiceUnavailable);
    }

    [Fact]
    public async Task MatchAlbumsAsync_ServiceThrowsGenericException()
    {
        var mi = GetPrivate("MatchAlbumsAsync");
        var ctx = ContextWithSessionCookie();
        var store = new InMemorySessionStore();
        store.StoreSession(new AuthSession { SessionId = "sid", AccessToken = "a", RefreshToken = "r", ExpiresAt = DateTime.UtcNow.AddHours(1), User = new SpotifyUser { Id = "id", DisplayName = "name" } });
        var fake = new FakeSpotifyMatchingService
        {
            OnMatchAlbumsAsync = (albums, token, ct) => throw new Exception("Unexpected error")
        };

        var request = new MatchAlbumsRequest { Albums = new List<NormalizedAlbum> { new() { Name = "a", Artist = "ar", Source = "s", SourceMetadata = new(), Tracks = new() } } };

        var result = await InvokeAsync(mi, request, fake, store, ctx, (object)CancellationToken.None);

        result.Should().BeOfType<ProblemHttpResult>();
        var problem = (ProblemHttpResult)result;
        problem.StatusCode.Should().Be(StatusCodes.Status500InternalServerError);
    }

    [Fact]
    public async Task SearchAlbumsAsync_InvalidQuery_ReturnsBadRequest()
    {
        var mi = GetPrivate("SearchAlbumsAsync");
        var ctx = ContextWithSessionCookie();
        var result = await InvokeAsync(mi,
            (string?)"",
            new FakeSpotifyMatchingService(),
            new InMemorySessionStore(),
            ctx,
            (object)CancellationToken.None);

        result.Should().BeOfType<BadRequest<ApiError>>();
        ((BadRequest<ApiError>)result).Value!.Code.Should().Be("INVALID_QUERY");
    }

    [Fact]
    public async Task SearchAlbumsAsync_MissingSession_ReturnsUnauthorized()
    {
        var mi = GetPrivate("SearchAlbumsAsync");
        var ctx = new DefaultHttpContext();
        var result = await InvokeAsync(mi,
            (string?)"query",
            new FakeSpotifyMatchingService(),
            new InMemorySessionStore(),
            ctx,
            (object)CancellationToken.None);

        result.Should().BeOfType<UnauthorizedHttpResult>();
    }

    [Fact]
    public async Task SearchAlbumsAsync_Success_ReturnsOkWithResults()
    {
        var mi = GetPrivate("SearchAlbumsAsync");
        var ctx = ContextWithSessionCookie();
        var store = new InMemorySessionStore();
        store.StoreSession(new AuthSession { SessionId = "sid", AccessToken = "a", RefreshToken = "r", ExpiresAt = DateTime.UtcNow.AddHours(1), User = new SpotifyUser { Id = "id", DisplayName = "name" } });
        var fake = new FakeSpotifyMatchingService
        {
            OnSearchAlbumsAsync = (q, token, ct) => Task.FromResult<IReadOnlyList<SpotifyAlbumInfo>>(new List<SpotifyAlbumInfo>
            {
                new() { Id = "1", Name = "Album 1", Artist = "Artist 1", ReleaseDate = "2020-01-01", Uri = "spotify:album:1", TotalTracks = 10 },
                new() { Id = "2", Name = "Album 2", Artist = "Artist 2", ReleaseDate = "2021-01-01", Uri = "spotify:album:2", TotalTracks = 12 }
            })
        };

        var result = await InvokeAsync(mi,
            (string?)"query",
            fake,
            store,
            ctx,
            (object)CancellationToken.None);

        result.Should().BeOfType<Ok<IReadOnlyList<SpotifyAlbumInfo>>>();
        var ok = (Ok<IReadOnlyList<SpotifyAlbumInfo>>)result;
        ok.Value!.Should().HaveCount(2);
        ok.Value![0].Name.Should().Be("Album 1");
    }

    [Fact]
    public async Task SearchAlbumsAsync_ServiceThrowsHttpRequestException()
    {
        var mi = GetPrivate("SearchAlbumsAsync");
        var ctx = ContextWithSessionCookie();
        var store = new InMemorySessionStore();
        store.StoreSession(new AuthSession { SessionId = "sid", AccessToken = "a", RefreshToken = "r", ExpiresAt = DateTime.UtcNow.AddHours(1), User = new SpotifyUser { Id = "id", DisplayName = "name" } });
        var fake = new FakeSpotifyMatchingService
        {
            OnSearchAlbumsAsync = (q, token, ct) => throw new HttpRequestException("Spotify unavailable")
        };

        var result = await InvokeAsync(mi,
            (string?)"query",
            fake,
            store,
            ctx,
            (object)CancellationToken.None);

        result.Should().BeOfType<ProblemHttpResult>();
        var problem = (ProblemHttpResult)result;
        problem.StatusCode.Should().Be(StatusCodes.Status503ServiceUnavailable);
    }

    [Fact]
    public async Task MatchArtistsAsync_InvalidRequest_ReturnsBadRequest()
    {
        var mi = GetPrivate("MatchArtistsAsync");
        var ctx = ContextWithSessionCookie();
        var store = new InMemorySessionStore();
        store.StoreSession(new AuthSession { SessionId = "sid", AccessToken = "a", RefreshToken = "r", ExpiresAt = DateTime.UtcNow.AddHours(1), User = new SpotifyUser { Id = "id", DisplayName = "name" } });

        var result = await InvokeAsync(mi,
            new MatchArtistsRequest { Artists = new List<NormalizedArtist>() },
            new FakeSpotifyMatchingService(),
            store,
            ctx,
            (object)CancellationToken.None);

        result.Should().BeOfType<BadRequest<ApiError>>();
        ((BadRequest<ApiError>)result).Value!.Code.Should().Be("INVALID_REQUEST");
    }

    [Fact]
    public async Task MatchArtistsAsync_MissingSession_ReturnsUnauthorized()
    {
        var mi = GetPrivate("MatchArtistsAsync");
        var ctx = new DefaultHttpContext();
        var result = await InvokeAsync(mi,
            new MatchArtistsRequest { Artists = new List<NormalizedArtist> { new() { Name = "a", Source = "s", SourceMetadata = new() } } },
            new FakeSpotifyMatchingService(),
            new InMemorySessionStore(),
            ctx,
            (object)CancellationToken.None);

        result.Should().BeOfType<UnauthorizedHttpResult>();
    }

    [Fact]
    public async Task MatchArtistsAsync_Success_ReturnsOk()
    {
        var mi = GetPrivate("MatchArtistsAsync");
        var ctx = ContextWithSessionCookie();
        var store = new InMemorySessionStore();
        store.StoreSession(new AuthSession { SessionId = "sid", AccessToken = "a", RefreshToken = "r", ExpiresAt = DateTime.UtcNow.AddHours(1), User = new SpotifyUser { Id = "id", DisplayName = "name" } });
        var fake = new FakeSpotifyMatchingService
        {
            OnMatchArtistsAsync = (artists, token, ct) => Task.FromResult(new MatchedArtistsResponse
            {
                Artists = new List<MatchedArtist>
                {
                    new() { SourceArtist = artists[0], Match = new SpotifyArtistMatch { SpotifyId = "x", Name = "n", Uri = "u", TopTracks = new(), Confidence = 100, Method = MatchMethod.Exact } }
                }
            })
        };

        var request = new MatchArtistsRequest { Artists = new List<NormalizedArtist> { new() { Name = "a", Source = "s", SourceMetadata = new() } } };

        var result = await InvokeAsync(mi, request, fake, store, ctx, (object)CancellationToken.None);

        result.Should().BeOfType<Ok<MatchedArtistsResponse>>();
        var ok = (Ok<MatchedArtistsResponse>)result;
        ok.Value!.Artists.Should().HaveCount(1);
    }

    [Fact]
    public async Task MatchArtistsAsync_ServiceThrowsHttpRequestException()
    {
        var mi = GetPrivate("MatchArtistsAsync");
        var ctx = ContextWithSessionCookie();
        var store = new InMemorySessionStore();
        store.StoreSession(new AuthSession { SessionId = "sid", AccessToken = "a", RefreshToken = "r", ExpiresAt = DateTime.UtcNow.AddHours(1), User = new SpotifyUser { Id = "id", DisplayName = "name" } });
        var fake = new FakeSpotifyMatchingService
        {
            OnMatchArtistsAsync = (artists, token, ct) => throw new HttpRequestException("Spotify API error")
        };

        var request = new MatchArtistsRequest { Artists = new List<NormalizedArtist> { new() { Name = "a", Source = "s", SourceMetadata = new() } } };

        var result = await InvokeAsync(mi, request, fake, store, ctx, (object)CancellationToken.None);

        result.Should().BeOfType<ProblemHttpResult>();
        var problem = (ProblemHttpResult)result;
        problem.StatusCode.Should().Be(StatusCodes.Status503ServiceUnavailable);
    }

    [Fact]
    public async Task MatchArtistsAsync_ServiceThrowsGenericException()
    {
        var mi = GetPrivate("MatchArtistsAsync");
        var ctx = ContextWithSessionCookie();
        var store = new InMemorySessionStore();
        store.StoreSession(new AuthSession { SessionId = "sid", AccessToken = "a", RefreshToken = "r", ExpiresAt = DateTime.UtcNow.AddHours(1), User = new SpotifyUser { Id = "id", DisplayName = "name" } });
        var fake = new FakeSpotifyMatchingService
        {
            OnMatchArtistsAsync = (artists, token, ct) => throw new Exception("Unexpected error")
        };

        var request = new MatchArtistsRequest { Artists = new List<NormalizedArtist> { new() { Name = "a", Source = "s", SourceMetadata = new() } } };

        var result = await InvokeAsync(mi, request, fake, store, ctx, (object)CancellationToken.None);

        result.Should().BeOfType<ProblemHttpResult>();
        var problem = (ProblemHttpResult)result;
        problem.StatusCode.Should().Be(StatusCodes.Status500InternalServerError);
    }

    [Fact]
    public async Task SearchArtistsAsync_InvalidQuery_ReturnsBadRequest()
    {
        var mi = GetPrivate("SearchArtistsAsync");
        var ctx = ContextWithSessionCookie();
        var result = await InvokeAsync(mi,
            (string?)"",
            new FakeSpotifyMatchingService(),
            new InMemorySessionStore(),
            ctx,
            (object)CancellationToken.None);

        result.Should().BeOfType<BadRequest<ApiError>>();
        ((BadRequest<ApiError>)result).Value!.Code.Should().Be("INVALID_QUERY");
    }

    [Fact]
    public async Task SearchArtistsAsync_MissingSession_ReturnsUnauthorized()
    {
        var mi = GetPrivate("SearchArtistsAsync");
        var ctx = new DefaultHttpContext();
        var result = await InvokeAsync(mi,
            (string?)"query",
            new FakeSpotifyMatchingService(),
            new InMemorySessionStore(),
            ctx,
            (object)CancellationToken.None);

        result.Should().BeOfType<UnauthorizedHttpResult>();
    }

    [Fact]
    public async Task SearchArtistsAsync_Success_ReturnsOkWithResults()
    {
        var mi = GetPrivate("SearchArtistsAsync");
        var ctx = ContextWithSessionCookie();
        var store = new InMemorySessionStore();
        store.StoreSession(new AuthSession { SessionId = "sid", AccessToken = "a", RefreshToken = "r", ExpiresAt = DateTime.UtcNow.AddHours(1), User = new SpotifyUser { Id = "id", DisplayName = "name" } });
        var fake = new FakeSpotifyMatchingService
        {
            OnSearchArtistsAsync = (q, token, ct) => Task.FromResult<IReadOnlyList<SpotifyArtistInfo>>(new List<SpotifyArtistInfo>
            {
                new() { Id = "1", Name = "Artist 1", Uri = "spotify:artist:1", Genres = new List<string> { "rock" } },
                new() { Id = "2", Name = "Artist 2", Uri = "spotify:artist:2", Genres = new List<string> { "pop" } }
            })
        };

        var result = await InvokeAsync(mi,
            (string?)"query",
            fake,
            store,
            ctx,
            (object)CancellationToken.None);

        result.Should().BeOfType<Ok<IReadOnlyList<SpotifyArtistInfo>>>();
        var ok = (Ok<IReadOnlyList<SpotifyArtistInfo>>)result;
        ok.Value!.Should().HaveCount(2);
        ok.Value![0].Name.Should().Be("Artist 1");
    }

    [Fact]
    public async Task SearchArtistsAsync_ServiceThrowsHttpRequestException()
    {
        var mi = GetPrivate("SearchArtistsAsync");
        var ctx = ContextWithSessionCookie();
        var store = new InMemorySessionStore();
        store.StoreSession(new AuthSession { SessionId = "sid", AccessToken = "a", RefreshToken = "r", ExpiresAt = DateTime.UtcNow.AddHours(1), User = new SpotifyUser { Id = "id", DisplayName = "name" } });
        var fake = new FakeSpotifyMatchingService
        {
            OnSearchArtistsAsync = (q, token, ct) => throw new HttpRequestException("Spotify unavailable")
        };

        var result = await InvokeAsync(mi,
            (string?)"query",
            fake,
            store,
            ctx,
            (object)CancellationToken.None);

        result.Should().BeOfType<ProblemHttpResult>();
        var problem = (ProblemHttpResult)result;
        problem.StatusCode.Should().Be(StatusCodes.Status503ServiceUnavailable);
    }

    [Fact]
    public async Task SearchArtistsAsync_ServiceThrowsGenericException()
    {
        var mi = GetPrivate("SearchArtistsAsync");
        var ctx = ContextWithSessionCookie();
        var store = new InMemorySessionStore();
        store.StoreSession(new AuthSession { SessionId = "sid", AccessToken = "a", RefreshToken = "r", ExpiresAt = DateTime.UtcNow.AddHours(1), User = new SpotifyUser { Id = "id", DisplayName = "name" } });
        var fake = new FakeSpotifyMatchingService
        {
            OnSearchArtistsAsync = (q, token, ct) => throw new Exception("Unexpected error")
        };

        var result = await InvokeAsync(mi,
            (string?)"query",
            fake,
            store,
            ctx,
            (object)CancellationToken.None);

        result.Should().BeOfType<ProblemHttpResult>();
        var problem = (ProblemHttpResult)result;
        problem.StatusCode.Should().Be(StatusCodes.Status500InternalServerError);
    }

    [Fact]
    public async Task SearchAlbumsAsync_EmptyResults()
    {
        var mi = GetPrivate("SearchAlbumsAsync");
        var ctx = ContextWithSessionCookie();
        var store = new InMemorySessionStore();
        store.StoreSession(new AuthSession { SessionId = "sid", AccessToken = "a", RefreshToken = "r", ExpiresAt = DateTime.UtcNow.AddHours(1), User = new SpotifyUser { Id = "id", DisplayName = "name" } });
        var fake = new FakeSpotifyMatchingService
        {
            OnSearchAlbumsAsync = (q, token, ct) => Task.FromResult<IReadOnlyList<SpotifyAlbumInfo>>(new List<SpotifyAlbumInfo>())
        };

        var result = await InvokeAsync(mi,
            (string?)"nonexistent",
            fake,
            store,
            ctx,
            (object)CancellationToken.None);

        result.Should().BeOfType<Ok<IReadOnlyList<SpotifyAlbumInfo>>>();
        var ok = (Ok<IReadOnlyList<SpotifyAlbumInfo>>)result;
        ok.Value!.Should().HaveCount(0);
    }

    [Fact]
    public async Task SearchArtistsAsync_EmptyResults()
    {
        var mi = GetPrivate("SearchArtistsAsync");
        var ctx = ContextWithSessionCookie();
        var store = new InMemorySessionStore();
        store.StoreSession(new AuthSession { SessionId = "sid", AccessToken = "a", RefreshToken = "r", ExpiresAt = DateTime.UtcNow.AddHours(1), User = new SpotifyUser { Id = "id", DisplayName = "name" } });
        var fake = new FakeSpotifyMatchingService
        {
            OnSearchArtistsAsync = (q, token, ct) => Task.FromResult<IReadOnlyList<SpotifyArtistInfo>>(new List<SpotifyArtistInfo>())
        };

        var result = await InvokeAsync(mi,
            (string?)"nonexistent",
            fake,
            store,
            ctx,
            (object)CancellationToken.None);

        result.Should().BeOfType<Ok<IReadOnlyList<SpotifyArtistInfo>>>();
        var ok = (Ok<IReadOnlyList<SpotifyArtistInfo>>)result;
        ok.Value!.Should().HaveCount(0);
    }
}
