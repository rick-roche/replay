using System.Globalization;
using Microsoft.AspNetCore.Mvc;
using RePlay.Server.Models;
using RePlay.Server.Services;

namespace RePlay.Server.Endpoints;

/// <summary>
/// Endpoints for fetching data from external music sources (Last.fm, Discogs, Setlist.fm).
/// </summary>
public static class SourcesEndpoints
{
    public static RouteGroupBuilder MapSourcesEndpoints(this RouteGroupBuilder group)
    {
        var sources = group.MapGroup("/sources");
        var lastfm = sources.MapGroup("/lastfm");

        lastfm.MapPost("/data", PostFetchLastfmDataNormalized)
            .WithName("FetchLastfmData")
            .WithSummary("Fetch normalized Last.fm data")
            .WithDescription("Fetches Last.fm data and normalizes it to a canonical format for consistent matching across all data sources.")
            .Accepts<FetchLastfmDataRequest>("application/json")
            .Produces<NormalizedDataResponse>(StatusCodes.Status200OK)
            .Produces<ApiError>(StatusCodes.Status400BadRequest, "application/json")
            .Produces<ApiError>(StatusCodes.Status500InternalServerError, "application/json");

        var discogs = sources.MapGroup("/discogs");

        discogs.MapPost("/data", PostFetchDiscogsDataNormalized)
            .WithName("FetchDiscogsData")
            .WithSummary("Fetch normalized Discogs collection data")
            .WithDescription("Fetches Discogs collection data and normalizes it to a canonical format for consistent matching across all data sources.")
            .Accepts<FetchDiscogsDataRequest>("application/json")
            .Produces<NormalizedDataResponse>(StatusCodes.Status200OK)
            .Produces<ApiError>(StatusCodes.Status400BadRequest, "application/json")
            .Produces<ApiError>(StatusCodes.Status500InternalServerError, "application/json");

        var setlistfm = sources.MapGroup("/setlistfm");
        setlistfm.AddEndpointFilter(async (context, next) =>
        {
            var httpContext = context.HttpContext;
            if (!httpContext.Request.Cookies.TryGetValue("replay_session_id", out var sessionId) ||
                string.IsNullOrWhiteSpace(sessionId))
            {
                return ApiErrorExtensions.Unauthorized("NO_SESSION", "No active session found");
            }

            var sessionStore = httpContext.RequestServices.GetRequiredService<ISessionStore>();
            var session = sessionStore.GetSession(sessionId);
            if (session is null)
            {
                return ApiErrorExtensions.Unauthorized("INVALID_SESSION", "Session not found or has been invalidated");
            }

            if (session.IsExpired())
            {
                sessionStore.RemoveSession(sessionId);
                httpContext.Response.Cookies.Delete("replay_session_id");
                return ApiErrorExtensions.Unauthorized("SESSION_EXPIRED", "Session has expired");
            }

            return await next(context);
        });

        setlistfm.MapPost("/data", PostFetchSetlistFmDataNormalized)
            .WithName("FetchSetlistFmData")
            .WithSummary("Fetch normalized Setlist.fm concert data")
            .WithDescription("Fetches Setlist.fm concert data and normalizes it to a canonical format for consistent matching across all data sources.")
            .Accepts<FetchSetlistFmDataRequest>("application/json")
            .Produces<NormalizedDataResponse>(StatusCodes.Status200OK)
            .Produces<ApiError>(StatusCodes.Status400BadRequest, "application/json")
            .Produces<ApiError>(StatusCodes.Status500InternalServerError, "application/json");

        setlistfm.MapPost("/concerts", PostFetchSetlistFmConcerts)
            .WithName("FetchSetlistFmConcerts")
            .WithSummary("Fetch a provider page of Setlist.fm concerts")
            .WithDescription("Fetches one attended-concert provider page. Date filters apply to concerts returned on that page.")
            .Accepts<FetchSetlistFmConcertsRequest>("application/json")
            .Produces<SetlistConcertsResponse>(StatusCodes.Status200OK)
            .Produces<ApiError>(StatusCodes.Status400BadRequest, "application/json")
            .Produces<ApiError>(StatusCodes.Status500InternalServerError, "application/json");

        return group;
    }

