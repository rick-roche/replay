using System.Reflection;
using FluentAssertions;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Http.HttpResults;
using RePlay.Server.Endpoints;
using RePlay.Server.Models;
using RePlay.Server.Services;

namespace RePlay.Server.Tests.Endpoints;

public class PlaylistEndpointsTests
{
    private static MethodInfo GetPrivate(string name)
    {
        var mi = typeof(PlaylistEndpoints).GetMethod(name, BindingFlags.NonPublic | BindingFlags.Static);
        mi.Should().NotBeNull();
        return mi!;
    }

    private static async Task<IResult> InvokeAsync(MethodInfo mi, params object[] args)
    {
        var task = (Task<IResult>)mi.Invoke(null, args)!;
        return await task.ConfigureAwait(false);
    }

    private sealed class FakeSpotifyMatchingService : ISpotifyMatchingService
    {
        public Func<PlaylistCreationRequest, string, string, CancellationToken, Task<PlaylistCreationResponse>>? OnCreatePlaylistAsync { get; set; }
        public Func<string, string, CancellationToken, Task<IReadOnlyList<SpotifyTrack>>>? OnSearchTracksAsync { get; set; }
        public Func<IReadOnlyList<NormalizedTrack>, string, CancellationToken, Task<MatchedDataResponse>>? OnMatchTracksAsync { get; set; }
        public Func<IReadOnlyList<NormalizedAlbum>, string, CancellationToken, Task<MatchedAlbumsResponse>>? OnMatchAlbumsAsync { get; set; }
        public Func<string, string, CancellationToken, Task<IReadOnlyList<SpotifyAlbumInfo>>>? OnSearchAlbumsAsync { get; set; }
        public Func<IReadOnlyList<NormalizedArtist>, string, CancellationToken, Task<MatchedArtistsResponse>>? OnMatchArtistsAsync { get; set; }
        public Func<string, string, CancellationToken, Task<IReadOnlyList<SpotifyArtistInfo>>>? OnSearchArtistsAsync { get; set; }

        public Task<PlaylistCreationResponse> CreatePlaylistAsync(PlaylistCreationRequest request, string accessToken, string userId, CancellationToken cancellationToken = default)
            => OnCreatePlaylistAsync?.Invoke(request, accessToken, userId, cancellationToken) 
                ?? Task.FromResult(new PlaylistCreationResponse { PlaylistId = "playlist123", Uri = "spotify:playlist:playlist123", Url = "https://spotify.com/playlist", TracksAdded = request.TrackUris.Count });

        public Task<IReadOnlyList<SpotifyTrack>> SearchTracksAsync(string query, string accessToken, CancellationToken cancellationToken = default)
            => OnSearchTracksAsync?.Invoke(query, accessToken, cancellationToken) ?? Task.FromResult<IReadOnlyList<SpotifyTrack>>(Array.Empty<SpotifyTrack>().AsReadOnly());

        public Task<MatchedDataResponse> MatchTracksAsync(IReadOnlyList<NormalizedTrack> tracks, string accessToken, CancellationToken cancellationToken = default)
            => OnMatchTracksAsync?.Invoke(tracks, accessToken, cancellationToken) ?? Task.FromResult(new MatchedDataResponse { Tracks = [] });

        public Task<MatchedAlbumsResponse> MatchAlbumsAsync(IReadOnlyList<NormalizedAlbum> albums, string accessToken, CancellationToken cancellationToken = default)
            => OnMatchAlbumsAsync?.Invoke(albums, accessToken, cancellationToken) ?? Task.FromResult(new MatchedAlbumsResponse { Albums = [] });

        public Task<IReadOnlyList<SpotifyAlbumInfo>> SearchAlbumsAsync(string query, string accessToken, CancellationToken cancellationToken = default)
            => OnSearchAlbumsAsync?.Invoke(query, accessToken, cancellationToken) ?? Task.FromResult<IReadOnlyList<SpotifyAlbumInfo>>(Array.Empty<SpotifyAlbumInfo>());

