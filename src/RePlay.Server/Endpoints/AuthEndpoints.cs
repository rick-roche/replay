using Microsoft.AspNetCore.Mvc;
using RePlay.Server.Models;
using RePlay.Server.Services;

namespace RePlay.Server.Endpoints;

/// <summary>
/// Authentication endpoints for Spotify OAuth flow.
/// </summary>
public static class AuthEndpoints
{
    private const string StateCookieName = "spotify_auth_state";
    private const string SessionCookieName = "replay_session_id";
    private const string ReturnUrlCookieName = "replay_return_url";

    public static RouteGroupBuilder MapAuthEndpoints(this RouteGroupBuilder group)
    {
        group.MapGet("/login", GetLogin);
        group.MapGet("/callback", GetCallback);
        group.MapGet("/session", GetSession);
        group.MapPost("/refresh", PostRefresh);
        group.MapPost("/logout", PostLogout);

        return group;
    }

    /// <summary>
    /// Initiate Spotify OAuth login.
    /// Generates a state token and redirects to Spotify authorization.
    /// </summary>
    private static IResult GetLogin(
        [FromQuery] string? returnUrl,
        ISpotifyAuthService authService,
        HttpContext httpContext)
    {
        var env = httpContext.RequestServices.GetRequiredService<IHostEnvironment>();
        var isDev = env.IsDevelopment();

        // Generate anti-forgery state token
        var state = Guid.NewGuid().ToString();

        // Default return url (fallback)
        returnUrl = string.IsNullOrWhiteSpace(returnUrl) ? "/" : returnUrl;

        // Store state in cookie for validation on callback
        httpContext.Response.Cookies.Append(StateCookieName, state, new CookieOptions
        {
            HttpOnly = true,
            Secure = !isDev,
            SameSite = SameSiteMode.Lax,
            MaxAge = TimeSpan.FromMinutes(10)
        });

        // Store return url (short-lived)
        httpContext.Response.Cookies.Append(ReturnUrlCookieName, returnUrl, new CookieOptions
        {
            HttpOnly = true,
            Secure = !isDev,
            SameSite = SameSiteMode.Lax,
            MaxAge = TimeSpan.FromMinutes(10)
        });

        try
        {
            var authUrl = authService.GetAuthorizationUrl(state);
            return Results.Redirect(authUrl);
        }
        catch (ArgumentException ex)
        {
            return Results.Problem(
                detail: ex.Message,
                statusCode: 500,
                title: "Configuration Error");
        }
    }

    /// <summary>
    /// Handle Spotify OAuth callback.
    /// Exchanges code for tokens and creates a session.
    /// </summary>
    private static async Task<IResult> GetCallback(
        [FromQuery] string? code,
        [FromQuery] string? state,
        [FromQuery] string? error,
        ISpotifyAuthService authService,
        ISessionStore sessionStore,
        HttpContext httpContext,
        CancellationToken cancellationToken)
    {
        // Check for OAuth errors
        if (!string.IsNullOrEmpty(error))
        {
            return Results.Problem(
                detail: $"Spotify authorization failed: {error}",
                statusCode: 400,
                title: "Authorization Failed");
        }

        // Validate required parameters
        if (string.IsNullOrEmpty(code) || string.IsNullOrEmpty(state))
        {
            return Results.Problem(
                detail: "Missing required parameters",
                statusCode: 400,
                title: "Bad Request");
        }

        // Validate state token (CSRF protection)
        if (!httpContext.Request.Cookies.TryGetValue(StateCookieName, out var storedState) ||
            storedState != state)
        {
            return Results.Problem(
                detail: "Invalid state token",
                statusCode: 400,
                title: "Invalid State");
        }

        try
        {
            var env = httpContext.RequestServices.GetRequiredService<IHostEnvironment>();
            var isDev = env.IsDevelopment();

            // Exchange authorization code for tokens
            var session = await authService.ExchangeCodeAsync(code, cancellationToken);

            // Store session
            sessionStore.StoreSession(session);

            // Set session cookie
            httpContext.Response.Cookies.Append(SessionCookieName, session.SessionId, new CookieOptions
            {
                HttpOnly = true,
                Secure = !isDev,
                SameSite = SameSiteMode.Lax,
                MaxAge = TimeSpan.FromDays(30)
            });

            // Clean up state cookie
            httpContext.Response.Cookies.Delete(StateCookieName);

            // Read return url
            httpContext.Request.Cookies.TryGetValue(ReturnUrlCookieName, out var returnUrl);
            httpContext.Response.Cookies.Delete(ReturnUrlCookieName);

            // Redirect to frontend (fallback if missing)
            return Results.Redirect(string.IsNullOrWhiteSpace(returnUrl) ? "/" : returnUrl);
        }
        catch (HttpRequestException ex)
        {
            return Results.Problem(
                detail: ex.Message,
                statusCode: 502,
                title: "Spotify API Error");
        }
    }

