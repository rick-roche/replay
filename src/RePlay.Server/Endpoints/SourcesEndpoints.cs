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

        lastfm.MapPost("/data", PostFetchLastfmData)
            .WithName("FetchLastfmData")
            .WithSummary("Fetch Last.fm data with filters")
            .WithDescription("Fetches tracks, albums, or artists from a user's Last.fm profile with optional filtering (time period, max results, custom date ranges).")
            .Accepts<FetchLastfmDataRequest>("application/json")
            .Produces<LastfmDataResponse>(StatusCodes.Status200OK)
            .Produces<ApiError>(StatusCodes.Status400BadRequest, "application/json")
            .Produces<ApiError>(StatusCodes.Status500InternalServerError, "application/json");

        lastfm.MapPost("/data/normalized", PostFetchLastfmDataNormalized)
            .WithName("FetchLastfmDataNormalized")
            .WithSummary("Fetch normalized Last.fm data")
            .WithDescription("Fetches Last.fm data and normalizes it to a canonical format for consistent matching across all data sources.")
            .Accepts<FetchLastfmDataRequest>("application/json")
            .Produces<NormalizedDataResponse>(StatusCodes.Status200OK)
            .Produces<ApiError>(StatusCodes.Status400BadRequest, "application/json")
            .Produces<ApiError>(StatusCodes.Status500InternalServerError, "application/json");

        var setlistfm = sources.MapGroup("/setlistfm");

        setlistfm.MapPost("/data", PostFetchSetlistFmData)
            .WithName("FetchSetlistFmData")
            .WithSummary("Fetch Setlist.fm concert data with filters")
            .WithDescription("Fetches attended concerts from a user's Setlist.fm profile with optional filtering (date range, max concerts, max tracks). Tracks are deduplicated.")
            .Accepts<FetchSetlistFmDataRequest>("application/json")
            .Produces<SetlistFmDataResponse>(StatusCodes.Status200OK)
            .Produces<ApiError>(StatusCodes.Status400BadRequest, "application/json")
            .Produces<ApiError>(StatusCodes.Status500InternalServerError, "application/json");

        setlistfm.MapPost("/data/normalized", PostFetchSetlistFmDataNormalized)
            .WithName("FetchSetlistFmDataNormalized")
            .WithSummary("Fetch normalized Setlist.fm concert data")
            .WithDescription("Fetches Setlist.fm concert data and normalizes it to a canonical format for consistent matching across all data sources.")
            .Accepts<FetchSetlistFmDataRequest>("application/json")
            .Produces<NormalizedDataResponse>(StatusCodes.Status200OK)
            .Produces<ApiError>(StatusCodes.Status400BadRequest, "application/json")
            .Produces<ApiError>(StatusCodes.Status500InternalServerError, "application/json");

        return group;
    }

    /// <summary>
    /// Fetch Last.fm data (tracks, albums, or artists) with specified filters.
    /// </summary>
    /// <param name="request">Contains username and filter options (data type, time period, max results, custom dates).</param>
    /// <param name="lastfmService">Service for Last.fm API interactions.</param>
    /// <param name="httpContext">HTTP context.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>
    /// 200 OK with LastfmDataResponse containing matched tracks, albums, or artists.
    /// 400 Bad Request if request validation fails or fetch fails.
    /// 500 Internal Server Error if an unexpected error occurs.
    /// </returns>
    private static async Task<IResult> PostFetchLastfmData(
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
            // Fetch data from Last.fm
            var data = await lastfmService.GetUserDataAsync(request.Username, request.Filter, cancellationToken);
            
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
    /// Fetch Setlist.fm concert data with specified filters.
    /// </summary>
    /// <param name="request">Contains user ID and filter options (date range, max concerts, max tracks).</param>
    /// <param name="setlistFmService">Service for Setlist.fm API interactions.</param>
    /// <param name="httpContext">HTTP context.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>
    /// 200 OK with SetlistFmDataResponse containing attended concerts and deduplicated tracks.
    /// 400 Bad Request if request validation fails or fetch fails.
    /// 500 Internal Server Error if an unexpected error occurs.
    /// </returns>
    private static async Task<IResult> PostFetchSetlistFmData(
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

        // Validate date range if specified
        if (!string.IsNullOrWhiteSpace(request.Filter.StartDate) && 
            !string.IsNullOrWhiteSpace(request.Filter.EndDate))
        {
            if (!DateTime.TryParse(request.Filter.StartDate, out var startDate) ||
                !DateTime.TryParse(request.Filter.EndDate, out var endDate))
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
            // Fetch data from Setlist.fm
            var data = await setlistFmService.GetUserConcertsAsync(request.UserId, request.Filter, cancellationToken);
            
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
                "SETLISTFM_FETCH_ERROR",
                "Error fetching Setlist.fm data",
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

        // Validate date range if specified
        if (!string.IsNullOrWhiteSpace(request.Filter.StartDate) && 
            !string.IsNullOrWhiteSpace(request.Filter.EndDate))
        {
            if (!DateTime.TryParse(request.Filter.StartDate, out var startDate) ||
                !DateTime.TryParse(request.Filter.EndDate, out var endDate))
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
            // Fetch and normalize data from Setlist.fm
            var data = await setlistFmService.GetUserConcertsNormalizedAsync(request.UserId, request.Filter, cancellationToken);
            
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
                "SETLISTFM_FETCH_ERROR",
                "Error fetching Setlist.fm data",
                ex.Message);
        }
    }
}
