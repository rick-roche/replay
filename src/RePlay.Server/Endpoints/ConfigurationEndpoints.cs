using Microsoft.AspNetCore.Mvc;
using RePlay.Server.Endpoints;
using RePlay.Server.Models;
using RePlay.Server.Services;

namespace RePlay.Server.Endpoints;

/// <summary>
/// Configuration endpoints for user accounts and external data sources.
/// </summary>
public static class ConfigurationEndpoints
{
    public static RouteGroupBuilder MapConfigurationEndpoints(this RouteGroupBuilder group)
    {
        var config = group.MapGroup("/config");
        
        config.MapPost("/lastfm", PostConfigureLastfm)
            .WithName("ConfigureLastfm")
            .WithSummary("Configure or validate a Last.fm username")
            .WithDescription("Validates the provided Last.fm username and stores the configuration in the user session.")
            .Accepts<ConfigureLastfmRequest>("application/json")
            .Produces<ConfigureLastfmResponse>(StatusCodes.Status200OK)
            .Produces<ApiError>(StatusCodes.Status400BadRequest, "application/json")
            .Produces<ApiError>(StatusCodes.Status401Unauthorized, "application/json")
            .Produces<ApiError>(StatusCodes.Status500InternalServerError, "application/json");

        return group;
    }

    /// <summary>
    /// Configure or validate a Last.fm username.
    /// </summary>
    /// <param name="request">Contains the Last.fm username to configure.</param>
    /// <param name="lastfmService">Service for Last.fm API interactions.</param>
    /// <param name="sessionStore">Session storage for authenticated users.</param>
    /// <param name="httpContext">HTTP context with session cookies.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>
    /// 200 OK with ConfigureLastfmResponse if username is valid.
    /// 400 Bad Request if username is missing or invalid.
    /// 401 Unauthorized if no active session.
    /// 500 Internal Server Error if an unexpected error occurs.
    /// </returns>
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
}