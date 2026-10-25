using System.Reflection;
using System.Net;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http.Metadata;
using Microsoft.AspNetCore.Routing;
using Microsoft.Extensions.Options;
using RePlay.Server.Configuration;
using FluentAssertions;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Http.HttpResults;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using RePlay.Server.Endpoints;
using RePlay.Server.Models;
using RePlay.Server.Services;

namespace RePlay.Server.Tests.Endpoints;

public class AuthEndpointsTests
{
    private static MethodInfo GetPrivate(string name)
    {
        var mi = typeof(AuthEndpoints).GetMethod(name, BindingFlags.NonPublic | BindingFlags.Static);
        mi.Should().NotBeNull();
        return mi!;
    }

    private static async Task<IResult> InvokeAsync(MethodInfo mi, params object[]? args)
    {
        if (typeof(Task).IsAssignableFrom(mi.ReturnType))
        {
            var task = (Task<IResult>)mi.Invoke(null, args)!;
            return await task.ConfigureAwait(false);
        }
        return (IResult)mi.Invoke(null, args)!;
    }

    private sealed class FakeAuthService : ISpotifyAuthService
    {
        public Func<string, string?, string>? OnGetAuthorizationUrl { get; set; }
        public Func<string, string?, CancellationToken, Task<AuthSession>>? OnExchangeCodeAsync { get; set; }
        public Func<string, CancellationToken, Task<AuthSession>>? OnRefreshTokenAsync { get; set; }
        public Func<string, CancellationToken, Task<SpotifyUser>>? OnGetUserProfileAsync { get; set; }

        public string GetAuthorizationUrl(string state, string? redirectUri = null)
            => OnGetAuthorizationUrl?.Invoke(state, redirectUri) ?? "https://auth.example/authorize";

        public Task<AuthSession> ExchangeCodeAsync(string code, CancellationToken cancellationToken = default)
            => OnExchangeCodeAsync?.Invoke(code, null, cancellationToken) ?? Task.FromResult(new AuthSession
            {
                SessionId = "sid",
                AccessToken = "access",
                RefreshToken = "refresh",
                ExpiresAt = DateTime.UtcNow.AddHours(1),
                User = new SpotifyUser { Id = "id", DisplayName = "name" }
            });

        public Task<AuthSession> ExchangeCodeAsync(string code, string? redirectUri, CancellationToken cancellationToken = default)
            => OnExchangeCodeAsync?.Invoke(code, redirectUri, cancellationToken) ?? Task.FromResult(new AuthSession
            {
                SessionId = "sid",
                AccessToken = "access",
                RefreshToken = "refresh",
                ExpiresAt = DateTime.UtcNow.AddHours(1),
                User = new SpotifyUser { Id = "id", DisplayName = "name" }
            });

        public Task<AuthSession> RefreshTokenAsync(string refreshToken, CancellationToken cancellationToken = default)
            => OnRefreshTokenAsync?.Invoke(refreshToken, cancellationToken) ?? Task.FromResult(new AuthSession
            {
                SessionId = "sid",
                AccessToken = "access",
                RefreshToken = "refresh",
                ExpiresAt = DateTime.UtcNow.AddHours(1),
                User = new SpotifyUser { Id = "id", DisplayName = "name" }
            });

        public Task<SpotifyUser> GetUserProfileAsync(string accessToken, CancellationToken cancellationToken = default)
            => OnGetUserProfileAsync?.Invoke(accessToken, cancellationToken) ?? Task.FromResult(new SpotifyUser { Id = "id", DisplayName = "name" });
    }

    private sealed class FakeEnv : IHostEnvironment
    {
        public FakeEnv(bool isDev)
        {
            EnvironmentName = isDev ? Environments.Development : Environments.Production;
            ApplicationName = "RePlay.Server";
            ContentRootPath = System.IO.Directory.GetCurrentDirectory();
            ContentRootFileProvider = new Microsoft.Extensions.FileProviders.NullFileProvider();
        }
        public string EnvironmentName { get; set; }
        public string ApplicationName { get; set; }
        public string ContentRootPath { get; set; }
        public Microsoft.Extensions.FileProviders.IFileProvider ContentRootFileProvider { get; set; }
    }