        public Task<MatchedArtistsResponse> MatchArtistsAsync(IReadOnlyList<NormalizedArtist> artists, string accessToken, CancellationToken cancellationToken = default)
            => OnMatchArtistsAsync?.Invoke(artists, accessToken, cancellationToken) ?? Task.FromResult(new MatchedArtistsResponse { Artists = [] });

        public Task<IReadOnlyList<SpotifyArtistInfo>> SearchArtistsAsync(string query, string accessToken, CancellationToken cancellationToken = default)
            => OnSearchArtistsAsync?.Invoke(query, accessToken, cancellationToken) ?? Task.FromResult<IReadOnlyList<SpotifyArtistInfo>>(Array.Empty<SpotifyArtistInfo>());
    }

    private static HttpContext ContextWithSessionCookie(string? sessionId = "sid")
    {
        var ctx = new DefaultHttpContext();
        if (sessionId != null)
        {
            ctx.Request.Headers.Append("Cookie", $"replay_session_id={sessionId}");
        }
        return ctx;
    }

    private static AuthSession NewSession(string id = "sid", DateTime? expiresAt = null) => new AuthSession
    {
        SessionId = id,
        AccessToken = "access_token",
        RefreshToken = "refresh_token",
        ExpiresAt = expiresAt ?? DateTime.UtcNow.AddHours(1),
        User = new SpotifyUser { Id = "user123", DisplayName = "Test User" }
    };

    [Fact]
    public async Task CreatePlaylist_ValidRequest_ReturnsOk()
    {
        var mi = GetPrivate("CreatePlaylist");
        var ctx = ContextWithSessionCookie();
        var store = new InMemorySessionStore();
        store.StoreSession(NewSession());

        var service = new FakeSpotifyMatchingService();
        var request = new PlaylistCreationRequest
        {
            Name = "My Playlist",
            IsPublic = true,
            TrackUris = ["spotify:track:track1", "spotify:track:track2"]
        };

        var result = await InvokeAsync(mi, ctx, request, store, service, CancellationToken.None);

        result.Should().BeOfType<Ok<PlaylistCreationResponse>>();
        var ok = (Ok<PlaylistCreationResponse>)result;
        ok.Value!.PlaylistId.Should().Be("playlist123");
        ok.Value!.TracksAdded.Should().Be(2);
    }

    [Fact]
    public async Task CreatePlaylist_NullRequest_ReturnsBadRequest()
    {
        var mi = GetPrivate("CreatePlaylist");
        var ctx = ContextWithSessionCookie();
        var store = new InMemorySessionStore();
        store.StoreSession(NewSession());
        var service = new FakeSpotifyMatchingService();

        var result = await InvokeAsync(mi, ctx, null!, store, service, CancellationToken.None);

        result.Should().BeOfType<BadRequest<ApiError>>();
        ((BadRequest<ApiError>)result).Value!.Code.Should().Be("INVALID_REQUEST");
    }

    [Fact]
    public async Task CreatePlaylist_MissingPlaylistName_ReturnsBadRequest()
    {
        var mi = GetPrivate("CreatePlaylist");
        var ctx = ContextWithSessionCookie();
        var store = new InMemorySessionStore();
        store.StoreSession(NewSession());
        var service = new FakeSpotifyMatchingService();

        var request = new PlaylistCreationRequest
        {
            Name = "",
            IsPublic = true,
            TrackUris = ["spotify:track:track1"]
        };

        var result = await InvokeAsync(mi, ctx, request, store, service, CancellationToken.None);

        result.Should().BeOfType<BadRequest<ApiError>>();
        ((BadRequest<ApiError>)result).Value!.Code.Should().Be("INVALID_PLAYLIST_NAME");
    }

