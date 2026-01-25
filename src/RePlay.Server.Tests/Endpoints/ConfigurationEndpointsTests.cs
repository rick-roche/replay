using System.Reflection;
using FluentAssertions;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Http.HttpResults;
using RePlay.Server.Endpoints;
using RePlay.Server.Models;
using RePlay.Server.Services;

namespace RePlay.Server.Tests.Endpoints;

public class ConfigurationEndpointsTests
{
    private static MethodInfo GetPrivate(string name)
    {
        var mi = typeof(ConfigurationEndpoints).GetMethod(name, BindingFlags.NonPublic | BindingFlags.Static);
        mi.Should().NotBeNull();
        return mi!;
    }

    private static async Task<IResult> InvokeAsync(MethodInfo mi, params object[] args)
    {
        var task = (Task<IResult>)mi.Invoke(null, args)!;
        return await task.ConfigureAwait(false);
    }

    private sealed class FakeLastfmService : ILastfmService
    {
        public Func<string, CancellationToken, Task<LastfmUser?>>? OnGetUserAsync { get; set; }
        public Func<string, LastfmFilter, CancellationToken, Task<LastfmDataResponse?>>? OnGetUserDataAsync { get; set; }
        public Func<string, LastfmFilter, CancellationToken, Task<NormalizedDataResponse?>>? OnGetUserDataNormalizedAsync { get; set; }

        public Task<LastfmUser?> GetUserAsync(string username, CancellationToken cancellationToken = default)
            => OnGetUserAsync?.Invoke(username, cancellationToken) ?? Task.FromResult<LastfmUser?>(null);

        public Task<LastfmDataResponse?> GetUserDataAsync(string username, LastfmFilter filter, CancellationToken cancellationToken = default)
            => OnGetUserDataAsync?.Invoke(username, filter, cancellationToken) ?? Task.FromResult<LastfmDataResponse?>(null);

        public Task<NormalizedDataResponse?> GetUserDataNormalizedAsync(string username, LastfmFilter filter, CancellationToken cancellationToken = default)
            => OnGetUserDataNormalizedAsync?.Invoke(username, filter, cancellationToken) ?? Task.FromResult<NormalizedDataResponse?>(null);
    }

    private sealed class FakeDiscogsService : IDiscogsService
    {
        public Func<string, CancellationToken, Task<DiscogsProfile?>>? OnGetProfileAsync { get; set; }

        public Task<DiscogsProfile?> GetProfileAsync(string usernameOrCollectionId, CancellationToken cancellationToken = default)
            => OnGetProfileAsync?.Invoke(usernameOrCollectionId, cancellationToken) ?? Task.FromResult<DiscogsProfile?>(null);
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
    public async Task PostConfigureLastfm_ValidatesMissingUsername()
    {
        var mi = GetPrivate("PostConfigureLastfm");
        var ctx = ContextWithSessionCookie();
        var result = await InvokeAsync(mi,
            new ConfigureLastfmRequest { Username = "" },
            new FakeLastfmService(),
            new InMemorySessionStore(),
            ctx,
            CancellationToken.None);

        result.Should().BeOfType<BadRequest<ApiError>>();
        ((BadRequest<ApiError>)result).Value!.Code.Should().Be("MISSING_USERNAME");
    }

    [Fact]
    public async Task PostConfigureLastfm_ValidatesMissingSession()
    {
        var mi = GetPrivate("PostConfigureLastfm");
        var ctx = new DefaultHttpContext();
        var result = await InvokeAsync(mi,
            new ConfigureLastfmRequest { Username = "user" },
            new FakeLastfmService(),
            new InMemorySessionStore(),
            ctx,
            CancellationToken.None);

        result.Should().BeOfType<JsonHttpResult<ApiError>>();
        var json = (JsonHttpResult<ApiError>)result;
        json.StatusCode.Should().Be(401);
        json.Value!.Code.Should().Be("NO_SESSION");
    }

    [Fact]
    public async Task PostConfigureLastfm_ReturnsInvalidUser_WhenServiceReturnsNull()
    {
        var mi = GetPrivate("PostConfigureLastfm");
        var ctx = ContextWithSessionCookie();
        var store = new InMemorySessionStore();
        store.StoreSession(new AuthSession
        {
            SessionId = "sid",
            AccessToken = "a",
            RefreshToken = "r",
            ExpiresAt = DateTime.UtcNow.AddHours(1),
            User = new SpotifyUser { Id = "id", DisplayName = "name" }
        });
        var fake = new FakeLastfmService
        {
            OnGetUserAsync = (u, ct) => Task.FromResult<LastfmUser?>(null)
        };
        var result = await InvokeAsync(mi,
            new ConfigureLastfmRequest { Username = "user" },
            fake,
            store,
            ctx,
            CancellationToken.None);

        result.Should().BeOfType<BadRequest<ApiError>>();
        ((BadRequest<ApiError>)result).Value!.Code.Should().Be("INVALID_LASTFM_USER");
    }