    /// <summary>
    /// Fetch Last.fm data and normalize it to canonical format for matching.
    /// </summary>
    /// <param name="request">Contains username and filter options (data type, time period, max results, custom dates).</param>
    /// <param name="lastfmService">Service for Last.fm API interactions.</param>
    /// <param name="httpContext">HTTP context.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>
    /// 200 OK with NormalizedDataResponse containing normalized tracks, albums, or artists with preserved source metadata.
    /// 400 Bad Request if request validation fails or fetch fails.
    /// 500 Internal Server Error if an unexpected error occurs.
    /// </returns>
    private static async Task<IResult> PostFetchLastfmDataNormalized(
        [FromBody] FetchLastfmDataRequest request,
        ILastfmService lastfmService,
        HttpContext httpContext,
        CancellationToken cancellationToken)
    {
        // Validate request
        if (string.IsNullOrWhiteSpace(request.Username))
        {
            return ApiErrorExtensions.BadRequest(
                "MISSING_USERNAME",
                "Username is required");
        }

        if (request.Filter == null)
        {
            return ApiErrorExtensions.BadRequest(
                "MISSING_FILTER",
                "Filter is required");
        }

        // Validate custom date range if specified
        if (request.Filter.TimePeriod == LastfmTimePeriod.Custom)
        {
            if (string.IsNullOrWhiteSpace(request.Filter.CustomStartDate) || 
                string.IsNullOrWhiteSpace(request.Filter.CustomEndDate))
            {
                return ApiErrorExtensions.BadRequest(
                    "INVALID_CUSTOM_DATES",
                    "Custom time period requires both start and end dates");
            }

            if (!DateTime.TryParse(request.Filter.CustomStartDate, out var startDate) ||
                !DateTime.TryParse(request.Filter.CustomEndDate, out var endDate))
            {
                return ApiErrorExtensions.BadRequest(
                    "INVALID_DATE_FORMAT",
                    "Dates must be in valid ISO 8601 format");
            }

            if (startDate > endDate)
            {
                return ApiErrorExtensions.BadRequest(
                    "INVALID_DATE_RANGE",
                    "Start date must be before end date");
            }
        }

        try
        {
            // Fetch and normalize data from Last.fm
            var data = await lastfmService.GetUserDataNormalizedAsync(request.Username, request.Filter, cancellationToken);
            
            if (data == null)
            {
                return ApiErrorExtensions.BadRequest(
                    "LASTFM_FETCH_FAILED",
                    "Failed to fetch data from Last.fm");
            }

            return Results.Ok(data);
        }
        catch (ArgumentException ex)
        {
            return ApiErrorExtensions.BadRequest(
                "INVALID_FILTER",
                ex.Message);
        }
        catch (Exception ex)
        {
            return ApiErrorExtensions.InternalServerError(
                "LASTFM_FETCH_ERROR",
                "Error fetching Last.fm data",
                ex.Message);
        }
    }