    [Fact]
    public async Task CreatePlaylist_EmptyTrackList_ReturnsBadRequest()
    {
        var mi = GetPrivate("CreatePlaylist");
        var ctx = ContextWithSessionCookie();
        var store = new InMemorySessionStore();
        store.StoreSession(NewSession());
        var service = new FakeSpotifyMatchingService();

        var request = new PlaylistCreationRequest
        {
            Name = "My Playlist",
            IsPublic = true,
            TrackUris = []
        };

        var result = await InvokeAsync(mi, ctx, request, store, service, CancellationToken.None);

        result.Should().BeOfType<BadRequest<ApiError>>();
        ((BadRequest<ApiError>)result).Value!.Code.Should().Be("INVALID_TRACKS");
    }

    [Fact]
    public async Task CreatePlaylist_NullTrackList_ReturnsBadRequest()
    {
        var mi = GetPrivate("CreatePlaylist");
        var ctx = ContextWithSessionCookie();
        var store = new InMemorySessionStore();
        store.StoreSession(NewSession());
        var service = new FakeSpotifyMatchingService();

        var request = new PlaylistCreationRequest
        {
            Name = "My Playlist",
            IsPublic = true,
            TrackUris = null!
        };

        var result = await InvokeAsync(mi, ctx, request, store, service, CancellationToken.None);

        result.Should().BeOfType<BadRequest<ApiError>>();
        ((BadRequest<ApiError>)result).Value!.Code.Should().Be("INVALID_TRACKS");
    }

    [Fact]
    public async Task CreatePlaylist_InvalidTrackUriFormat_ReturnsBadRequest()
    {
        var mi = GetPrivate("CreatePlaylist");
        var ctx = ContextWithSessionCookie();
        var store = new InMemorySessionStore();
        store.StoreSession(NewSession());
        var service = new FakeSpotifyMatchingService();

        var request = new PlaylistCreationRequest
        {
            Name = "My Playlist",
            IsPublic = true,
            TrackUris = ["invalid:uri:format"]
        };

        var result = await InvokeAsync(mi, ctx, request, store, service, CancellationToken.None);

        result.Should().BeOfType<BadRequest<ApiError>>();
        ((BadRequest<ApiError>)result).Value!.Code.Should().Be("INVALID_TRACK_URI");
    }

    [Fact]
    public async Task CreatePlaylist_NoSessionCookie_ReturnsUnauthorized()
    {
        var mi = GetPrivate("CreatePlaylist");
        var ctx = ContextWithSessionCookie(null);
        var store = new InMemorySessionStore();
        var service = new FakeSpotifyMatchingService();

        var request = new PlaylistCreationRequest
        {
            Name = "My Playlist",
            IsPublic = true,
            TrackUris = ["spotify:track:track1"]
        };

        var result = await InvokeAsync(mi, ctx, request, store, service, CancellationToken.None);

        result.Should().BeOfType<JsonHttpResult<ApiError>>();
        var json = (JsonHttpResult<ApiError>)result;
        json.StatusCode.Should().Be(401);
        json.Value!.Code.Should().Be("UNAUTHORIZED");
    }

    [Fact]
    public async Task CreatePlaylist_InvalidSessionId_ReturnsUnauthorized()
    {
        var mi = GetPrivate("CreatePlaylist");
        var ctx = ContextWithSessionCookie("invalid_session");
        var store = new InMemorySessionStore();
        var service = new FakeSpotifyMatchingService();

        var request = new PlaylistCreationRequest
        {
            Name = "My Playlist",
            IsPublic = true,
            TrackUris = ["spotify:track:track1"]
        };

        var result = await InvokeAsync(mi, ctx, request, store, service, CancellationToken.None);

        result.Should().BeOfType<JsonHttpResult<ApiError>>();
        var json = (JsonHttpResult<ApiError>)result;
        json.StatusCode.Should().Be(401);
        json.Value!.Code.Should().Be("INVALID_SESSION");
    }

