using System.Net;
using Microsoft.AspNetCore.Mvc;
using RePlay.Server.Models;
using RePlay.Server.Services;

namespace RePlay.Server.Endpoints;

internal static class ProviderErrors
{
    public static RouteGroupBuilder WithProviderErrors(this RouteGroupBuilder group) => group.WithMetadata(
        new ProducesResponseTypeAttribute(typeof(ApiError), 401),
        new ProducesResponseTypeAttribute(typeof(ApiError), 403),
        new ProducesResponseTypeAttribute(typeof(ApiError), 429),
        new ProducesResponseTypeAttribute(typeof(ApiError), 502));

    public static IResult Spotify(HttpRequestException error) => error is SpotifyApiException { ErrorCode: "PLAYLIST_CREATION_UNCONFIRMED" }
        ? Results.Json(new ApiError { Code = "PLAYLIST_CREATION_UNCONFIRMED", Message = "Spotify may have created the playlist. Check your Spotify library before trying again." }, statusCode: 502)
        : error is SpotifyApiException { ErrorCode: "SPOTIFY_TOP_TRACKS_UNSUPPORTED" }
        ? Results.Json(new ApiError { Code = "SPOTIFY_TOP_TRACKS_UNSUPPORTED", Message = "Spotify artist top tracks require Extended Quota Mode. Choose tracks or albums instead." }, statusCode: 403)
        : error.StatusCode switch
    {
        HttpStatusCode.Unauthorized => ApiErrorExtensions.Unauthorized("SPOTIFY_TOKEN_EXPIRED", "Refresh your Spotify access token and try again"),
        HttpStatusCode.TooManyRequests => Results.Json(new ApiError { Code = "SPOTIFY_RATE_LIMITED", Message = "Spotify is rate limiting requests. Please try again later." }, statusCode: 429),
        HttpStatusCode.Forbidden or HttpStatusCode.NotFound => Results.Json(new ApiError { Code = "SPOTIFY_FEATURE_UNAVAILABLE", Message = "This Spotify feature is unavailable for your account or application quota mode." }, statusCode: 403),
        _ => ApiErrorExtensions.ServiceUnavailable("SPOTIFY_API_ERROR", "Spotify is temporarily unavailable. Please try again later.")
    };

    public static IResult Source(string source, HttpRequestException error) => error.StatusCode == HttpStatusCode.TooManyRequests
        ? Results.Json(new ApiError { Code = $"{source}_RATE_LIMITED", Message = "The music provider is rate limiting requests. Please try again later." }, statusCode: 429)
        : ApiErrorExtensions.ServiceUnavailable($"{source}_API_ERROR", "The music provider is temporarily unavailable. Please try again later.");
}
