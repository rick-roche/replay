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

    private static async Task<IResult> InvokeAsync(MethodInfo mi, params object[] args)
    {
        var task = (Task<IResult>)mi.Invoke(null, args)!;
        return await task.ConfigureAwait(false);
    }

    private sealed class FakeSpotifyMatchingService : ISpotifyMatchingService
    {
        public Func<IReadOnlyList<NormalizedTrack>, string, CancellationToken, Task<MatchedDataResponse>>? OnMatchTracksAsync { get; set; }
        public Func<string, string, CancellationToken, Task<IReadOnlyList<SpotifyTrack>>>? OnSearchTracksAsync { get; set; }

        public Task<MatchedDataResponse> MatchTracksAsync(IReadOnlyList<NormalizedTrack> tracks, string accessToken, CancellationToken cancellationToken = default)
            => OnMatchTracksAsync?.Invoke(tracks, accessToken, cancellationToken) ?? Task.FromResult(new MatchedDataResponse { Tracks = new List<MatchedTrack>() });

        public Task<IReadOnlyList<SpotifyTrack>> SearchTracksAsync(string query, string accessToken, CancellationToken cancellationToken = default)
            => OnSearchTracksAsync?.Invoke(query, accessToken, cancellationToken) ?? Task.FromResult<IReadOnlyList<SpotifyTrack>>(Array.Empty<SpotifyTrack>());
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
            CancellationToken.None);

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
            CancellationToken.None);

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
            CancellationToken.None);

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
            CancellationToken.None);

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
            CancellationToken.None);

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
            CancellationToken.None);

        result.Should().BeOfType<Ok<IReadOnlyList<SpotifyTrack>>>();
        var ok = (Ok<IReadOnlyList<SpotifyTrack>>)result;
        ok.Value!.Should().HaveCount(2);
        ok.Value![0].Name.Should().Be("Track 1");
    }
}
