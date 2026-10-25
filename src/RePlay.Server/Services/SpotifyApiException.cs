using System.Net;

namespace RePlay.Server.Services;

/// <summary>A Spotify failure with a safe message and the original provider status.</summary>
public sealed class SpotifyApiException(
    HttpStatusCode statusCode,
    string errorCode = "SPOTIFY_API_ERROR",
    string message = "Spotify could not complete the request.") : HttpRequestException(message, null, statusCode)
{
    public string ErrorCode { get; } = errorCode;
}
