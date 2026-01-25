namespace RePlay.Server.Models;

/// <summary>
/// Minimal Setlist.fm user profile returned from the public API.
/// </summary>
public sealed record SetlistUser
{
    public required string UserId { get; init; }
    public required string DisplayName { get; init; }
    public string? Url { get; init; }
    public int AttendedConcerts { get; init; }
}

/// <summary>
/// Request payload to configure a Setlist.fm profile.
/// </summary>
public sealed record ConfigureSetlistRequest
{
    /// <summary>
    /// Username or numeric ID in Setlist.fm.
    /// </summary>
    public required string UsernameOrId { get; init; }
}

/// <summary>
/// Response containing Setlist.fm configuration metadata.
/// </summary>
public sealed record ConfigureSetlistResponse
{
    public required string UserId { get; init; }
    public required string DisplayName { get; init; }
    public string? ProfileUrl { get; init; }
    public int AttendedConcerts { get; init; }
    public required bool IsConfigured { get; init; }
}
