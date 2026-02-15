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

        setlistfm.MapPost("/data", PostFetchSetlistFmDataNormalized)
            .WithName("FetchSetlistFmData")
            .WithSummary("Fetch normalized Setlist.fm concert data")
            .WithDescription("Fetches Setlist.fm concert data and normalizes it to a canonical format for consistent matching across all data sources.")
            .Accepts<FetchSetlistFmDataRequest>("application/json")
            .Produces<NormalizedDataResponse>(StatusCodes.Status200OK)
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
