using System.Net;
using System.Net.Http.Json;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Hosting.Server;
using Microsoft.AspNetCore.Hosting.Server.Features;
using Microsoft.Extensions.DependencyInjection;
using NSubstitute;
using RePlay.Server;
using RePlay.Server.Endpoints;
using RePlay.Server.Models;
using RePlay.Server.Services;

namespace RePlay.Server.Tests.Endpoints;

public sealed class HttpBoundaryTests
{
    [Theory]
    [InlineData("/api/sources/lastfm/data", "{\"username\":\"listener\",\"filter\":{\"dataType\":0,\"timePeriod\":0}}")]
    [InlineData("/api/sources/discogs/data", "{\"usernameOrCollectionId\":\"collector\",\"filter\":{}}")]
    public async Task SourceRequests_WithoutSession_ReturnJsonUnauthorized(string path, string body)
    {
        await using var api = await RunningApi.StartAsync();
        using var response = await api.Client.PostAsync(path, new StringContent(body, System.Text.Encoding.UTF8, "application/json"));

        Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
        Assert.Equal("NO_SESSION", (await response.Content.ReadFromJsonAsync<ApiError>())!.Code);
    }

    [Fact]
    public async Task MatchingRoute_UsesSingleApiPrefix()
    {
        await using var api = await RunningApi.StartAsync();
        using var response = await api.Client.PostAsJsonAsync("/api/match/spotify", new { tracks = new[] { new { name = "Song", artist = "Artist", source = "lastfm", sourceMetadata = new { } } } });

        Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
        Assert.Equal("NO_SESSION", (await response.Content.ReadFromJsonAsync<ApiError>())!.Code);
    }

    [Theory]
    [InlineData("/api/match/spotify", "{\"tracks\":[null]}")]
    [InlineData("/api/match/spotify/albums", "{\"albums\":[null]}")]
    [InlineData("/api/match/spotify/artists", "{\"artists\":[null]}")]
    [InlineData("/api/playlist/create", "{\"name\":\"Playlist\",\"isPublic\":false,\"trackUris\":[null]}")]
    [InlineData("/api/sources/discogs/data", "{\"usernameOrCollectionId\":\"collector\",\"filter\":{\"maxTracks\":501}}")]
    [InlineData("/api/sources/lastfm/data", "{\"username\":\"listener\",\"filter\":{\"dataType\":0,\"timePeriod\":0,\"maxResults\":0}}")]
    public async Task InvalidCollections_AreRejectedBeforeProviderWork(string path, string body)
    {
        await using var api = await RunningApi.StartAsync(authenticated: true);
        using var response = await api.Client.PostAsync(path, new StringContent(body, System.Text.Encoding.UTF8, "application/json"));

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
        Assert.False(string.IsNullOrWhiteSpace((await response.Content.ReadFromJsonAsync<ApiError>())!.Code));
    }

    [Theory]
    [InlineData(HttpStatusCode.Unauthorized, HttpStatusCode.Unauthorized, "SPOTIFY_TOKEN_EXPIRED")]
    [InlineData(HttpStatusCode.TooManyRequests, HttpStatusCode.TooManyRequests, "SPOTIFY_RATE_LIMITED")]
    [InlineData(HttpStatusCode.ServiceUnavailable, HttpStatusCode.BadGateway, "SPOTIFY_API_ERROR")]
    public async Task SpotifyFailures_ReturnMachineReadableErrors(HttpStatusCode upstream, HttpStatusCode expected, string code)
    {
        await using var api = await RunningApi.StartAsync(authenticated: true);
        api.Matching.SearchTracksAsync("song", Arg.Any<string>(), Arg.Any<CancellationToken>())
            .Returns(Task.FromException<IReadOnlyList<SpotifyTrack>>(new HttpRequestException("Sensitive upstream details", null, upstream)));

        using var response = await api.Client.GetAsync("/api/match/spotify/search?query=song");

        Assert.Equal(expected, response.StatusCode);
        var error = await response.Content.ReadFromJsonAsync<ApiError>();
        Assert.Equal(code, error!.Code);
        Assert.DoesNotContain("Sensitive", await response.Content.ReadAsStringAsync());
    }

