using RePlay.Server.Models;
using RePlay.Server.Services;

namespace RePlay.Server.Endpoints;

/// <summary>
/// Endpoints for playlist creation and management.
/// </summary>
public static class PlaylistEndpoints
{
    /// <summary>
    /// Registers all playlist endpoints.
    /// </summary>
    public static void MapPlaylistEndpoints(this IEndpointRouteBuilder endpoints)
    {
        var group = endpoints.MapGroup("/playlist");

        group.MapPost("/create", CreatePlaylist)
            .WithName("CreatePlaylist")
            .WithSummary("Create a playlist on Spotify")
            .WithDescription("Creates a new Spotify playlist with the provided tracks. Requires an authenticated session.")
            .Accepts<PlaylistCreationRequest>("application/json")
            .Produces<PlaylistCreationResponse>(StatusCodes.Status200OK)
            .Produces<ApiError>(StatusCodes.Status400BadRequest)
            .Produces<ApiError>(StatusCodes.Status401Unauthorized)
            .Produces<ApiError>(StatusCodes.Status500InternalServerError);
    }

    /// <summary>
    /// Creates a playlist on Spotify with the provided tracks.
    /// </summary>
    private static async Task<IResult> CreatePlaylist(
        HttpContext context,
        PlaylistCreationRequest request,
        ISessionStore sessionStore,
        ISpotifyMatchingService matchingService,
        CancellationToken cancellationToken)
    {
        // Validate request
        if (request == null)
        {
            return ApiErrorExtensions.BadRequest(
                "INVALID_REQUEST",
                "Request body is required.");
        }

        if (string.IsNullOrWhiteSpace(request.Name))
        {
            return ApiErrorExtensions.BadRequest(
                "INVALID_PLAYLIST_NAME",
                "Playlist name is required and cannot be empty.");
        }

        if (request.TrackUris == null || request.TrackUris.Count == 0)
        {
            return ApiErrorExtensions.BadRequest(
                "INVALID_TRACKS",
                "At least one track URI is required.");
        }

        // Validate all track URIs have correct format
        foreach (var uri in request.TrackUris)
        {
            if (!uri.StartsWith("spotify:track:", StringComparison.Ordinal))
            {
                return ApiErrorExtensions.BadRequest(
                    "INVALID_TRACK_URI",
                    $"Invalid Spotify track URI format: {uri}. Expected format: spotify:track:ID");
            }
        }

        // Get session from cookie
        if (!context.Request.Cookies.TryGetValue("replay_session_id", out var sessionId) ||
            string.IsNullOrWhiteSpace(sessionId))
        {
            return ApiErrorExtensions.Unauthorized(
                "UNAUTHORIZED",
                "User not authenticated.");
        }

        // Retrieve session data
        var session = sessionStore.GetSession(sessionId);
        if (session is null)
        {
            return ApiErrorExtensions.Unauthorized(
                "INVALID_SESSION",
                "Session not found or invalid.");
        }

        if (session.IsExpired())
        {
            sessionStore.RemoveSession(sessionId);
            context.Response.Cookies.Delete("replay_session_id");
            return ApiErrorExtensions.Unauthorized(
                "SESSION_EXPIRED",
                "Session has expired.");
        }

        var userId = session.User.Id;
        var accessToken = session.AccessToken;

        if (string.IsNullOrWhiteSpace(userId) || string.IsNullOrWhiteSpace(accessToken))
        {
            return ApiErrorExtensions.Unauthorized(
                "MISSING_CREDENTIALS",
                "User ID or access token is missing from session.");
        }

        try
        {
            var response = await matchingService.CreatePlaylistAsync(
                request,
                accessToken,
                userId,
                cancellationToken);

            return Results.Ok(response);
        }
        catch (InvalidOperationException ex)
        {
            return ApiErrorExtensions.InternalServerError(
                "PLAYLIST_CREATION_FAILED",
                "Failed to create playlist on Spotify.",
                ex.Message);
        }
        catch (Exception ex)
        {
            return ApiErrorExtensions.InternalServerError(
                "INTERNAL_SERVER_ERROR",
                "An unexpected error occurred while creating the playlist.",
                ex.Message);
        }
    }
}

