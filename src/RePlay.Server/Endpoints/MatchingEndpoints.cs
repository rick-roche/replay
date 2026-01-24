using Microsoft.AspNetCore.Mvc;
using RePlay.Server.Models;
using RePlay.Server.Services;

namespace RePlay.Server.Endpoints;

/// <summary>
/// Endpoints for matching normalized tracks to Spotify.
/// </summary>
public static class MatchingEndpoints
{
    public static void MapMatchingEndpoints(this IEndpointRouteBuilder endpoints)
    {
        var group = endpoints.MapGroup("/api/match");

        group.MapPost("/spotify", MatchTracksAsync)
            .WithName("MatchTracksToSpotify")
            .WithSummary("Match normalized tracks to Spotify")
            .WithDescription("Attempts to find Spotify matches for normalized tracks using exact, normalized, fuzzy, and album-based matching strategies.")
            .Accepts<MatchTracksRequest>("application/json")
            .Produces<MatchedDataResponse>(StatusCodes.Status200OK)
            .Produces<ApiError>(StatusCodes.Status400BadRequest)
            .Produces<ApiError>(StatusCodes.Status401Unauthorized)
            .Produces<ApiError>(StatusCodes.Status500InternalServerError);
    }

    /// <summary>
    /// Matches normalized tracks to Spotify.
    /// </summary>
    /// <param name="request">Request containing normalized tracks.</param>
    /// <param name="matchingService">Spotify matching service.</param>
    /// <param name="sessionStore">Session store to retrieve access token.</param>
    /// <param name="httpContext">HTTP context to get session ID from cookie.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>Matched tracks with confidence scores.</returns>
    private static async Task<IResult> MatchTracksAsync(
        [FromBody] MatchTracksRequest request,
        [FromServices] ISpotifyMatchingService matchingService,
        [FromServices] ISessionStore sessionStore,
        HttpContext httpContext,
        CancellationToken cancellationToken)
    {
        // Validate request
        if (request.Tracks is null || request.Tracks.Count == 0)
        {
            return Results.BadRequest(new ApiError
            {
                Code = "INVALID_REQUEST",
                Message = "Tracks list cannot be null or empty."
            });
        }

        // Get session from cookie
        if (!httpContext.Request.Cookies.TryGetValue("replay_session_id", out var sessionId) ||
            string.IsNullOrWhiteSpace(sessionId))
        {
            return Results.Unauthorized();
        }

        // Retrieve session and access token
        var session = sessionStore.GetSession(sessionId);
        if (session is null)
        {
            return Results.Unauthorized();
        }

        try
        {
            var matchedData = await matchingService.MatchTracksAsync(
                request.Tracks, 
                session.AccessToken,
                cancellationToken);

            return Results.Ok(matchedData);
        }
        catch (HttpRequestException ex)
        {
            return Results.Problem(
                detail: ex.Message,
                statusCode: StatusCodes.Status503ServiceUnavailable,
                title: "Spotify API Error");
        }
        catch (Exception ex)
        {
            return Results.Problem(
                detail: ex.Message,
                statusCode: StatusCodes.Status500InternalServerError,
                title: "Internal Server Error");
        }
    }
}

/// <summary>
/// Request to match normalized tracks to Spotify.
/// </summary>
public sealed class MatchTracksRequest
{
    /// <summary>
    /// Normalized tracks to match.
    /// </summary>
    public required List<NormalizedTrack> Tracks { get; init; }

}