    [Fact]
    public async Task MalformedJson_ReturnsMachineReadableBadRequest()
    {
        await using var api = await RunningApi.StartAsync(authenticated: true);
        using var response = await api.Client.PostAsync("/api/match/spotify", new StringContent("{", System.Text.Encoding.UTF8, "application/json"));
        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
        Assert.Equal("INVALID_REQUEST", (await response.Content.ReadFromJsonAsync<ApiError>())!.Code);
    }

    [Fact]
    public async Task RepeatedRequests_AreRateLimitedWithJsonError()
    {
        await using var api = await RunningApi.StartAsync();
        HttpResponseMessage? rejected = null;
        for (var i = 0; i < 65; i++)
        {
            var response = await api.Client.GetAsync("/api/security-probe");
            if (response.StatusCode == HttpStatusCode.TooManyRequests) { rejected = response; break; }
            response.Dispose();
        }
        using (rejected)
        {
            Assert.NotNull(rejected);
            Assert.Equal("RATE_LIMITED", (await rejected.Content.ReadFromJsonAsync<ApiError>())!.Code);
        }
    }

    [Fact]
    public async Task PartialPlaylist_ReturnsExistingPlaylistAndConfirmedProgress()
    {
        await using var api = await RunningApi.StartAsync(authenticated: true);
        api.Matching.CreatePlaylistAsync(Arg.Any<PlaylistCreationRequest>(), "access", "user", Arg.Any<CancellationToken>())
            .Returns(new PlaylistCreationResponse
            {
                PlaylistId = "created", Uri = "spotify:playlist:created", Url = "https://open.spotify.com/playlist/created",
                TracksAdded = 100, IsComplete = false, ErrorCode = "PLAYLIST_PARTIALLY_CREATED", ErrorMessage = "Open the existing playlist before retrying."
            });
        using var response = await api.Client.PostAsJsonAsync("/api/playlist/create", new
        {
            name = "Playlist", isPublic = false, trackUris = new[] { "spotify:track:abc" }
        });
        Assert.Equal(HttpStatusCode.BadGateway, response.StatusCode);
        var result = await response.Content.ReadFromJsonAsync<PlaylistCreationResponse>();
        Assert.False(result!.IsComplete);
        Assert.Equal("created", result.PlaylistId);
        Assert.Equal(100, result.TracksAdded);
    }

    [Fact]
    public async Task ForwardedHost_CannotChangeRequestHost()
    {
        await using var api = await RunningApi.StartAsync();
        using var request = new HttpRequestMessage(HttpMethod.Get, "/api/request-info");
        request.Headers.Add("X-Forwarded-Host", "attacker.example");
        request.Headers.Add("X-Forwarded-Proto", "https");
        using var response = await api.Client.SendAsync(request);
        var body = await response.Content.ReadAsStringAsync();
        Assert.StartsWith("https|127.0.0.1:", body);
        Assert.DoesNotContain("attacker", body);
    }

    [Theory]
    [InlineData("/api/config/discogs", "{\"usernameOrCollectionId\":\"../secret\"}")]
    [InlineData("/api/config/setlistfm", "{\"usernameOrId\":\"../secret\"}")]
    public async Task InvalidProfileIdentifiers_ReturnClientError(string path, string body)
    {
        await using var api = await RunningApi.StartAsync(authenticated: true);
        api.Discogs.GetProfileAsync("../secret", Arg.Any<CancellationToken>())
            .Returns(Task.FromException<DiscogsProfile?>(new ArgumentException("Provider validation details")));
        api.Setlist.GetUserAsync("../secret", Arg.Any<CancellationToken>())
            .Returns(Task.FromException<SetlistUser?>(new ArgumentException("Provider validation details")));

        using var response = await api.Client.PostAsync(path, new StringContent(body, System.Text.Encoding.UTF8, "application/json"));
        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
        var error = await response.Content.ReadFromJsonAsync<ApiError>();
        Assert.Equal("INVALID_PROFILE", error!.Code);
        Assert.DoesNotContain("Provider validation details", await response.Content.ReadAsStringAsync());
    }