    /// <summary>
    /// Fetch Discogs collection data and normalize it to canonical format for matching.
    /// </summary>
    /// <param name="request">Contains username/collection ID and filter options (release year range, media format, year added, max results).</param>
    /// <param name="discogsService">Service for Discogs API interactions.</param>
    /// <param name="httpContext">HTTP context.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>
    /// 200 OK with NormalizedDataResponse containing normalized tracks with preserved source metadata.
    /// 400 Bad Request if request validation fails or fetch fails.
    /// 500 Internal Server Error if an unexpected error occurs.
    /// </returns>
    private static async Task<IResult> PostFetchDiscogsDataNormalized(
        [FromBody] FetchDiscogsDataRequest request,
        IDiscogsService discogsService,
        HttpContext httpContext,
        CancellationToken cancellationToken)
    {
        // Validate request
        if (string.IsNullOrWhiteSpace(request.UsernameOrCollectionId))
        {
            return ApiErrorExtensions.BadRequest(
                "MISSING_USERNAME",
                "Username or collection ID is required");
        }

        if (request.Filter == null)
        {
            return ApiErrorExtensions.BadRequest(
                "MISSING_FILTER",
                "Filter is required");
        }

        // Validate year ranges if specified
        if ((request.Filter.MinReleaseYear.HasValue || request.Filter.MaxReleaseYear.HasValue) &&
            request.Filter.MinReleaseYear.HasValue && request.Filter.MaxReleaseYear.HasValue &&
            request.Filter.MinReleaseYear > request.Filter.MaxReleaseYear)
        {
            return ApiErrorExtensions.BadRequest(
                "INVALID_YEAR_RANGE",
                "Minimum release year must be less than or equal to maximum release year");
        }

        if ((request.Filter.MinYearAdded.HasValue || request.Filter.MaxYearAdded.HasValue) &&
            request.Filter.MinYearAdded.HasValue && request.Filter.MaxYearAdded.HasValue &&
            request.Filter.MinYearAdded > request.Filter.MaxYearAdded)
        {
            return ApiErrorExtensions.BadRequest(
                "INVALID_YEAR_ADDED_RANGE",
                "Minimum year added must be less than or equal to maximum year added");
        }

        try
        {
            // Fetch and normalize data from Discogs
            var data = await discogsService.GetCollectionNormalizedAsync(request.UsernameOrCollectionId, request.Filter, cancellationToken);
            
            if (data == null)
            {
                return ApiErrorExtensions.BadRequest(
                    "DISCOGS_FETCH_FAILED",
                    "Failed to fetch data from Discogs");
            }

            return Results.Ok(data);
        }
        catch (ArgumentException ex)
        {
            return ApiErrorExtensions.BadRequest(
                "INVALID_FILTER",
                ex.Message);
        }
        catch (Exception ex)
        {
            return ApiErrorExtensions.InternalServerError(
                "DISCOGS_FETCH_ERROR",
                "Error fetching Discogs data",
                ex.Message);
        }
    }

    /// <summary>
    /// Fetch Setlist.fm concert data and normalize it to canonical format for matching.
    /// </summary>
    /// <param name="request">Contains user ID and filter options (date range, max concerts, max tracks).</param>
    /// <param name="setlistFmService">Service for Setlist.fm API interactions.</param>
    /// <param name="httpContext">HTTP context.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>
    /// 200 OK with NormalizedDataResponse containing normalized tracks with preserved source metadata.
    /// 400 Bad Request if request validation fails or fetch fails.
    /// 500 Internal Server Error if an unexpected error occurs.
    /// </returns>
    private static async Task<IResult> PostFetchSetlistFmDataNormalized(
        [FromBody] FetchSetlistFmDataRequest request,
        ISetlistFmService setlistFmService,
        HttpContext httpContext,
        CancellationToken cancellationToken)
    {
        // Validate request
        if (string.IsNullOrWhiteSpace(request.UserId))
        {
            return ApiErrorExtensions.BadRequest(
                "MISSING_USER_ID",
                "User ID is required");
        }

        if (request.Filter == null)
        {
            return ApiErrorExtensions.BadRequest(
                "MISSING_FILTER",
                "Filter is required");
        }

        if (ValidateSetlistFmDateRange(request.Filter) is { } dateValidationError)
        {
            return dateValidationError;
        }

        if (request.SelectedConcertIds != null && request.SelectedConcertIds.Count == 0)
        {
            return ApiErrorExtensions.BadRequest(
                "EMPTY_CONCERT_SELECTION",
                "SelectedConcertIds must contain at least one id when provided");
        }

        try
        {
            // Fetch and normalize data from Setlist.fm
            var data = await setlistFmService.GetUserConcertsNormalizedAsync(
                request.UserId,
                request.Filter,
                request.SelectedConcertIds,
                cancellationToken);
            
            return Results.Ok(data);
        }
        catch (ArgumentException ex)
        {
            return ApiErrorExtensions.BadRequest(
                "INVALID_FILTER",
                ex.Message);
        }
        catch (HttpRequestException ex)
        {
            return ApiErrorExtensions.ServiceUnavailable(
                "SETLISTFM_API_ERROR",
                "Failed to communicate with Setlist.fm",
                ex.Message);
        }
        catch (Exception ex)
        {
            return ApiErrorExtensions.InternalServerError(
                "SETLISTFM_FETCH_ERROR",
                "Error fetching Setlist.fm data",
                ex.Message);
        }
    }

