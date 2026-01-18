using System.Reflection;
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
        public Func<string, string>? OnGetAuthorizationUrl { get; set; }
        public Func<string, CancellationToken, Task<AuthSession>>? OnExchangeCodeAsync { get; set; }
        public Func<string, CancellationToken, Task<AuthSession>>? OnRefreshTokenAsync { get; set; }
        public Func<string, CancellationToken, Task<SpotifyUser>>? OnGetUserProfileAsync { get; set; }

        public string GetAuthorizationUrl(string state)
            => OnGetAuthorizationUrl?.Invoke(state) ?? "https://auth.example/authorize";

        public Task<AuthSession> ExchangeCodeAsync(string code, CancellationToken cancellationToken = default)
            => OnExchangeCodeAsync?.Invoke(code, cancellationToken) ?? Task.FromResult(new AuthSession
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
    public async Task GetLogin_SetsCookies_AndRedirects()
    {
        var mi = GetPrivate("GetLogin");
        var ctx = ContextWithServices();
        var auth = new FakeAuthService { OnGetAuthorizationUrl = s => "https://auth.example/authorize?state=" + s };

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
        var auth = new FakeAuthService { OnGetAuthorizationUrl = _ => throw new ArgumentException("bad config") };

        var result = await InvokeAsync(mi, null!, auth, ctx);

        result.Should().BeOfType<JsonHttpResult<ApiError>>();
        var json = (JsonHttpResult<ApiError>)result;
        json.StatusCode.Should().Be(500);
        json.Value!.Code.Should().Be("SPOTIFY_CONFIG_ERROR");
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
            OnExchangeCodeAsync = (code, ct) => Task.FromResult(NewSession("sid"))
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
            OnExchangeCodeAsync = (code, ct) => throw new HttpRequestException("fail")
        };

        var result = await InvokeAsync(mi, "code", "abc", null!, auth, store, ctx, CancellationToken.None);
        result.Should().BeOfType<JsonHttpResult<ApiError>>();
        var json = (JsonHttpResult<ApiError>)result;
        json.StatusCode.Should().Be(502);
        json.Value!.Code.Should().Be("SPOTIFY_API_ERROR");
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

    [Fact]
    public async Task GetSession_Expired_RemovesCookie_Returns401()
    {
        var mi = GetPrivate("GetSession");
        var ctx = ContextWithCookies("replay_session_id=sid");
        var store = new InMemorySessionStore();
        store.StoreSession(NewSession("sid", DateTime.UtcNow.AddHours(-1)));

        var result = await InvokeAsync(mi, store, ctx);
        result.Should().BeOfType<JsonHttpResult<ApiError>>();
        ((JsonHttpResult<ApiError>)result).StatusCode.Should().Be(401);
        var setCookie = string.Join(";", ctx.Response.Headers["Set-Cookie"].ToArray());
        setCookie.Should().Contain("replay_session_id="); // deleted
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
    public async Task PostRefresh_Failure_RemovesCookie_Returns503()
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
        var setCookie = string.Join(";", ctx.Response.Headers["Set-Cookie"].ToArray());
        setCookie.Should().Contain("replay_session_id="); // deleted
    }

    [Fact]
    public async Task PostRefresh_Success_UpdatesCookie_ReturnsOk()
    {
        var mi = GetPrivate("PostRefresh");
        var ctx = ContextWithCookies("replay_session_id=sid");
        var store = new InMemorySessionStore();
        store.StoreSession(NewSession("sid"));
        var auth = new FakeAuthService
        {
            OnRefreshTokenAsync = (rt, ct) => Task.FromResult(NewSession("sid2"))
        };

        var result = await InvokeAsync(mi, auth, store, ctx, CancellationToken.None);
        result.Should().BeOfType<Ok<SessionInfo>>();
        var setCookie = string.Join(";", ctx.Response.Headers["Set-Cookie"].ToArray());
        setCookie.Should().Contain("replay_session_id=sid2");
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
