using RePlay.Server.Models;
using RePlay.Server.Services;

namespace RePlay.Server.Endpoints;

internal static class SessionAccess
{
    public static (AuthSession? Session, IResult? Error) Get(HttpContext context, ISessionStore store, bool requireSpotifyToken = false)
    {
        if (!context.Request.Cookies.TryGetValue("replay_session_id", out var id) || string.IsNullOrWhiteSpace(id))
            return (null, ApiErrorExtensions.Unauthorized("NO_SESSION", "No active session found"));

        var session = store.GetSession(id);
        if (session is null)
            return (null, ApiErrorExtensions.Unauthorized("INVALID_SESSION", "Session not found or has expired"));

        // Access-token expiry must never revoke the refreshable application session.
        if (requireSpotifyToken && session.ExpiresAt <= DateTime.UtcNow)
            return (null, ApiErrorExtensions.Unauthorized("SPOTIFY_TOKEN_EXPIRED", "Refresh your Spotify access token and try again"));

        return (session, null);
    }
}

public sealed class SessionEndpointFilter(ISessionStore store) : IEndpointFilter
{
    public async ValueTask<object?> InvokeAsync(EndpointFilterInvocationContext context, EndpointFilterDelegate next)
    {
        var (_, error) = SessionAccess.Get(context.HttpContext, store);
        return error is not null ? error : await next(context);
    }
}
