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
/// Response from Discogs API when fetching user collection.
/// </summary>
public sealed record DiscogsCollectionResponse
{
    [JsonPropertyName("pagination")]
    public required DiscogsPagination Pagination { get; init; }
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