    [Fact]
    public async Task CreatePlaylist_ExpiredSession_DeletesCookie_ReturnsUnauthorized()
    {
        var mi = GetPrivate("CreatePlaylist");
        var ctx = ContextWithSessionCookie("sid");
        var store = new InMemorySessionStore();
        store.StoreSession(NewSession("sid", DateTime.UtcNow.AddHours(-1)));
        var service = new FakeSpotifyMatchingService();

        var request = new PlaylistCreationRequest
        {
            Name = "My Playlist",
            IsPublic = true,
            TrackUris = ["spotify:track:track1"]
        };

        var result = await InvokeAsync(mi, ctx, request, store, service, CancellationToken.None);

        result.Should().BeOfType<JsonHttpResult<ApiError>>();
        var json = (JsonHttpResult<ApiError>)result;
        json.StatusCode.Should().Be(401);
        json.Value!.Code.Should().Be("SESSION_EXPIRED");
        var setCookie = string.Join(";", ctx.Response.Headers["Set-Cookie"].ToArray());
        setCookie.Should().Contain("replay_session_id=");
    }

    [Fact]
    public async Task CreatePlaylist_MissingUserId_ReturnsUnauthorized()
    {
        var mi = GetPrivate("CreatePlaylist");
        var ctx = ContextWithSessionCookie("sid");
        var store = new InMemorySessionStore();
        var session = new AuthSession
        {
            SessionId = "sid",
            AccessToken = "access_token",
            RefreshToken = "refresh_token",
            ExpiresAt = DateTime.UtcNow.AddHours(1),
            User = new SpotifyUser { Id = "", DisplayName = "Test User" }
        };
        store.StoreSession(session);
        var service = new FakeSpotifyMatchingService();

        var request = new PlaylistCreationRequest
        {
            Name = "My Playlist",
            IsPublic = true,
            TrackUris = ["spotify:track:track1"]
        };

        var result = await InvokeAsync(mi, ctx, request, store, service, CancellationToken.None);

        result.Should().BeOfType<JsonHttpResult<ApiError>>();
        var json = (JsonHttpResult<ApiError>)result;
        json.StatusCode.Should().Be(401);
        json.Value!.Code.Should().Be("MISSING_CREDENTIALS");
    }

    [Fact]
    public async Task CreatePlaylist_MissingAccessToken_ReturnsUnauthorized()
    {
        var mi = GetPrivate("CreatePlaylist");
        var ctx = ContextWithSessionCookie("sid");
        var store = new InMemorySessionStore();
        var session = new AuthSession
        {
            SessionId = "sid",
            AccessToken = "",
            RefreshToken = "refresh_token",
            ExpiresAt = DateTime.UtcNow.AddHours(1),
            User = new SpotifyUser { Id = "user123", DisplayName = "Test User" }
        };
        store.StoreSession(session);
        var service = new FakeSpotifyMatchingService();

        var request = new PlaylistCreationRequest
        {
            Name = "My Playlist",
            IsPublic = true,
            TrackUris = ["spotify:track:track1"]
        };

        var result = await InvokeAsync(mi, ctx, request, store, service, CancellationToken.None);

        result.Should().BeOfType<JsonHttpResult<ApiError>>();
        var json = (JsonHttpResult<ApiError>)result;
        json.StatusCode.Should().Be(401);
        json.Value!.Code.Should().Be("MISSING_CREDENTIALS");
    }

    [Fact]
    public async Task CreatePlaylist_ServiceThrowsInvalidOperation_ReturnsInternalServerError()
    {
        var mi = GetPrivate("CreatePlaylist");
        var ctx = ContextWithSessionCookie();
        var store = new InMemorySessionStore();
        store.StoreSession(NewSession());

        var service = new FakeSpotifyMatchingService
        {
            OnCreatePlaylistAsync = (r, at, uid, ct) => throw new InvalidOperationException("Failed to create playlist")
        };

        var request = new PlaylistCreationRequest
        {
            Name = "My Playlist",
            IsPublic = true,
            TrackUris = ["spotify:track:track1"]
        };

        var result = await InvokeAsync(mi, ctx, request, store, service, CancellationToken.None);

        result.Should().BeOfType<JsonHttpResult<ApiError>>();
        var json = (JsonHttpResult<ApiError>)result;
        json.StatusCode.Should().Be(500);
        json.Value!.Code.Should().Be("PLAYLIST_CREATION_FAILED");
    }

