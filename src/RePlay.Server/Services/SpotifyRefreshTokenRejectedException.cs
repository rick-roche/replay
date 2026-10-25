using System.Net;

namespace RePlay.Server.Services;

/// <summary>Spotify explicitly rejected a refresh grant; reauthentication is required.</summary>
public sealed class SpotifyRefreshTokenRejectedException()
    : HttpRequestException("Spotify rejected the refresh grant", null, HttpStatusCode.BadRequest);