    [Fact]
    public async Task PostConfigureLastfm_Success_ReturnsOk()
    {
        var mi = GetPrivate("PostConfigureLastfm");
        var ctx = ContextWithSessionCookie();
        var store = new InMemorySessionStore();
        store.StoreSession(new AuthSession
        {
            SessionId = "sid",
            AccessToken = "a",
            RefreshToken = "r",
            ExpiresAt = DateTime.UtcNow.AddHours(1),
            User = new SpotifyUser { Id = "id", DisplayName = "name" }
        });
        var fake = new FakeLastfmService
        {
            OnGetUserAsync = (u, ct) => Task.FromResult<LastfmUser?>(new LastfmUser { Username = u, PlayCount = 1, Registered = "", ProfileUrl = "https://www.last.fm/user/" + u })
        };

        var result = await InvokeAsync(mi,
            new ConfigureLastfmRequest { Username = "user" },
            fake,
            store,
            ctx,
            CancellationToken.None);

        result.Should().BeOfType<Ok<ConfigureLastfmResponse>>();
        var ok = (Ok<ConfigureLastfmResponse>)result;
        ok.Value!.IsConfigured.Should().BeTrue();
        ok.Value!.Username.Should().Be("user");
    }

    [Fact]
    public async Task PostConfigureDiscogs_ValidatesMissingIdentifier()
    {
        var mi = GetPrivate("PostConfigureDiscogs");
        var ctx = ContextWithSessionCookie();
        var store = new InMemorySessionStore();
        store.StoreSession(new AuthSession
        {
            SessionId = "sid",
            AccessToken = "a",
            RefreshToken = "r",
            ExpiresAt = DateTime.UtcNow.AddHours(1),
            User = new SpotifyUser { Id = "id", DisplayName = "name" }
        });

        var result = await InvokeAsync(mi,
            new ConfigureDiscogsRequest { UsernameOrCollectionId = "" },
            new FakeDiscogsService(),
            store,
            ctx,
            CancellationToken.None);

        result.Should().BeOfType<BadRequest<ApiError>>();
        ((BadRequest<ApiError>)result).Value!.Code.Should().Be("MISSING_USERNAME_OR_COLLECTION");
    }

    [Fact]
    public async Task PostConfigureDiscogs_ReturnsUnauthorized_WhenSessionMissing()
    {
        var mi = GetPrivate("PostConfigureDiscogs");

        var result = await InvokeAsync(mi,
            new ConfigureDiscogsRequest { UsernameOrCollectionId = "user" },
            new FakeDiscogsService(),
            new InMemorySessionStore(),
            new DefaultHttpContext(),
            CancellationToken.None);

        result.Should().BeOfType<JsonHttpResult<ApiError>>();
        ((JsonHttpResult<ApiError>)result).StatusCode.Should().Be(401);
    }

    [Fact]
    public async Task PostConfigureDiscogs_ReturnsBadRequest_WhenProfileNotFound()
    {
        var mi = GetPrivate("PostConfigureDiscogs");
        var ctx = ContextWithSessionCookie();
        var store = new InMemorySessionStore();
        store.StoreSession(new AuthSession
        {
            SessionId = "sid",
            AccessToken = "a",
            RefreshToken = "r",
            ExpiresAt = DateTime.UtcNow.AddHours(1),
            User = new SpotifyUser { Id = "id", DisplayName = "name" }
        });

        var fake = new FakeDiscogsService
        {
            OnGetProfileAsync = (_, _) => Task.FromResult<DiscogsProfile?>(null)
        };

        var result = await InvokeAsync(mi,
            new ConfigureDiscogsRequest { UsernameOrCollectionId = "missing" },
            fake,
            store,
            ctx,
            CancellationToken.None);

        result.Should().BeOfType<BadRequest<ApiError>>();
        ((BadRequest<ApiError>)result).Value!.Code.Should().Be("INVALID_DISCOGS_PROFILE");
    }

    [Fact]
    public async Task PostConfigureDiscogs_ReturnsOk_WhenProfileFound()
    {
        var mi = GetPrivate("PostConfigureDiscogs");
        var ctx = ContextWithSessionCookie();
        var store = new InMemorySessionStore();
        store.StoreSession(new AuthSession
        {
            SessionId = "sid",
            AccessToken = "a",
            RefreshToken = "r",
            ExpiresAt = DateTime.UtcNow.AddHours(1),
            User = new SpotifyUser { Id = "id", DisplayName = "name" }
        });

        var fake = new FakeDiscogsService
        {
            OnGetProfileAsync = (identifier, _) => Task.FromResult<DiscogsProfile?>(new DiscogsProfile
            {
                Username = identifier,
                CollectionUrl = "https://discogs.com/users/test/collection",
                ReleaseCount = 5
            })
        };

        var result = await InvokeAsync(mi,
            new ConfigureDiscogsRequest { UsernameOrCollectionId = "collector" },
            fake,
            store,
            ctx,
            CancellationToken.None);

        result.Should().BeOfType<Ok<ConfigureDiscogsResponse>>();
        var ok = (Ok<ConfigureDiscogsResponse>)result;
        ok.Value!.IsConfigured.Should().BeTrue();
        ok.Value!.Username.Should().Be("collector");

        var stored = store.GetSourceConfig("sid", "discogs");
        stored.Should().NotBeNull();
        stored!.ConfigValue.Should().Be("collector");
    }
}