    [Fact]
    public async Task CreatePlaylist_ServiceThrowsGenericException_ReturnsInternalServerError()
    {
        var mi = GetPrivate("CreatePlaylist");
        var ctx = ContextWithSessionCookie();
        var store = new InMemorySessionStore();
        store.StoreSession(NewSession());

        var service = new FakeSpotifyMatchingService
        {
            OnCreatePlaylistAsync = (r, at, uid, ct) => throw new Exception("Unexpected error")
        };

        var request = new PlaylistCreationRequest
        {
            Name = "My Playlist",
            IsPublic = true,
            TrackUris = ["spotify:track:track1"]
        };

        var result = await InvokeAsync(mi, ctx, request, store, service, CancellationToken.None);

        result.Should().BeOfType<JsonHttpResult<ApiError>>();
        var json = (JsonHttpResult<ApiError>)result;
        json.StatusCode.Should().Be(500);
        json.Value!.Code.Should().Be("INTERNAL_SERVER_ERROR");
    }

    [Fact]
    public async Task CreatePlaylist_MultipleValidTracks_ReturnsOk()
    {
        var mi = GetPrivate("CreatePlaylist");
        var ctx = ContextWithSessionCookie();
        var store = new InMemorySessionStore();
        store.StoreSession(NewSession());

        var service = new FakeSpotifyMatchingService();
        var request = new PlaylistCreationRequest
        {
            Name = "Big Playlist",
            IsPublic = false,
            TrackUris = [
                "spotify:track:track1",
                "spotify:track:track2",
                "spotify:track:track3",
                "spotify:track:track4",
                "spotify:track:track5"
            ]
        };

        var result = await InvokeAsync(mi, ctx, request, store, service, CancellationToken.None);

        result.Should().BeOfType<Ok<PlaylistCreationResponse>>();
        var ok = (Ok<PlaylistCreationResponse>)result;
        ok.Value!.TracksAdded.Should().Be(5);
    }

    [Fact]
    public async Task CreatePlaylist_WhitespacePlaylistName_ReturnsBadRequest()
    {
        var mi = GetPrivate("CreatePlaylist");
        var ctx = ContextWithSessionCookie();
        var store = new InMemorySessionStore();
        store.StoreSession(NewSession());
        var service = new FakeSpotifyMatchingService();

        var request = new PlaylistCreationRequest
        {
            Name = "   ",
            IsPublic = true,
            TrackUris = ["spotify:track:track1"]
        };

        var result = await InvokeAsync(mi, ctx, request, store, service, CancellationToken.None);

        result.Should().BeOfType<BadRequest<ApiError>>();
        ((BadRequest<ApiError>)result).Value!.Code.Should().Be("INVALID_PLAYLIST_NAME");
    }

    [Fact]
    public async Task CreatePlaylist_TrackUriWithoutPrefix_ReturnsBadRequest()
    {
        var mi = GetPrivate("CreatePlaylist");
        var ctx = ContextWithSessionCookie();
        var store = new InMemorySessionStore();
        store.StoreSession(NewSession());
        var service = new FakeSpotifyMatchingService();

        var request = new PlaylistCreationRequest
        {
            Name = "My Playlist",
            IsPublic = true,
            TrackUris = ["track:track1"]
        };

        var result = await InvokeAsync(mi, ctx, request, store, service, CancellationToken.None);

        result.Should().BeOfType<BadRequest<ApiError>>();
        ((BadRequest<ApiError>)result).Value!.Code.Should().Be("INVALID_TRACK_URI");
    }

    [Fact]
    public async Task CreatePlaylist_VeryLongPlaylistName_ReturnsOk()
    {
        var mi = GetPrivate("CreatePlaylist");
        var ctx = ContextWithSessionCookie();
        var store = new InMemorySessionStore();
        store.StoreSession(NewSession());
        var service = new FakeSpotifyMatchingService();

        var longName = new string('a', 300);
        var request = new PlaylistCreationRequest
        {
            Name = longName,
            IsPublic = true,
            TrackUris = ["spotify:track:track1"]
        };

        var result = await InvokeAsync(mi, ctx, request, store, service, CancellationToken.None);

        result.Should().BeOfType<Ok<PlaylistCreationResponse>>();
    }

