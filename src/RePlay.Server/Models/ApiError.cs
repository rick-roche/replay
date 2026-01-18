namespace RePlay.Server.Models;

/// <summary>
/// Standard API error response with machine-readable error code.
/// </summary>
public sealed record ApiError
{
    /// <summary>
    /// Machine-readable error code.
    /// </summary>
    public required string Code { get; init; }

    /// <summary>
    /// Human-readable error message.
    /// </summary>
    public required string Message { get; init; }

    /// <summary>
    /// Optional additional details about the error.
    /// </summary>
    public string? Details { get; init; }
}

/// <summary>
/// Helper extensions for creating consistent error responses.
/// </summary>
public static class ApiErrorExtensions
{
    public static IResult BadRequest(string code, string message, string? details = null)
    {
        return Results.BadRequest(new ApiError
        {
            Code = code,
            Message = message,
            Details = details
        });
    }

    public static IResult Unauthorized(string code, string message, string? details = null)
    {
        return Results.Json(new ApiError
        {
            Code = code,
            Message = message,
            Details = details
        }, statusCode: 401);
    }

    public static IResult InternalServerError(string code, string message, string? details = null)
    {
        return Results.Json(new ApiError
        {
            Code = code,
            Message = message,
            Details = details
        }, statusCode: 500);
    }

    public static IResult ServiceUnavailable(string code, string message, string? details = null)
    {
        return Results.Json(new ApiError
        {
            Code = code,
            Message = message,
            Details = details
        }, statusCode: 502);
    }
}