    [Fact]
    public async Task UnconfirmedCreation_ReturnsWarningInsteadOfRetryableGenericError()
    {
        await using var api = await RunningApi.StartAsync(authenticated: true);
        api.Matching.CreatePlaylistAsync(Arg.Any<PlaylistCreationRequest>(), "access", "user", Arg.Any<CancellationToken>())
            .Returns(Task.FromException<PlaylistCreationResponse>(new SpotifyApiException(HttpStatusCode.BadGateway, "PLAYLIST_CREATION_UNCONFIRMED")));
        using var response = await api.Client.PostAsJsonAsync("/api/playlist/create", new
        {
            name = "Playlist", isPublic = false, trackUris = new[] { "spotify:track:abc" }
        });
        Assert.Equal(HttpStatusCode.BadGateway, response.StatusCode);
        var error = await response.Content.ReadFromJsonAsync<ApiError>();
        Assert.Equal("PLAYLIST_CREATION_UNCONFIRMED", error!.Code);
        Assert.Contains("Check your Spotify library", error.Message);
    }

    [Fact]
    public async Task PlaylistFailureContract_DescribesBothKnownAndUnknownCreationOutcomes()
    {
        await using var api = await RunningApi.StartAsync();
        using var document = await System.Text.Json.JsonDocument.ParseAsync(await api.Client.GetStreamAsync("/openapi/v1.json"));
        var variants = document.RootElement.GetProperty("paths").GetProperty("/api/playlist/create").GetProperty("post")
            .GetProperty("responses").GetProperty("502").GetProperty("content").GetProperty("application/json")
            .GetProperty("schema").GetProperty("oneOf");
        Assert.Equal(2, variants.GetArrayLength());
        var schemas = variants.EnumerateArray().Select(schema => schema.TryGetProperty("$ref", out var reference)
            ? document.RootElement.GetProperty("components").GetProperty("schemas").GetProperty(reference.GetString()!.Split('/')[^1])
            : schema).ToArray();
        Assert.Contains(schemas, schema => schema.GetProperty("properties").TryGetProperty("code", out _) &&
            schema.GetProperty("properties").TryGetProperty("message", out _));
        Assert.Contains(schemas, schema => schema.GetProperty("properties").TryGetProperty("playlistId", out _) &&
            schema.GetProperty("properties").TryGetProperty("tracksAdded", out _));
    }

    private sealed class RunningApi(WebApplication app, HttpClient client, ISpotifyMatchingService matching) : IAsyncDisposable
    {
        public HttpClient Client { get; } = client;
        public ISpotifyMatchingService Matching { get; } = matching;
        public IDiscogsService Discogs => app.Services.GetRequiredService<IDiscogsService>();
        public ISetlistFmService Setlist => app.Services.GetRequiredService<ISetlistFmService>();

        public static async Task<RunningApi> StartAsync(bool authenticated = false)
        {
            var builder = WebApplication.CreateBuilder();
            builder.WebHost.UseUrls("http://127.0.0.1:0");
            var store = new InMemorySessionStore();
            if (authenticated)
            {
                store.StoreSession(new AuthSession
                {
                    SessionId = "session", AccessToken = "access", RefreshToken = "refresh",
                    ExpiresAt = DateTime.UtcNow.AddHours(1), User = new SpotifyUser { Id = "user", DisplayName = "Listener" }
                });
            }
            var matching = Substitute.For<ISpotifyMatchingService>();
            builder.Services.AddSingleton<ISessionStore>(store);
            builder.Services.AddSingleton(matching);
            builder.Services.AddSingleton(Substitute.For<ILastfmService>());
            builder.Services.AddSingleton(Substitute.For<IDiscogsService>());
            builder.Services.AddSingleton(Substitute.For<ISetlistFmService>());
            builder.Services.AddOpenApi();
            builder.AddRequestSecurity();
            var app = builder.Build();
            app.UseRequestSecurity();
            app.MapOpenApi();
            var api = app.MapGroup("/api");
            api.MapGet("/security-probe", () => "ok");
            api.MapGet("/request-info", (Microsoft.AspNetCore.Http.HttpContext context) => $"{context.Request.Scheme}|{context.Request.Host}");
            api.MapSourcesEndpoints();
            api.MapMatchingEndpoints();
            api.MapPlaylistEndpoints();
            api.MapConfigurationEndpoints();
            await app.StartAsync();
            var address = app.Services.GetRequiredService<IServer>().Features.Get<IServerAddressesFeature>()!.Addresses.Single();
            var client = new HttpClient { BaseAddress = new Uri(address) };
            if (authenticated) client.DefaultRequestHeaders.Add("Cookie", "replay_session_id=session");
            return new RunningApi(app, client, matching);
        }

        public async ValueTask DisposeAsync()
        {
            Client.Dispose();
            await app.StopAsync();
            await app.DisposeAsync();
        }
    }
}