    /// <summary>
    /// Get current session information.
    /// </summary>
    private static IResult GetSession(
        ISessionStore sessionStore,
        HttpContext httpContext)
    {
        if (!httpContext.Request.Cookies.TryGetValue(SessionCookieName, out var sessionId))
        {
            return Results.Unauthorized();
        }

        var session = sessionStore.GetSession(sessionId);
        if (session == null)
        {
            return Results.Unauthorized();
        }

        // Check if session is expired
        if (session.IsExpired())
        {
            sessionStore.RemoveSession(sessionId);
            httpContext.Response.Cookies.Delete(SessionCookieName);
            return Results.Unauthorized();
        }

        var sessionInfo = new SessionInfo
        {
            SessionId = session.SessionId,
            User = session.User,
            ExpiresAt = session.ExpiresAt
        };

        return Results.Ok(sessionInfo);
    }

    /// <summary>
    /// Refresh an expired or expiring access token.
    /// </summary>
    private static async Task<IResult> PostRefresh(
        ISpotifyAuthService authService,
        ISessionStore sessionStore,
        HttpContext httpContext,
        CancellationToken cancellationToken)
    {
        if (!httpContext.Request.Cookies.TryGetValue(SessionCookieName, out var sessionId))
        {
            return Results.Unauthorized();
        }

        var session = sessionStore.GetSession(sessionId);
        if (session == null)
        {
            return Results.Unauthorized();
        }

        try
        {
            var env = httpContext.RequestServices.GetRequiredService<IHostEnvironment>();
            var isDev = env.IsDevelopment();

            // Refresh the token
            var newSession = await authService.RefreshTokenAsync(session.RefreshToken, cancellationToken);

            // Update stored session
            sessionStore.StoreSession(newSession);

            // Update session cookie
            httpContext.Response.Cookies.Append(SessionCookieName, newSession.SessionId, new CookieOptions
            {
                HttpOnly = true,
                Secure = !isDev,
                SameSite = SameSiteMode.Lax,
                MaxAge = TimeSpan.FromDays(30)
            });

            var sessionInfo = new SessionInfo
            {
                SessionId = newSession.SessionId,
                User = newSession.User,
                ExpiresAt = newSession.ExpiresAt
            };

            return Results.Ok(sessionInfo);
        }
        catch (HttpRequestException ex)
        {
            sessionStore.RemoveSession(sessionId);
            httpContext.Response.Cookies.Delete(SessionCookieName);

            return Results.Problem(
                detail: ex.Message,
                statusCode: 502,
                title: "Token Refresh Failed");
        }
    }

    /// <summary>
    /// Logout and clear session.
    /// </summary>
    private static IResult PostLogout(
        ISessionStore sessionStore,
        HttpContext httpContext)
    {
        if (httpContext.Request.Cookies.TryGetValue(SessionCookieName, out var sessionId))
        {
            sessionStore.RemoveSession(sessionId);
            httpContext.Response.Cookies.Delete(SessionCookieName);
        }

        return Results.NoContent();
    }
}