    private static HttpContext ContextWithServices(bool isDev = true)
    {
        var ctx = new DefaultHttpContext();
        var sc = new ServiceCollection();
        sc.AddSingleton<IHostEnvironment>(new FakeEnv(isDev));
        ctx.RequestServices = sc.BuildServiceProvider();
        return ctx;
    }

    private static HttpContext ContextWithCookies(string cookieHeader, bool isDev = true)
    {
        var ctx = ContextWithServices(isDev);
        ctx.Request.Headers.Append("Cookie", cookieHeader);
        return ctx;
    }

    private static AuthSession NewSession(string id = "sid", DateTime? expiresAt = null) => new AuthSession
    {
        SessionId = id,
        AccessToken = "access",
        RefreshToken = "refresh",
        ExpiresAt = expiresAt ?? DateTime.UtcNow.AddHours(1),
        User = new SpotifyUser { Id = "id", DisplayName = "name" }
    };

    [Fact]
    public async Task PostRefresh_InvalidGrant_RevokesSessionAndClearsCookie()
    {
        using var store = new InMemorySessionStore();
        store.StoreSession(NewSession());
        var ctx = ContextWithCookies("replay_session_id=sid");
        using var client = new HttpClient(new InvalidGrantHandler());
        var auth = new SpotifyAuthService(client, Options.Create(new SpotifyOptions
        {
            ClientId = "client", ClientSecret = "secret", RedirectUri = "https://replay.example/api/auth/callback"
        }));

        var result = (JsonHttpResult<ApiError>)await InvokeAsync(GetPrivate("PostRefresh"), auth, store, ctx, CancellationToken.None);

        result.StatusCode.Should().Be(401);
        result.Value!.Code.Should().Be("TOKEN_REFRESH_REJECTED");
        result.Value.Details.Should().BeNull();
        store.GetSession("sid").Should().BeNull();
        string.Join(";", ctx.Response.Headers.SetCookie.ToArray()).Should().Contain("replay_session_id=");
    }

