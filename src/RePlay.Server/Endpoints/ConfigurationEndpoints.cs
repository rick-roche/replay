using Microsoft.AspNetCore.Mvc;
using RePlay.Server.Endpoints;
using RePlay.Server.Models;
using RePlay.Server.Services;

namespace RePlay.Server.Endpoints;

/// <summary>
/// Configuration endpoints for external data sources.
/// </summary>
public static class ConfigurationEndpoints
{
    public static RouteGroupBuilder MapConfigurationEndpoints(this RouteGroupBuilder group)
    {
        var config = group.MapGroup("/config");
        config.MapPost("/lastfm", PostConfigureLastfm);
        config.MapGet("/lastfm", GetLastfmConfig);
        config.MapPost("/lastfm/data", PostFetchLastfmData);

        return group;
    }

    /// <summary>
    /// Configure or validate a Last.fm username.
    /// </summary>
    private static async Task<IResult> PostConfigureLastfm(
        [FromBody] ConfigureLastfmRequest request,
        ILastfmService lastfmService,
        ISessionStore sessionStore,
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

        // Get current session
        if (!httpContext.Request.Cookies.TryGetValue("replay_session_id", out var sessionId))
        {
            return ApiErrorExtensions.Unauthorized(
                "NO_SESSION",
                "No active session found");
        }

        var session = sessionStore.GetSession(sessionId);
        if (session == null)
        {
            return ApiErrorExtensions.Unauthorized(
                "INVALID_SESSION",
                "Session not found or has been invalidated");
        }

        try
        {
            // Validate username with Last.fm
            var user = await lastfmService.GetUserAsync(request.Username, cancellationToken);
            if (user == null)
            {
                return ApiErrorExtensions.BadRequest(
                    "INVALID_LASTFM_USER",
                    "Invalid Last.fm username or user not found");
            }

            // Store configuration in session (in-memory for now)
            // TODO: Persist to database
            httpContext.Items["lastfm_config"] = new ExternalSourceConfig
            {
                Source = "lastfm",
                ConfigValue = request.Username,
                ConfiguredAt = DateTime.UtcNow
            };

            var response = new ConfigureLastfmResponse
            {
                Username = user.Username,
                PlayCount = user.PlayCount,
                IsConfigured = true
            };

            return Results.Ok(response);
        }
        catch (Exception ex)
        {
            return ApiErrorExtensions.InternalServerError(
                "LASTFM_CONFIG_ERROR",
                "Error configuring Last.fm",
                ex.Message);
        }
    }

    /// <summary>
    /// Get the current Last.fm configuration.
    /// </summary>
    private static IResult GetLastfmConfig(HttpContext httpContext)
    {
        // Get current session
        if (!httpContext.Request.Cookies.TryGetValue("replay_session_id", out var sessionId))
        {
            return ApiErrorExtensions.Unauthorized(
                "NO_SESSION",
                "No active session found");
        }

        // Check if configuration is in session context
        if (httpContext.Items.TryGetValue("lastfm_config", out var config) && config is ExternalSourceConfig sourceConfig)
        {
            return Results.Ok(new ConfigureLastfmResponse
            {
                Username = sourceConfig.ConfigValue,
                PlayCount = 0, // TODO: fetch from Last.fm
                IsConfigured = true
            });
        }

        return Results.Ok(new ConfigureLastfmResponse
        {
            Username = "",
            PlayCount = 0,
            IsConfigured = false
        });
    }

    /// <summary>
    /// Fetch Last.fm data (tracks, albums, or artists) with specified filters.
    /// </summary>
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
    }}