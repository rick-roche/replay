using System.Reflection;
using FluentAssertions;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Http.HttpResults;
using RePlay.Server.Endpoints;
using RePlay.Server.Models;
using RePlay.Server.Services;
using SetlistRequest = RePlay.Server.Endpoints.ConfigureSetlistRequest;
using SetlistResponse = RePlay.Server.Endpoints.ConfigureSetlistResponse;

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
        public Func<string, DiscogsFilter, CancellationToken, Task<DiscogsDataResponse?>>? OnGetCollectionAsync { get; set; }
        public Func<string, DiscogsFilter, CancellationToken, Task<NormalizedDataResponse?>>? OnGetCollectionNormalizedAsync { get; set; }

        public Task<DiscogsProfile?> GetProfileAsync(string usernameOrCollectionId, CancellationToken cancellationToken = default)
            => OnGetProfileAsync?.Invoke(usernameOrCollectionId, cancellationToken) ?? Task.FromResult<DiscogsProfile?>(null);

        public Task<DiscogsDataResponse?> GetCollectionAsync(string usernameOrCollectionId, DiscogsFilter filter, CancellationToken cancellationToken = default)
            => OnGetCollectionAsync?.Invoke(usernameOrCollectionId, filter, cancellationToken) ?? Task.FromResult<DiscogsDataResponse?>(null);

        public Task<NormalizedDataResponse?> GetCollectionNormalizedAsync(string usernameOrCollectionId, DiscogsFilter filter, CancellationToken cancellationToken = default)
            => OnGetCollectionNormalizedAsync?.Invoke(usernameOrCollectionId, filter, cancellationToken) ?? Task.FromResult<NormalizedDataResponse?>(null);
    }

    private sealed class FakeSetlistFmService : ISetlistFmService
    {
        public Func<string, CancellationToken, Task<SetlistUser?>>? OnGetUserAsync { get; set; }
        public Func<string, SetlistFmFilter, CancellationToken, Task<SetlistFmDataResponse>>? OnGetUserConcertsAsync { get; set; }
        public Func<string, SetlistFmFilter, CancellationToken, Task<NormalizedDataResponse>>? OnGetUserConcertsNormalizedAsync { get; set; }

        public Task<SetlistUser?> GetUserAsync(string usernameOrId, CancellationToken cancellationToken = default)
            => OnGetUserAsync?.Invoke(usernameOrId, cancellationToken) ?? Task.FromResult<SetlistUser?>(null);

        public Task<SetlistFmDataResponse> GetUserConcertsAsync(string userId, SetlistFmFilter filter, CancellationToken cancellationToken = default)
            => OnGetUserConcertsAsync?.Invoke(userId, filter, cancellationToken) ?? Task.FromResult(new SetlistFmDataResponse { Concerts = [], Tracks = [], TotalConcerts = 0, TotalTracks = 0 });

        public Task<NormalizedDataResponse> GetUserConcertsNormalizedAsync(string userId, SetlistFmFilter filter, CancellationToken cancellationToken = default)
            => OnGetUserConcertsNormalizedAsync?.Invoke(userId, filter, cancellationToken) ?? Task.FromResult(new NormalizedDataResponse { DataType = "Concerts", Tracks = [], Albums = [], Artists = [], TotalResults = 0, Source = "setlistfm" });
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

    [Fact]
    public async Task PostConfigureSetlistFm_ValidatesMissingUsernameOrId()
    {
        var mi = GetPrivate("PostConfigureSetlistFm");
        var ctx = ContextWithSessionCookie();
        var result = await InvokeAsync(mi,
            new SetlistRequest { UsernameOrId = "" },
            new FakeSetlistFmService(),
            new InMemorySessionStore(),
            ctx,
            CancellationToken.None);

        result.Should().BeOfType<BadRequest<ApiError>>();
        ((BadRequest<ApiError>)result).Value!.Code.Should().Be("MISSING_USERNAME");
    }

    [Fact]
    public async Task PostConfigureSetlistFm_ValidatesMissingSession()
    {
        var mi = GetPrivate("PostConfigureSetlistFm");
        var ctx = new DefaultHttpContext();
        var result = await InvokeAsync(mi,
            new SetlistRequest { UsernameOrId = "user123" },
            new FakeSetlistFmService(),
            new InMemorySessionStore(),
            ctx,
            CancellationToken.None);

        result.Should().BeOfType<JsonHttpResult<ApiError>>();
        var json = (JsonHttpResult<ApiError>)result;
        json.StatusCode.Should().Be(401);
        json.Value!.Code.Should().Be("NO_SESSION");
    }

    [Fact]
    public async Task PostConfigureSetlistFm_ReturnsInvalidUser_WhenServiceReturnsNull()
    {
        var mi = GetPrivate("PostConfigureSetlistFm");
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
        var fake = new FakeSetlistFmService
        {
            OnGetUserAsync = (u, ct) => Task.FromResult<SetlistUser?>(null)
        };
        var result = await InvokeAsync(mi,
            new SetlistRequest { UsernameOrId = "user123" },
            fake,
            store,
            ctx,
            CancellationToken.None);

        result.Should().BeOfType<BadRequest<ApiError>>();
        ((BadRequest<ApiError>)result).Value!.Code.Should().Be("INVALID_SETLIST_USER");
    }

    [Fact]
    public async Task PostConfigureSetlistFm_Success_ReturnsOk()
    {
        var mi = GetPrivate("PostConfigureSetlistFm");
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
        var fake = new FakeSetlistFmService
        {
            OnGetUserAsync = (u, ct) => Task.FromResult<SetlistUser?>(new SetlistUser
            {
                UserId = "user123",
                DisplayName = "Test User",
                Url = "https://setlist.fm/user/user123",
                AttendedConcerts = 42
            })
        };

        var result = await InvokeAsync(mi,
            new SetlistRequest { UsernameOrId = "user123" },
            fake,
            store,
            ctx,
            CancellationToken.None);

        result.Should().BeOfType<Ok<SetlistResponse>>();
        var ok = (Ok<SetlistResponse>)result;
        ok.Value!.IsConfigured.Should().BeTrue();
        ok.Value!.UserId.Should().Be("user123");
        ok.Value!.DisplayName.Should().Be("Test User");
        ok.Value!.AttendedConcerts.Should().Be(42);

        var stored = store.GetSourceConfig("sid", "setlistfm");
        stored.Should().NotBeNull();
        stored!.ConfigValue.Should().Be("user123");
    }

    [Fact]
    public async Task PostConfigureSetlistFm_ServiceThrowsException_ReturnsInternalServerError()
    {
        var mi = GetPrivate("PostConfigureSetlistFm");
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
        var fake = new FakeSetlistFmService
        {
            OnGetUserAsync = (u, ct) => throw new HttpRequestException("API error")
        };

        var result = await InvokeAsync(mi,
            new SetlistRequest { UsernameOrId = "user123" },
            fake,
            store,
            ctx,
            CancellationToken.None);

        result.Should().BeOfType<JsonHttpResult<ApiError>>();
        var json = (JsonHttpResult<ApiError>)result;
        json.StatusCode.Should().Be(500);
        json.Value!.Code.Should().Be("SETLIST_CONFIG_ERROR");
    }

    [Fact]
    public async Task PostConfigureLastfm_ServiceThrowsException_ReturnsInternalServerError()
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
            OnGetUserAsync = (u, ct) => throw new HttpRequestException("API error")
        };

        var result = await InvokeAsync(mi,
            new ConfigureLastfmRequest { Username = "user" },
            fake,
            store,
            ctx,
            CancellationToken.None);

        result.Should().BeOfType<JsonHttpResult<ApiError>>();
        var json = (JsonHttpResult<ApiError>)result;
        json.StatusCode.Should().Be(500);
        json.Value!.Code.Should().Be("LASTFM_CONFIG_ERROR");
    }

    [Fact]
    public async Task PostConfigureDiscogs_ServiceThrowsException_ReturnsInternalServerError()
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
            OnGetProfileAsync = (id, ct) => throw new HttpRequestException("API error")
        };

        var result = await InvokeAsync(mi,
            new ConfigureDiscogsRequest { UsernameOrCollectionId = "user" },
            fake,
            store,
            ctx,
            CancellationToken.None);

        result.Should().BeOfType<JsonHttpResult<ApiError>>();
        var json = (JsonHttpResult<ApiError>)result;
        json.StatusCode.Should().Be(500);
        json.Value!.Code.Should().Be("DISCOGS_CONFIG_ERROR");
    }

    [Fact]
    public async Task PostConfigureLastfm_ValidatesInvalidSessionId()
    {
        var mi = GetPrivate("PostConfigureLastfm");
        var ctx = ContextWithSessionCookie("invalid_session_id");
        var store = new InMemorySessionStore();
        
        var result = await InvokeAsync(mi,
            new ConfigureLastfmRequest { Username = "user" },
            new FakeLastfmService(),
            store,
            ctx,
            CancellationToken.None);

        result.Should().BeOfType<JsonHttpResult<ApiError>>();
        ((JsonHttpResult<ApiError>)result).StatusCode.Should().Be(401);
        ((JsonHttpResult<ApiError>)result).Value!.Code.Should().Be("INVALID_SESSION");
    }

    [Fact]
    public async Task PostConfigureDiscogs_ValidatesInvalidSessionId()
    {
        var mi = GetPrivate("PostConfigureDiscogs");
        var ctx = ContextWithSessionCookie("invalid_session_id");
        var store = new InMemorySessionStore();

        var result = await InvokeAsync(mi,
            new ConfigureDiscogsRequest { UsernameOrCollectionId = "user" },
            new FakeDiscogsService(),
            store,
            ctx,
            CancellationToken.None);

        result.Should().BeOfType<JsonHttpResult<ApiError>>();
        ((JsonHttpResult<ApiError>)result).StatusCode.Should().Be(401);
        ((JsonHttpResult<ApiError>)result).Value!.Code.Should().Be("INVALID_SESSION");
    }

    [Fact]
    public async Task PostConfigureSetlistFm_ValidatesInvalidSessionId()
    {
        var mi = GetPrivate("PostConfigureSetlistFm");
        var ctx = ContextWithSessionCookie("invalid_session_id");
        var store = new InMemorySessionStore();

        var result = await InvokeAsync(mi,
            new SetlistRequest { UsernameOrId = "user123" },
            new FakeSetlistFmService(),
            store,
            ctx,
            CancellationToken.None);

        result.Should().BeOfType<JsonHttpResult<ApiError>>();
        ((JsonHttpResult<ApiError>)result).StatusCode.Should().Be(401);
        ((JsonHttpResult<ApiError>)result).Value!.Code.Should().Be("INVALID_SESSION");
    }

    [Fact]
    public async Task PostConfigureLastfm_ValidatesEmptyUsername()
    {
        var mi = GetPrivate("PostConfigureLastfm");
        var ctx = ContextWithSessionCookie();

        var result = await InvokeAsync(mi,
            new ConfigureLastfmRequest { Username = "   " },
            new FakeLastfmService(),
            new InMemorySessionStore(),
            ctx,
            CancellationToken.None);

        result.Should().BeOfType<BadRequest<ApiError>>();
        ((BadRequest<ApiError>)result).Value!.Code.Should().Be("MISSING_USERNAME");
    }

    [Fact]
    public async Task PostConfigureDiscogs_ValidatesEmptyIdentifier()
    {
        var mi = GetPrivate("PostConfigureDiscogs");
        var ctx = ContextWithSessionCookie();

        var result = await InvokeAsync(mi,
            new ConfigureDiscogsRequest { UsernameOrCollectionId = "   " },
            new FakeDiscogsService(),
            new InMemorySessionStore(),
            ctx,
            CancellationToken.None);

        result.Should().BeOfType<BadRequest<ApiError>>();
        ((BadRequest<ApiError>)result).Value!.Code.Should().Be("MISSING_USERNAME_OR_COLLECTION");
    }

    [Fact]
    public async Task PostConfigureLastfm_UsernameWithSpecialCharacters()
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
            OnGetUserAsync = (u, ct) => Task.FromResult<LastfmUser?>(new LastfmUser { Username = u, PlayCount = 100, Registered = "", ProfileUrl = "https://www.last.fm/user/" + u })
        };

        var result = await InvokeAsync(mi,
            new ConfigureLastfmRequest { Username = "user@domain_123" },
            fake,
            store,
            ctx,
            CancellationToken.None);

        result.Should().BeOfType<Ok<ConfigureLastfmResponse>>();
        var ok = (Ok<ConfigureLastfmResponse>)result;
        ok.Value!.Username.Should().Be("user@domain_123");
    }

    [Fact]
    public async Task PostConfigureDiscogs_StoresConfigInSessionStore()
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
                ReleaseCount = 10
            })
        };

        var result = await InvokeAsync(mi,
            new ConfigureDiscogsRequest { UsernameOrCollectionId = "testuser" },
            fake,
            store,
            ctx,
            CancellationToken.None);

        result.Should().BeOfType<Ok<ConfigureDiscogsResponse>>();
        
        // Verify config was stored
        var stored = store.GetSourceConfig("sid", "discogs");
        stored.Should().NotBeNull();
        stored!.ConfigValue.Should().Be("testuser");
        stored!.Source.Should().Be("discogs");
        stored!.ConfiguredAt.Should().BeBefore(DateTime.UtcNow.AddSeconds(1));
    }

    [Fact]
    public async Task PostConfigureLastfm_StoresConfigInSessionStore()
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
            OnGetUserAsync = (u, ct) => Task.FromResult<LastfmUser?>(new LastfmUser { Username = u, PlayCount = 500, Registered = "", ProfileUrl = "https://www.last.fm/user/" + u })
        };

        var result = await InvokeAsync(mi,
            new ConfigureLastfmRequest { Username = "lastfmuser" },
            fake,
            store,
            ctx,
            CancellationToken.None);

        result.Should().BeOfType<Ok<ConfigureLastfmResponse>>();
        
        // Verify config was stored
        var stored = store.GetSourceConfig("sid", "lastfm");
        stored.Should().NotBeNull();
        stored!.ConfigValue.Should().Be("lastfmuser");
        stored!.Source.Should().Be("lastfm");
    }

    [Fact]
    public async Task PostConfigureSetlistFm_StoresConfigInSessionStore()
    {
        var mi = GetPrivate("PostConfigureSetlistFm");
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
        var fake = new FakeSetlistFmService
        {
            OnGetUserAsync = (u, ct) => Task.FromResult<SetlistUser?>(new SetlistUser
            {
                UserId = "setlist123",
                DisplayName = "Concert Fan",
                Url = "https://setlist.fm/user/setlist123",
                AttendedConcerts = 25
            })
        };

        var result = await InvokeAsync(mi,
            new SetlistRequest { UsernameOrId = "setlist123" },
            fake,
            store,
            ctx,
            CancellationToken.None);

        result.Should().BeOfType<Ok<SetlistResponse>>();
        
        // Verify config was stored
        var stored = store.GetSourceConfig("sid", "setlistfm");
        stored.Should().NotBeNull();
        stored!.ConfigValue.Should().Be("setlist123");
        stored!.Source.Should().Be("setlistfm");
    }
}
