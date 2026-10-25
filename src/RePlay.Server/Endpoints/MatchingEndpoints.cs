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
        var group = endpoints.MapGroup("/match").AddEndpointFilter<SessionEndpointFilter>().WithProviderErrors();

        group.MapPost("/spotify", MatchTracksAsync)
            .WithName("MatchTracksToSpotify")
            .WithSummary("Match normalized tracks to Spotify")
            .WithDescription("Attempts to find Spotify matches for normalized tracks using exact, normalized, fuzzy, and album-based matching strategies.")
            .Accepts<MatchTracksRequest>("application/json")
            .Produces<MatchedDataResponse>(StatusCodes.Status200OK)
            .Produces<ApiError>(StatusCodes.Status400BadRequest)
            .Produces<ApiError>(StatusCodes.Status401Unauthorized)
            .Produces<ApiError>(StatusCodes.Status500InternalServerError);

        group.MapGet("/spotify/search", SearchTracksAsync)
            .WithName("SearchSpotifyTracks")
            .WithSummary("Search Spotify for tracks")
            .WithDescription("Searches Spotify for tracks matching the given query. Returns up to 10 results.")
            .Produces<List<SpotifyTrack>>(StatusCodes.Status200OK)
            .Produces<ApiError>(StatusCodes.Status400BadRequest)
            .Produces<ApiError>(StatusCodes.Status401Unauthorized)
            .Produces<ApiError>(StatusCodes.Status500InternalServerError);

        group.MapPost("/spotify/albums", MatchAlbumsAsync)
            .WithName("MatchAlbumsToSpotify")
            .WithSummary("Match normalized albums to Spotify")
            .WithDescription("Attempts to find Spotify matches for normalized albums and loads tracks from each matched album.")
            .Accepts<MatchAlbumsRequest>("application/json")
            .Produces<MatchedAlbumsResponse>(StatusCodes.Status200OK)
            .Produces<ApiError>(StatusCodes.Status400BadRequest)
            .Produces<ApiError>(StatusCodes.Status401Unauthorized)
            .Produces<ApiError>(StatusCodes.Status500InternalServerError);

        group.MapGet("/spotify/albums/search", SearchAlbumsAsync)
            .WithName("SearchSpotifyAlbums")
            .WithSummary("Search Spotify for albums")
            .WithDescription("Searches Spotify for albums matching the given query. Returns up to 5 results.")
            .Produces<List<SpotifyAlbumInfo>>(StatusCodes.Status200OK)
            .Produces<ApiError>(StatusCodes.Status400BadRequest)
            .Produces<ApiError>(StatusCodes.Status401Unauthorized)
            .Produces<ApiError>(StatusCodes.Status500InternalServerError);

        group.MapPost("/spotify/artists", MatchArtistsAsync)
            .WithName("MatchArtistsToSpotify")
            .WithSummary("Match normalized artists to Spotify")
            .WithDescription("Attempts to find Spotify matches for normalized artists and loads top tracks from each matched artist.")
            .Accepts<MatchArtistsRequest>("application/json")
            .Produces<MatchedArtistsResponse>(StatusCodes.Status200OK)
            .Produces<ApiError>(StatusCodes.Status400BadRequest)
            .Produces<ApiError>(StatusCodes.Status401Unauthorized)
            .Produces<ApiError>(StatusCodes.Status500InternalServerError);

        group.MapGet("/spotify/artists/search", SearchArtistsAsync)
            .WithName("SearchSpotifyArtists")
            .WithSummary("Search Spotify for artists")
            .WithDescription("Searches Spotify for artists matching the given query. Returns up to 5 results.")
            .Produces<List<SpotifyArtistInfo>>(StatusCodes.Status200OK)
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
        if (request.Tracks is null || request.Tracks.Count is 0 or > 500 ||
            request.Tracks.Any(track => track is null || !ValidText(track.Name) || !ValidText(track.Artist)))
        {
            return Results.BadRequest(new ApiError
            {
                Code = "INVALID_REQUEST",
                Message = "Tracks list cannot be null or empty."
            });
        }

        var (session, sessionError) = SessionAccess.Get(httpContext, sessionStore, requireSpotifyToken: true);
        if (sessionError is not null) return sessionError;

        try
        {
            var matchedData = await matchingService.MatchTracksAsync(
                request.Tracks, 
                session!.AccessToken,
                cancellationToken);

            return Results.Ok(matchedData);
        }
        catch (HttpRequestException ex)
        {
            return ProviderErrors.Spotify(ex);
        }
        catch (OperationCanceledException) when (cancellationToken.IsCancellationRequested) { throw; }
        catch (Exception)
        {
            return ApiErrorExtensions.InternalServerError("INTERNAL_SERVER_ERROR", "An unexpected error occurred while matching Spotify data.");
        }
    }

    /// <summary>
    /// Searches Spotify for tracks.
    /// </summary>
    /// <param name="query">Track/artist query string.</param>
    /// <param name="matchingService">Spotify matching service.</param>
    /// <param name="sessionStore">Session store to retrieve access token.</param>
    /// <param name="httpContext">HTTP context to get session ID from cookie.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>List of matching Spotify tracks.</returns>
    private static async Task<IResult> SearchTracksAsync(
        [FromQuery] string? query,
        [FromServices] ISpotifyMatchingService matchingService,
        [FromServices] ISessionStore sessionStore,
        HttpContext httpContext,
        CancellationToken cancellationToken)
    {
        // Validate query
        if (!ValidText(query))
        {
            return Results.BadRequest(new ApiError
            {
                Code = "INVALID_QUERY",
                Message = "Query cannot be empty."
            });
        }

        var (session, sessionError) = SessionAccess.Get(httpContext, sessionStore, requireSpotifyToken: true);
        if (sessionError is not null) return sessionError;

        try
        {
            var results = await matchingService.SearchTracksAsync(
                query,
                session!.AccessToken,
                cancellationToken);

            return Results.Ok(results);
        }
        catch (HttpRequestException ex)
        {
            return ProviderErrors.Spotify(ex);
        }
        catch (OperationCanceledException) when (cancellationToken.IsCancellationRequested) { throw; }
        catch (Exception)
        {
            return ApiErrorExtensions.InternalServerError("INTERNAL_SERVER_ERROR", "An unexpected error occurred while searching Spotify.");
        }
    }

    /// <summary>
    /// Matches normalized albums to Spotify and loads tracks from each album.
    /// </summary>
    /// <param name="request">Request containing normalized albums.</param>
    /// <param name="matchingService">Spotify matching service.</param>
    /// <param name="sessionStore">Session store to retrieve access token.</param>
    /// <param name="httpContext">HTTP context to get session ID from cookie.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>Matched albums with their tracks and confidence scores.</returns>
    private static async Task<IResult> MatchAlbumsAsync(
        [FromBody] MatchAlbumsRequest request,
        [FromServices] ISpotifyMatchingService matchingService,
        [FromServices] ISessionStore sessionStore,
        HttpContext httpContext,
        CancellationToken cancellationToken)
    {
        // Validate request
        if (request.Albums is null || request.Albums.Count is 0 or > 500 ||
            request.Albums.Any(album => album is null || !ValidText(album.Name) || !ValidText(album.Artist)))
        {
            return Results.BadRequest(new ApiError
            {
                Code = "INVALID_REQUEST",
                Message = "Albums list cannot be null or empty."
            });
        }

        var (session, sessionError) = SessionAccess.Get(httpContext, sessionStore, requireSpotifyToken: true);
        if (sessionError is not null) return sessionError;

        try
        {
            var matchedData = await matchingService.MatchAlbumsAsync(
                request.Albums,
                session!.AccessToken,
                cancellationToken);

            return Results.Ok(matchedData);
        }
        catch (HttpRequestException ex)
        {
            return ProviderErrors.Spotify(ex);
        }
        catch (OperationCanceledException) when (cancellationToken.IsCancellationRequested) { throw; }
        catch (Exception)
        {
            return ApiErrorExtensions.InternalServerError("INTERNAL_SERVER_ERROR", "An unexpected error occurred while matching Spotify data.");
        }
    }

    /// <summary>
    /// Searches Spotify for albums.
    /// </summary>
    /// <param name="query">Album/artist query string.</param>
    /// <param name="matchingService">Spotify matching service.</param>
    /// <param name="sessionStore">Session store to retrieve access token.</param>
    /// <param name="httpContext">HTTP context to get session ID from cookie.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>List of matching Spotify albums.</returns>
    private static async Task<IResult> SearchAlbumsAsync(
        [FromQuery] string? query,
        [FromServices] ISpotifyMatchingService matchingService,
        [FromServices] ISessionStore sessionStore,
        HttpContext httpContext,
        CancellationToken cancellationToken)
    {
        // Validate query
        if (!ValidText(query))
        {
            return Results.BadRequest(new ApiError
            {
                Code = "INVALID_QUERY",
                Message = "Query cannot be empty."
            });
        }

        var (session, sessionError) = SessionAccess.Get(httpContext, sessionStore, requireSpotifyToken: true);
        if (sessionError is not null) return sessionError;

        try
        {
            var results = await matchingService.SearchAlbumsAsync(
                query,
                session!.AccessToken,
                cancellationToken);

            return Results.Ok(results);
        }
        catch (HttpRequestException ex)
        {
            return ProviderErrors.Spotify(ex);
        }
        catch (OperationCanceledException) when (cancellationToken.IsCancellationRequested) { throw; }
        catch (Exception)
        {
            return ApiErrorExtensions.InternalServerError("INTERNAL_SERVER_ERROR", "An unexpected error occurred while searching Spotify.");
        }
    }

    /// <summary>
    /// Matches normalized artists to Spotify and loads top tracks from each artist.
    /// </summary>
    /// <param name="request">Request containing normalized artists.</param>
    /// <param name="matchingService">Spotify matching service.</param>
    /// <param name="sessionStore">Session store to retrieve access token.</param>
    /// <param name="httpContext">HTTP context to get session ID from cookie.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>Matched artists with their top tracks and confidence scores.</returns>
    private static async Task<IResult> MatchArtistsAsync(
        [FromBody] MatchArtistsRequest request,
        [FromServices] ISpotifyMatchingService matchingService,
        [FromServices] ISessionStore sessionStore,
        HttpContext httpContext,
        CancellationToken cancellationToken)
    {
        // Validate request
        if (request.Artists is null || request.Artists.Count is 0 or > 500 ||
            request.Artists.Any(artist => artist is null || !ValidText(artist.Name)))
        {
            return Results.BadRequest(new ApiError
            {
                Code = "INVALID_REQUEST",
                Message = "Artists list cannot be null or empty."
            });
        }

        var (session, sessionError) = SessionAccess.Get(httpContext, sessionStore, requireSpotifyToken: true);
        if (sessionError is not null) return sessionError;

        try
        {
            var matchedData = await matchingService.MatchArtistsAsync(
                request.Artists,
                session!.AccessToken,
                cancellationToken);

            return Results.Ok(matchedData);
        }
        catch (HttpRequestException ex)
        {
            return ProviderErrors.Spotify(ex);
        }
        catch (OperationCanceledException) when (cancellationToken.IsCancellationRequested) { throw; }
        catch (Exception)
        {
            return ApiErrorExtensions.InternalServerError("INTERNAL_SERVER_ERROR", "An unexpected error occurred while matching Spotify data.");
        }
    }

    /// <summary>
    /// Searches Spotify for artists.
    /// </summary>
    /// <param name="query">Artist query string.</param>
    /// <param name="matchingService">Spotify matching service.</param>
    /// <param name="sessionStore">Session store to retrieve access token.</param>
    /// <param name="httpContext">HTTP context to get session ID from cookie.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>List of matching Spotify artists.</returns>
    private static async Task<IResult> SearchArtistsAsync(
        [FromQuery] string? query,
        [FromServices] ISpotifyMatchingService matchingService,
        [FromServices] ISessionStore sessionStore,
        HttpContext httpContext,
        CancellationToken cancellationToken)
    {
        // Validate query
        if (!ValidText(query))
        {
            return Results.BadRequest(new ApiError
            {
                Code = "INVALID_QUERY",
                Message = "Query cannot be empty."
            });
        }

        var (session, sessionError) = SessionAccess.Get(httpContext, sessionStore, requireSpotifyToken: true);
        if (sessionError is not null) return sessionError;

        try
        {
            var results = await matchingService.SearchArtistsAsync(
                query,
                session!.AccessToken,
                cancellationToken);

            return Results.Ok(results);
        }
        catch (HttpRequestException ex)
        {
            return ProviderErrors.Spotify(ex);
        }
        catch (OperationCanceledException) when (cancellationToken.IsCancellationRequested) { throw; }
        catch (Exception)
        {
            return ApiErrorExtensions.InternalServerError("INTERNAL_SERVER_ERROR", "An unexpected error occurred while searching Spotify.");
        }
    }

    private static bool ValidText([System.Diagnostics.CodeAnalysis.NotNullWhen(true)] string? value) =>
        !string.IsNullOrWhiteSpace(value) && value.Length <= 500;
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

/// <summary>
/// Request to match normalized albums to Spotify.
/// </summary>
public sealed class MatchAlbumsRequest
{
    /// <summary>
    /// Normalized albums to match.
    /// </summary>
    public required List<NormalizedAlbum> Albums { get; init; }
}

/// <summary>
/// Request to match normalized artists to Spotify.
/// </summary>
public sealed class MatchArtistsRequest
{
    /// <summary>
    /// Normalized artists to match.
    /// </summary>
    public required List<NormalizedArtist> Artists { get; init; }
}