    private sealed class InvalidGrantHandler : HttpMessageHandler
    {
        protected override Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken)
            => Task.FromResult(new HttpResponseMessage(HttpStatusCode.BadRequest)
            {
                Content = new StringContent("""{"error":"invalid_grant","error_description":"sensitive provider detail"}""")
            });
    }

    [Theory]
    [InlineData(HttpStatusCode.ServiceUnavailable)]
    [InlineData(HttpStatusCode.TooManyRequests)]
    [InlineData(HttpStatusCode.Unauthorized)]
    [InlineData(HttpStatusCode.BadRequest)]
    public async Task PostRefresh_OtherProviderFailures_PreserveSessionAndHideDetails(HttpStatusCode statusCode)
    {
        using var store = new InMemorySessionStore();
        store.StoreSession(NewSession());
        var ctx = ContextWithCookies("replay_session_id=sid");
        var auth = new FakeAuthService
        {
            OnRefreshTokenAsync = (_, _) => throw new HttpRequestException("sensitive provider detail", null, statusCode)
        };

        var result = (JsonHttpResult<ApiError>)await InvokeAsync(GetPrivate("PostRefresh"), auth, store, ctx, CancellationToken.None);

        result.StatusCode.Should().Be(502);
        result.Value!.Details.Should().BeNull();
        store.GetSession("sid").Should().NotBeNull();
        ctx.Response.Headers.SetCookie.Should().BeEmpty();
    }

    [Theory]
    [InlineData("/api/auth/session", 200, typeof(SessionInfo))]
    [InlineData("/api/auth/session", 401, typeof(ApiError))]
    [InlineData("/api/auth/refresh", 200, typeof(SessionInfo))]
    [InlineData("/api/auth/refresh", 401, typeof(ApiError))]
    [InlineData("/api/auth/refresh", 502, typeof(ApiError))]
    [InlineData("/api/auth/logout", 204, typeof(void))]
    public async Task AuthRoutes_DeclareResponseContracts(string path, int statusCode, Type responseType)
    {
        var builder = WebApplication.CreateBuilder();
        builder.Services.AddSingleton<ISpotifyAuthService>(new FakeAuthService());
        builder.Services.AddSingleton<ISessionStore, InMemorySessionStore>();
        await using var app = builder.Build();
        app.MapGroup("/api/auth").MapAuthEndpoints();

        var endpoint = ((IEndpointRouteBuilder)app).DataSources.SelectMany(source => source.Endpoints)
            .OfType<RouteEndpoint>().Single(endpoint => endpoint.RoutePattern.RawText == path);

        endpoint.Metadata.GetOrderedMetadata<IProducesResponseTypeMetadata>()
            .Should().Contain(metadata => metadata.StatusCode == statusCode && metadata.Type == responseType);
    }

    [Fact]
    public async Task GetLogin_UsesConfiguredCallback_NotRequestHost()
    {
        var ctx = ContextWithServices();
        ctx.Request.Scheme = "https";
        ctx.Request.Host = new HostString("attacker.example");
        string? overrideUri = "not-called";
        var auth = new FakeAuthService { OnGetAuthorizationUrl = (_, uri) => { overrideUri = uri; return "https://auth.example"; } };

        await InvokeAsync(GetPrivate("GetLogin"), "/go", auth, ctx);

        overrideUri.Should().BeNull();
        string.Join(";", ctx.Response.Headers.SetCookie.ToArray()).Should().NotContain("replay_redirect_uri");
    }

    [Theory]
    [InlineData("https://attacker.example")]
    [InlineData("//attacker.example")]
    [InlineData("/\\attacker.example")]
    [InlineData("/safe\\bad")]
    public async Task Callback_RejectsNonLocalReturnPaths_AndCookieCallbackOverride(string returnUrl)
    {
        var ctx = ContextWithCookies($"spotify_auth_state=abc; replay_return_url={Uri.EscapeDataString(returnUrl)}; replay_redirect_uri=https://attacker.example");
        string? overrideUri = "not-called";
        var auth = new FakeAuthService
        {
            OnExchangeCodeAsync = (_, uri, _) => { overrideUri = uri; return Task.FromResult(NewSession()); }
        };

        var result = (RedirectHttpResult)await InvokeAsync(GetPrivate("GetCallback"), "code", "abc", null!, auth, new InMemorySessionStore(), ctx, CancellationToken.None);

        result.Url.Should().Be("/");
        result.AcceptLocalUrlOnly.Should().BeTrue();
        overrideUri.Should().BeNull();
    }

    [Fact]
    public async Task PostRefresh_LogoutDuringProviderCall_DoesNotResurrectSession()
    {
        var store = new InMemorySessionStore();
        store.StoreSession(NewSession());
        var started = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);
        var release = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);
        var auth = new FakeAuthService
        {
            OnRefreshTokenAsync = async (_, _) => { started.SetResult(); await release.Task; return NewSession("new-id"); }
        };
        var refresh = InvokeAsync(GetPrivate("PostRefresh"), auth, store, ContextWithCookies("replay_session_id=sid"), CancellationToken.None);
        await started.Task;
        await InvokeAsync(GetPrivate("PostLogout"), store, ContextWithCookies("replay_session_id=sid"));
        release.SetResult();

        var result = await refresh;

        store.GetSession("sid").Should().BeNull();
        store.GetSession("new-id").Should().BeNull();
        ((JsonHttpResult<ApiError>)result).StatusCode.Should().Be(401);
    }

    [Fact]
    public async Task GetLogin_SetsCookies_AndRedirects()
    {
        var mi = GetPrivate("GetLogin");
        var ctx = ContextWithServices();
        var auth = new FakeAuthService { OnGetAuthorizationUrl = (s, r) => "https://auth.example/authorize?state=" + s };

        var result = await InvokeAsync(mi, null!, auth, ctx);

        result.Should().BeOfType<RedirectHttpResult>();
        var setCookie = string.Join(";", ctx.Response.Headers["Set-Cookie"].ToArray());
        setCookie.Should().Contain("spotify_auth_state=");
        setCookie.Should().Contain("replay_return_url=");
    }

    [Fact]
    public async Task GetLogin_ConfigError_Returns500()
    {
        var mi = GetPrivate("GetLogin");
        var ctx = ContextWithServices();
        var auth = new FakeAuthService { OnGetAuthorizationUrl = (_, __) => throw new ArgumentException("bad config") };

        var result = await InvokeAsync(mi, null!, auth, ctx);

        result.Should().BeOfType<JsonHttpResult<ApiError>>();
        var json = (JsonHttpResult<ApiError>)result;
        json.StatusCode.Should().Be(500);
        json.Value!.Code.Should().Be("SPOTIFY_CONFIG_ERROR");
        json.Value.Details.Should().BeNull();
    }

    [Fact]
    public async Task GetCallback_OAuthError_ReturnsBadRequest()
    {
        var mi = GetPrivate("GetCallback");
        var ctx = ContextWithCookies("spotify_auth_state=abc; replay_return_url=/go");
        var auth = new FakeAuthService();
        var store = new InMemorySessionStore();

        var result = await InvokeAsync(mi, null!, "abc", "access_denied", auth, store, ctx, CancellationToken.None);
        result.Should().BeOfType<BadRequest<ApiError>>();
        ((BadRequest<ApiError>)result).Value!.Code.Should().Be("OAUTH_ERROR");
    }

    [Fact]
    public async Task GetCallback_MissingParams_ReturnsBadRequest()
    {
        var mi = GetPrivate("GetCallback");
        var ctx = ContextWithCookies("spotify_auth_state=abc");
        var auth = new FakeAuthService();
        var store = new InMemorySessionStore();

        var result = await InvokeAsync(mi, null!, null!, null!, auth, store, ctx, CancellationToken.None);
        result.Should().BeOfType<BadRequest<ApiError>>();
        ((BadRequest<ApiError>)result).Value!.Code.Should().Be("MISSING_PARAMETERS");
    }

    [Fact]
    public async Task GetCallback_InvalidState_ReturnsBadRequest()
    {
        var mi = GetPrivate("GetCallback");
        var ctx = ContextWithCookies("spotify_auth_state=zzz");
        var auth = new FakeAuthService();
        var store = new InMemorySessionStore();

        var result = await InvokeAsync(mi, "code", "abc", null!, auth, store, ctx, CancellationToken.None);
        result.Should().BeOfType<BadRequest<ApiError>>();
        ((BadRequest<ApiError>)result).Value!.Code.Should().Be("INVALID_STATE");
    }

    [Fact]
    public async Task GetCallback_Success_SetsSessionCookie_AndRedirects()
    {
        var mi = GetPrivate("GetCallback");
        var ctx = ContextWithCookies("spotify_auth_state=abc; replay_return_url=/go");
        var store = new InMemorySessionStore();
        var auth = new FakeAuthService
        {
            OnExchangeCodeAsync = (code, state, ct) => Task.FromResult(NewSession("sid"))
        };

        var result = await InvokeAsync(mi, "code", "abc", null!, auth, store, ctx, CancellationToken.None);

        result.Should().BeOfType<RedirectHttpResult>();
        ((RedirectHttpResult)result).Url.Should().Be("/go");
        var setCookie = string.Join(";", ctx.Response.Headers["Set-Cookie"].ToArray());
        setCookie.Should().Contain("replay_session_id=sid");
        setCookie.Should().Contain("spotify_auth_state="); // deleted
        setCookie.Should().Contain("replay_return_url=");   // deleted
    }

    [Fact]
    public async Task GetCallback_SpotifyApiError_Returns503()
    {
        var mi = GetPrivate("GetCallback");
        var ctx = ContextWithCookies("spotify_auth_state=abc");
        var store = new InMemorySessionStore();
        var auth = new FakeAuthService
        {
            OnExchangeCodeAsync = (code, state, ct) => throw new HttpRequestException("fail")
        };

        var result = await InvokeAsync(mi, "code", "abc", null!, auth, store, ctx, CancellationToken.None);
        result.Should().BeOfType<JsonHttpResult<ApiError>>();
        var json = (JsonHttpResult<ApiError>)result;
        json.StatusCode.Should().Be(502);
        json.Value!.Code.Should().Be("SPOTIFY_API_ERROR");
        json.Value.Details.Should().BeNull();
    }

    [Fact]
    public async Task GetSession_NoCookie_Returns401()
    {
        var mi = GetPrivate("GetSession");
        var ctx = ContextWithServices();
        var store = new InMemorySessionStore();
        var result = await InvokeAsync(mi, store, ctx);
        result.Should().BeOfType<JsonHttpResult<ApiError>>();
        ((JsonHttpResult<ApiError>)result).StatusCode.Should().Be(401);
    }

    [Fact]
    public async Task GetSession_InvalidSession_Returns401()
    {
        var mi = GetPrivate("GetSession");
        var ctx = ContextWithCookies("replay_session_id=sid");
        var store = new InMemorySessionStore();
        var result = await InvokeAsync(mi, store, ctx);
        result.Should().BeOfType<JsonHttpResult<ApiError>>();
        ((JsonHttpResult<ApiError>)result).StatusCode.Should().Be(401);
    }

    [Theory]
    [InlineData(-60)]
    [InlineData(4)]
    public async Task GetSession_ExpiredOrExpiringAccessToken_RemainsRefreshable(int minutesUntilExpiry)
    {
        var mi = GetPrivate("GetSession");
        var ctx = ContextWithCookies("replay_session_id=sid");
        var store = new InMemorySessionStore();
        store.StoreSession(NewSession("sid", DateTime.UtcNow.AddMinutes(minutesUntilExpiry)));

        var result = await InvokeAsync(mi, store, ctx);
        result.Should().BeOfType<Ok<SessionInfo>>();
        store.GetSession("sid").Should().NotBeNull();
        ctx.Response.Headers.SetCookie.Should().BeEmpty();
    }

    [Fact]
    public async Task GetSession_AbsoluteExpiry_ReturnsUnauthorized()
    {
        var store = new InMemorySessionStore();
        store.StoreSession(NewSession() with { CreatedAt = DateTime.UtcNow.AddDays(-31) });

        var result = await InvokeAsync(GetPrivate("GetSession"), store, ContextWithCookies("replay_session_id=sid"));

        ((JsonHttpResult<ApiError>)result).StatusCode.Should().Be(401);
        store.GetSession("sid").Should().BeNull();
    }

    [Fact]
    public async Task GetSession_Valid_ReturnsOk()
    {
        var mi = GetPrivate("GetSession");
        var ctx = ContextWithCookies("replay_session_id=sid");
        var store = new InMemorySessionStore();
        store.StoreSession(NewSession("sid"));

        var result = await InvokeAsync(mi, store, ctx);
        result.Should().BeOfType<Ok<SessionInfo>>();
        var ok = (Ok<SessionInfo>)result;
        ok.Value!.SessionId.Should().Be("sid");
    }

    [Fact]
    public async Task PostRefresh_NoCookie_Returns401()
    {
        var mi = GetPrivate("PostRefresh");
        var ctx = ContextWithServices();
        var store = new InMemorySessionStore();
        var auth = new FakeAuthService();
        var result = await InvokeAsync(mi, auth, store, ctx, CancellationToken.None);
        result.Should().BeOfType<JsonHttpResult<ApiError>>();
        ((JsonHttpResult<ApiError>)result).StatusCode.Should().Be(401);
    }

    [Fact]
    public async Task PostRefresh_InvalidSession_Returns401()
    {
        var mi = GetPrivate("PostRefresh");
        var ctx = ContextWithCookies("replay_session_id=sid");
        var store = new InMemorySessionStore();
        var auth = new FakeAuthService();
        var result = await InvokeAsync(mi, auth, store, ctx, CancellationToken.None);
        result.Should().BeOfType<JsonHttpResult<ApiError>>();
        ((JsonHttpResult<ApiError>)result).StatusCode.Should().Be(401);
    }

    [Fact]
    public async Task PostRefresh_TransientFailure_RetainsSession()
    {
        var mi = GetPrivate("PostRefresh");
        var ctx = ContextWithCookies("replay_session_id=sid");
        var store = new InMemorySessionStore();
        store.StoreSession(NewSession("sid"));
        var auth = new FakeAuthService { OnRefreshTokenAsync = (rt, ct) => throw new HttpRequestException("nope") };

        var result = await InvokeAsync(mi, auth, store, ctx, CancellationToken.None);
        result.Should().BeOfType<JsonHttpResult<ApiError>>();
        var json = (JsonHttpResult<ApiError>)result;
        json.StatusCode.Should().Be(502);
        json.Value!.Code.Should().Be("TOKEN_REFRESH_FAILED");
        store.GetSession("sid").Should().NotBeNull();
        ctx.Response.Headers.SetCookie.Should().BeEmpty();
    }

    [Fact]
    public async Task PostRefresh_Success_UpdatesCookie_ReturnsOk()
    {
        var mi = GetPrivate("PostRefresh");
        var ctx = ContextWithCookies("replay_session_id=sid");
        var store = new InMemorySessionStore();
        store.StoreSession(NewSession("sid"));
        var config = new ExternalSourceConfig { Source = "lastfm", ConfigValue = "listener", ConfiguredAt = DateTime.UtcNow };
        store.StoreSourceConfig("sid", config);
        var auth = new FakeAuthService
        {
            OnRefreshTokenAsync = (rt, ct) => Task.FromResult(NewSession("sid2"))
        };

        var result = await InvokeAsync(mi, auth, store, ctx, CancellationToken.None);
        result.Should().BeOfType<Ok<SessionInfo>>();
        var setCookie = string.Join(";", ctx.Response.Headers["Set-Cookie"].ToArray());
        setCookie.Should().Contain("replay_session_id=sid;");
        ((Ok<SessionInfo>)result).Value!.SessionId.Should().Be("sid");
        store.GetSession("sid2").Should().BeNull();
        store.GetSourceConfig("sid", "lastfm").Should().Be(config);
    }

    [Fact]
    public async Task PostLogout_WithCookie_DeletesCookie_NoContent()
    {
        var mi = GetPrivate("PostLogout");
        var ctx = ContextWithCookies("replay_session_id=sid");
        var store = new InMemorySessionStore();
        store.StoreSession(NewSession("sid"));

        var result = await InvokeAsync(mi, store, ctx);
        result.Should().BeOfType<NoContent>();
        var setCookie = string.Join(";", ctx.Response.Headers["Set-Cookie"].ToArray());
        setCookie.Should().Contain("replay_session_id="); // deleted
    }

    [Fact]
    public async Task PostLogout_NoCookie_NoContent()
    {
        var mi = GetPrivate("PostLogout");
        var ctx = ContextWithServices();
        var store = new InMemorySessionStore();

        var result = await InvokeAsync(mi, store, ctx);
        result.Should().BeOfType<NoContent>();
    }
}
