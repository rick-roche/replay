using Microsoft.AspNetCore.Mvc;
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
            return Results.BadRequest(new { error = "Username is required" });
        }

        // Get current session
        if (!httpContext.Request.Cookies.TryGetValue("replay_session_id", out var sessionId))
        {
            return Results.Unauthorized();
        }

        var session = sessionStore.GetSession(sessionId);
        if (session == null)
        {
            return Results.Unauthorized();
        }

        try
        {
            // Validate username with Last.fm
            var user = await lastfmService.GetUserAsync(request.Username, cancellationToken);
            if (user == null)
            {
                return Results.BadRequest(new { error = "Invalid Last.fm username or user not found" });
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
            return Results.Problem(
                detail: ex.Message,
                statusCode: 500,
                title: "Error configuring Last.fm");
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
            return Results.Unauthorized();
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
}
