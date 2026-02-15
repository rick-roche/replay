using System.Text.Json.Serialization;

namespace RePlay.Server.Models;

/// <summary>
/// Response from Discogs API when fetching user information.
/// </summary>
public sealed record DiscogsUserResponse
{
    [JsonPropertyName("username")]
    public required string Username { get; init; }

    [JsonPropertyName("name")]
    public string? Name { get; init; }

    [JsonPropertyName("location")]
    public string? Location { get; init; }

    [JsonPropertyName("profile")]
    public string? Profile { get; init; }

    [JsonPropertyName("uri")]
    public string? Uri { get; init; }

    [JsonPropertyName("resource_url")]
    public string? ResourceUrl { get; init; }

    [JsonPropertyName("num_lists")]
    public int? NumLists { get; init; }

    [JsonPropertyName("num_inventory")]
    public int? NumInventory { get; init; }

    [JsonPropertyName("num_collection")]
    public int? NumCollection { get; init; }
}

/// <summary>
/// Represents a release item in a user's collection from Discogs API.
/// </summary>
public sealed record DiscogsCollectionItem
{
    [JsonPropertyName("id")]
    public int Id { get; init; }

    [JsonPropertyName("instance_id")]
    public int InstanceId { get; init; }

    [JsonPropertyName("date_added")]
    public string? DateAdded { get; init; }

    [JsonPropertyName("basic_information")]
    public DiscogsReleaseBasicInfo? BasicInformation { get; init; }
}

/// <summary>
/// Basic information about a Discogs release.
/// </summary>
public sealed record DiscogsReleaseBasicInfo
{
    [JsonPropertyName("id")]
    public int Id { get; init; }

    [JsonPropertyName("title")]
    public string? Title { get; init; }

    [JsonPropertyName("year")]
    public int? Year { get; init; }

    [JsonPropertyName("artists")]
    public List<DiscogsArtistBasicInfo> Artists { get; init; } = [];

    [JsonPropertyName("formats")]
    public List<DiscogsFormat> Formats { get; init; } = [];
}

/// <summary>
/// Artist information from Discogs API.
/// </summary>
public sealed record DiscogsArtistBasicInfo
{
    [JsonPropertyName("name")]
    public string? Name { get; init; }

    [JsonPropertyName("resource_url")]
    public string? ResourceUrl { get; init; }
}

/// <summary>
/// Format information from Discogs API.
/// </summary>
public sealed record DiscogsFormat
{
    [JsonPropertyName("name")]
    public string? Name { get; init; }

    [JsonPropertyName("qty")]
    public string? Qty { get; init; }
}

/// <summary>
/// Represents a track in a Discogs release.
/// </summary>
public sealed record DiscogsReleaseTrack
{
    [JsonPropertyName("position")]
    public string? Position { get; init; }

    [JsonPropertyName("title")]
    public string? Title { get; init; }

    [JsonPropertyName("artists")]
    public List<DiscogsArtistBasicInfo> Artists { get; init; } = [];
}

/// <summary>
/// Full release information from Discogs API (obtained when fetching release details).
/// </summary>
public sealed record DiscogsReleaseDetail
{
    [JsonPropertyName("id")]
    public int Id { get; init; }

    [JsonPropertyName("title")]
    public string? Title { get; init; }

    [JsonPropertyName("year")]
    public int? Year { get; init; }

    [JsonPropertyName("artists")]
    public List<DiscogsArtistBasicInfo> Artists { get; init; } = [];

    [JsonPropertyName("formats")]
    public List<DiscogsFormat> Formats { get; init; } = [];

    [JsonPropertyName("tracklist")]
    public List<DiscogsReleaseTrack> Tracklist { get; init; } = [];
}

/// <summary>
/// Response from Discogs API when fetching user collection.
/// </summary>
public sealed record DiscogsCollectionResponse
{
    [JsonPropertyName("pagination")]
    public required DiscogsPagination Pagination { get; init; }

    [JsonPropertyName("releases")]
    public List<DiscogsCollectionItem> Releases { get; init; } = [];
}

/// <summary>
/// Pagination information from Discogs API responses.
/// </summary>
public sealed record DiscogsPagination
{
    [JsonPropertyName("per_page")]
    public int Per_Page { get; init; }

    [JsonPropertyName("items")]
    public required int Items { get; init; }

    [JsonPropertyName("page")]
    public int Page { get; init; }

    [JsonPropertyName("pages")]
    public int Pages { get; init; }
}