    /// <summary>
    /// Fetch a paginated list of Setlist.fm concerts for user selection.
    /// </summary>
    private static async Task<IResult> PostFetchSetlistFmConcerts(
        [FromBody] FetchSetlistFmConcertsRequest request,
        ISetlistFmService setlistFmService,
        HttpContext httpContext,
        CancellationToken cancellationToken)
    {
        if (string.IsNullOrWhiteSpace(request.UserId))
        {
            return ApiErrorExtensions.BadRequest(
                "MISSING_USER_ID",
                "User ID is required");
        }

        if (request.Filter == null)
        {
            return ApiErrorExtensions.BadRequest(
                "MISSING_FILTER",
                "Filter is required");
        }

        if (request.PageNumber < 1)
        {
            return ApiErrorExtensions.BadRequest(
                "INVALID_PAGE_NUMBER",
                "Page number must be at least 1");
        }

        if (ValidateSetlistFmDateRange(request.Filter) is { } dateValidationError)
        {
            return dateValidationError;
        }

        try
        {
            var data = await setlistFmService.GetUserConcertsPageAsync(
                request.UserId,
                request.Filter,
                request.PageNumber,
                20,
                cancellationToken);

            return Results.Ok(data);
        }
        catch (ArgumentException ex)
        {
            return ApiErrorExtensions.BadRequest(
                "INVALID_FILTER",
                ex.Message);
        }
        catch (HttpRequestException ex)
        {
            return ApiErrorExtensions.ServiceUnavailable(
                "SETLISTFM_API_ERROR",
                "Failed to communicate with Setlist.fm",
                ex.Message);
        }
        catch (Exception ex)
        {
            return ApiErrorExtensions.InternalServerError(
                "SETLISTFM_FETCH_ERROR",
                "Error fetching Setlist.fm concerts",
                ex.Message);
        }
    }

    private static IResult? ValidateSetlistFmDateRange(SetlistFmFilter filter)
    {
        DateTime? startDate = null;
        DateTime? endDate = null;

        if (!string.IsNullOrWhiteSpace(filter.StartDate))
        {
            if (!DateTime.TryParseExact(filter.StartDate, "yyyy-MM-dd", CultureInfo.InvariantCulture,
                    DateTimeStyles.None, out var parsedStartDate))
            {
                return ApiErrorExtensions.BadRequest(
                    "INVALID_DATE_FORMAT",
                    "Dates must be in valid ISO 8601 format");
            }

            startDate = parsedStartDate;
        }

        if (!string.IsNullOrWhiteSpace(filter.EndDate))
        {
            if (!DateTime.TryParseExact(filter.EndDate, "yyyy-MM-dd", CultureInfo.InvariantCulture,
                    DateTimeStyles.None, out var parsedEndDate))
            {
                return ApiErrorExtensions.BadRequest(
                    "INVALID_DATE_FORMAT",
                    "Dates must be in valid ISO 8601 format");
            }

            endDate = parsedEndDate;
        }

        if (startDate > endDate)
        {
            return ApiErrorExtensions.BadRequest(
                "INVALID_DATE_RANGE",
                "Start date must be before end date");
        }

        return null;
    }
}