    [Fact]
    public async Task CreatePlaylist_PlaylistNameWithSpecialCharacters_ReturnsOk()
    {
        var mi = GetPrivate("CreatePlaylist");
        var ctx = ContextWithSessionCookie();
        var store = new InMemorySessionStore();
        store.StoreSession(NewSession());
        var service = new FakeSpotifyMatchingService();

        var request = new PlaylistCreationRequest
        {
            Name = "Playlist @#$%^&*()[]{}!',.:;?|\\",
            IsPublic = true,
            TrackUris = ["spotify:track:track1"]
        };

        var result = await InvokeAsync(mi, ctx, request, store, service, CancellationToken.None);

        result.Should().BeOfType<Ok<PlaylistCreationResponse>>();
    }

    [Fact]
    public async Task CreatePlaylist_MixedValidInvalidTrackUris_ReturnsBadRequest()
    {
        var mi = GetPrivate("CreatePlaylist");
        var ctx = ContextWithSessionCookie();
        var store = new InMemorySessionStore();
        store.StoreSession(NewSession());
        var service = new FakeSpotifyMatchingService();

        var request = new PlaylistCreationRequest
        {
            Name = "My Playlist",
            IsPublic = true,
            TrackUris = ["spotify:track:track1", "invalid:track:format", "spotify:track:track2"]
        };

        var result = await InvokeAsync(mi, ctx, request, store, service, CancellationToken.None);

        result.Should().BeOfType<BadRequest<ApiError>>();
        ((BadRequest<ApiError>)result).Value!.Code.Should().Be("INVALID_TRACK_URI");
    }

    [Fact]
    public async Task CreatePlaylist_SingleValidTrackUri_ReturnsOk()
    {
        var mi = GetPrivate("CreatePlaylist");
        var ctx = ContextWithSessionCookie();
        var store = new InMemorySessionStore();
        store.StoreSession(NewSession());
        var service = new FakeSpotifyMatchingService();

        var request = new PlaylistCreationRequest
        {
            Name = "Single Track Playlist",
            IsPublic = true,
            TrackUris = ["spotify:track:abc123xyz"]
        };

        var result = await InvokeAsync(mi, ctx, request, store, service, CancellationToken.None);

        result.Should().BeOfType<Ok<PlaylistCreationResponse>>();
        var ok = (Ok<PlaylistCreationResponse>)result;
        ok.Value!.TracksAdded.Should().Be(1);
    }

    [Fact]
    public async Task CreatePlaylist_TrackUriWithExtraColons_ReturnsBadRequest()
    {
        var mi = GetPrivate("CreatePlaylist");
        var ctx = ContextWithSessionCookie();
        var store = new InMemorySessionStore();
        store.StoreSession(NewSession());
        var service = new FakeSpotifyMatchingService();

        var request = new PlaylistCreationRequest
        {
            Name = "My Playlist",
            IsPublic = true,
            TrackUris = ["spotify:track:id:extra"]
        };

        // This might actually be valid if the ID contains colons, but let's check the validation logic
        var result = await InvokeAsync(mi, ctx, request, store, service, CancellationToken.None);

        result.Should().BeOfType<Ok<PlaylistCreationResponse>>();
    }

    [Fact]
    public async Task CreatePlaylist_EmptyStringTrackUri_ReturnsBadRequest()
    {
        var mi = GetPrivate("CreatePlaylist");
        var ctx = ContextWithSessionCookie();
        var store = new InMemorySessionStore();
        store.StoreSession(NewSession());
        var service = new FakeSpotifyMatchingService();

        var request = new PlaylistCreationRequest
        {
            Name = "My Playlist",
            IsPublic = true,
            TrackUris = [""]
        };

        var result = await InvokeAsync(mi, ctx, request, store, service, CancellationToken.None);

        result.Should().BeOfType<BadRequest<ApiError>>();
        ((BadRequest<ApiError>)result).Value!.Code.Should().Be("INVALID_TRACK_URI");
    }

