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
        var group = endpoints.MapGroup("/playlist").AddEndpointFilter<SessionEndpointFilter>().WithProviderErrors();

        group.MapPost("/create", CreatePlaylist)
            .WithName("CreatePlaylist")
            .WithSummary("Create a playlist on Spotify")
            .WithDescription("Creates a new Spotify playlist with the provided tracks. Requires an authenticated session.")
            .Accepts<PlaylistCreationRequest>("application/json")
            .Produces<PlaylistCreationResponse>(StatusCodes.Status200OK)
            .Produces<PlaylistCreationResponse>(StatusCodes.Status502BadGateway)
            .Produces<ApiError>(StatusCodes.Status400BadRequest)
            .Produces<ApiError>(StatusCodes.Status401Unauthorized)
            .Produces<ApiError>(StatusCodes.Status500InternalServerError)
            .AddOpenApiOperationTransformer(async (operation, context, cancellationToken) =>
            {
                // A known partial playlist and an unconfirmed creation have different recovery data.
                var response = operation.Responses!["502"].Content!["application/json"];
                response.Schema = new Microsoft.OpenApi.OpenApiSchema
                {
                    OneOf = [response.Schema!, await context.GetOrCreateSchemaAsync(typeof(ApiError), null, cancellationToken)]
                };
            });
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

        if (string.IsNullOrWhiteSpace(request.Name) || request.Name.Length > 100 || request.Description?.Length > 300)
        {
            return ApiErrorExtensions.BadRequest(
                "INVALID_PLAYLIST_NAME",
                "Playlist name is required and cannot be empty.");
        }

        // Sanitize description by removing line breaks (Spotify limitation)
        var sanitizedDescription = request.Description is not null
            ? System.Text.RegularExpressions.Regex.Replace(request.Description, @"[\r\n]+", " ")
            : null;

        // Create sanitized request if description was modified
        var sanitizedRequest = sanitizedDescription != request.Description
            ? request with { Description = sanitizedDescription }
            : request;

        if (sanitizedRequest.TrackUris == null || sanitizedRequest.TrackUris.Count is 0 or > 10000)
        {
            return ApiErrorExtensions.BadRequest(
                "INVALID_TRACKS",
                "At least one track URI is required.");
        }

        // Validate all track URIs have correct format
        foreach (var uri in sanitizedRequest.TrackUris)
        {
            if (uri is null || !uri.StartsWith("spotify:track:", StringComparison.Ordinal) ||
                uri.Length is <= 14 or > 78 || uri.Skip(14).Any(c => !char.IsAsciiLetterOrDigit(c)))
            {
                return ApiErrorExtensions.BadRequest(
                    "INVALID_TRACK_URI",
                    $"Invalid Spotify track URI format: {uri}. Expected format: spotify:track:ID");
            }
        }

        var (session, sessionError) = SessionAccess.Get(context, sessionStore, requireSpotifyToken: true);
        if (sessionError is not null) return sessionError;

        var userId = session!.User.Id;
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
                sanitizedRequest,
                accessToken,
                userId,
                cancellationToken);

            return response.IsComplete ? Results.Ok(response) : Results.Json(response, statusCode: StatusCodes.Status502BadGateway);
        }
        catch (HttpRequestException ex) { return ProviderErrors.Spotify(ex); }
        catch (OperationCanceledException) when (cancellationToken.IsCancellationRequested) { throw; }
        catch (InvalidOperationException)
        {
            return ApiErrorExtensions.InternalServerError(
                "PLAYLIST_CREATION_FAILED",
                "Failed to create playlist on Spotify.");
        }
        catch (Exception)
        {
            return ApiErrorExtensions.InternalServerError(
                "INTERNAL_SERVER_ERROR",
                "An unexpected error occurred while creating the playlist.");
        }
    }
}