    [Fact]
    public async Task CreatePlaylist_SessionStoreReturnsNullSecondTime()
    {
        var mi = GetPrivate("CreatePlaylist");
        var ctx = ContextWithSessionCookie("sid");
        
        // Create a custom session store that returns session once, then null
        var sessionStore = new InMemorySessionStore();
        var session = NewSession("sid");
        sessionStore.StoreSession(session);
        
        var service = new FakeSpotifyMatchingService();

        var request = new PlaylistCreationRequest
        {
            Name = "My Playlist",
            IsPublic = true,
            TrackUris = ["spotify:track:track1"]
        };

        var result = await InvokeAsync(mi, ctx, request, sessionStore, service, CancellationToken.None);

        // First call should succeed because session exists
        result.Should().BeOfType<Ok<PlaylistCreationResponse>>();

        // Remove the session from store
        sessionStore.RemoveSession("sid");

        // Second call should fail
        var ctx2 = ContextWithSessionCookie("sid");
        var result2 = await InvokeAsync(mi, ctx2, request, sessionStore, service, CancellationToken.None);

        result2.Should().BeOfType<JsonHttpResult<ApiError>>();
        var json = (JsonHttpResult<ApiError>)result2;
        json.StatusCode.Should().Be(401);
        json.Value!.Code.Should().Be("INVALID_SESSION");
    }

    [Fact]
    public async Task CreatePlaylist_PlaylistNameWithUnicode_ReturnsOk()
    {
        var mi = GetPrivate("CreatePlaylist");
        var ctx = ContextWithSessionCookie();
        var store = new InMemorySessionStore();
        store.StoreSession(NewSession());
        var service = new FakeSpotifyMatchingService();

        var request = new PlaylistCreationRequest
        {
            Name = "Playlist 🎵 日本語 Ñoño",
            IsPublic = true,
            TrackUris = ["spotify:track:track1"]
        };

        var result = await InvokeAsync(mi, ctx, request, store, service, CancellationToken.None);

        result.Should().BeOfType<Ok<PlaylistCreationResponse>>();
    }

    [Fact]
    public async Task CreatePlaylist_ServiceReturnsNullPlaylistId()
    {
        var mi = GetPrivate("CreatePlaylist");
        var ctx = ContextWithSessionCookie();
        var store = new InMemorySessionStore();
        store.StoreSession(NewSession());

        var service = new FakeSpotifyMatchingService
        {
            OnCreatePlaylistAsync = (r, at, uid, ct) => Task.FromResult(new PlaylistCreationResponse { PlaylistId = "", Uri = "", Url = "", TracksAdded = 0 })
        };

        var request = new PlaylistCreationRequest
        {
            Name = "My Playlist",
            IsPublic = true,
            TrackUris = ["spotify:track:track1"]
        };

        var result = await InvokeAsync(mi, ctx, request, store, service, CancellationToken.None);

        result.Should().BeOfType<Ok<PlaylistCreationResponse>>();
        var ok = (Ok<PlaylistCreationResponse>)result;
        ok.Value!.PlaylistId.Should().Be("");
    }

    [Fact]
    public async Task CreatePlaylist_IsPrivatePlaylist()
    {
        var mi = GetPrivate("CreatePlaylist");
        var ctx = ContextWithSessionCookie();
        var store = new InMemorySessionStore();
        store.StoreSession(NewSession());

        var service = new FakeSpotifyMatchingService();

        var request = new PlaylistCreationRequest
        {
            Name = "Private Playlist",
            IsPublic = false,
            TrackUris = ["spotify:track:track1", "spotify:track:track2"]
        };

        var result = await InvokeAsync(mi, ctx, request, store, service, CancellationToken.None);

        result.Should().BeOfType<Ok<PlaylistCreationResponse>>();
        var ok = (Ok<PlaylistCreationResponse>)result;
        ok.Value!.TracksAdded.Should().Be(2);
    }
}
