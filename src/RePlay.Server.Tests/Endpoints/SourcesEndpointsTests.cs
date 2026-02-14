using System.Reflection;
using FluentAssertions;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Http.HttpResults;
using RePlay.Server.Endpoints;
using RePlay.Server.Models;
using RePlay.Server.Services;

namespace RePlay.Server.Tests.Endpoints;

public class SourcesEndpointsTests
{
    private static MethodInfo GetPrivate(string name)
    {
        var mi = typeof(SourcesEndpoints).GetMethod(name, BindingFlags.NonPublic | BindingFlags.Static);
        mi.Should().NotBeNull();
        return mi!;
    }

    private static async Task<IResult> InvokeAsync(MethodInfo mi, params object[] args)
    {
        var task = (Task<IResult>)mi.Invoke(null, args)!;
        return await task.ConfigureAwait(false);
    }

    private sealed class FakeLastfmService : ILastfmService
    {
        public Func<string, CancellationToken, Task<LastfmUser?>>? OnGetUserAsync { get; set; }
        public Func<string, LastfmFilter, CancellationToken, Task<LastfmDataResponse?>>? OnGetUserDataAsync { get; set; }
        public Func<string, LastfmFilter, CancellationToken, Task<NormalizedDataResponse?>>? OnGetUserDataNormalizedAsync { get; set; }

        public Task<LastfmUser?> GetUserAsync(string username, CancellationToken cancellationToken = default)
            => OnGetUserAsync?.Invoke(username, cancellationToken) ?? Task.FromResult<LastfmUser?>(null);

        public Task<LastfmDataResponse?> GetUserDataAsync(string username, LastfmFilter filter, CancellationToken cancellationToken = default)
            => OnGetUserDataAsync?.Invoke(username, filter, cancellationToken) ?? Task.FromResult<LastfmDataResponse?>(null);

        public Task<NormalizedDataResponse?> GetUserDataNormalizedAsync(string username, LastfmFilter filter, CancellationToken cancellationToken = default)
            => OnGetUserDataNormalizedAsync?.Invoke(username, filter, cancellationToken) ?? Task.FromResult<NormalizedDataResponse?>(null);
    }

    private sealed class FakeDiscogsService : IDiscogsService
    {
        public Func<string, CancellationToken, Task<DiscogsProfile?>>? OnGetProfileAsync { get; set; }
        public Func<string, DiscogsFilter, CancellationToken, Task<DiscogsDataResponse?>>? OnGetCollectionAsync { get; set; }
        public Func<string, DiscogsFilter, CancellationToken, Task<NormalizedDataResponse?>>? OnGetCollectionNormalizedAsync { get; set; }

        public Task<DiscogsProfile?> GetProfileAsync(string usernameOrCollectionId, CancellationToken cancellationToken = default)
            => OnGetProfileAsync?.Invoke(usernameOrCollectionId, cancellationToken) ?? Task.FromResult<DiscogsProfile?>(null);

        public Task<DiscogsDataResponse?> GetCollectionAsync(string usernameOrCollectionId, DiscogsFilter filter, CancellationToken cancellationToken = default)
            => OnGetCollectionAsync?.Invoke(usernameOrCollectionId, filter, cancellationToken) ?? Task.FromResult<DiscogsDataResponse?>(null);

        public Task<NormalizedDataResponse?> GetCollectionNormalizedAsync(string usernameOrCollectionId, DiscogsFilter filter, CancellationToken cancellationToken = default)
            => OnGetCollectionNormalizedAsync?.Invoke(usernameOrCollectionId, filter, cancellationToken) ?? Task.FromResult<NormalizedDataResponse?>(null);
    }

    private sealed class FakeSetlistFmService : ISetlistFmService
    {
        public Func<string, CancellationToken, Task<SetlistUser?>>? OnGetUserAsync { get; set; }
        public Func<string, SetlistFmFilter, CancellationToken, Task<SetlistFmDataResponse>>? OnGetUserConcertsAsync { get; set; }
        public Func<string, SetlistFmFilter, CancellationToken, Task<NormalizedDataResponse>>? OnGetUserConcertsNormalizedAsync { get; set; }

        public Task<SetlistUser?> GetUserAsync(string usernameOrId, CancellationToken cancellationToken = default)
            => OnGetUserAsync?.Invoke(usernameOrId, cancellationToken) ?? Task.FromResult<SetlistUser?>(null);

        public Task<SetlistFmDataResponse> GetUserConcertsAsync(string userId, SetlistFmFilter filter, CancellationToken cancellationToken = default)
            => OnGetUserConcertsAsync?.Invoke(userId, filter, cancellationToken) ?? Task.FromResult(new SetlistFmDataResponse { Concerts = [], Tracks = [] });

        public Task<NormalizedDataResponse> GetUserConcertsNormalizedAsync(string userId, SetlistFmFilter filter, CancellationToken cancellationToken = default)
            => OnGetUserConcertsNormalizedAsync?.Invoke(userId, filter, cancellationToken) ?? Task.FromResult(new NormalizedDataResponse { DataType = "Tracks", Tracks = [], Albums = [], Artists = [], Source = "setlistfm" });
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

    [Fact]
    public async Task PostFetchLastfmData_ValidatesErrors()
    {
        var mi = GetPrivate("PostFetchLastfmData");
        var ctx = ContextWithSessionCookie();
        var fake = new FakeLastfmService();

        // Missing username
        var r1 = await InvokeAsync(mi, new FetchLastfmDataRequest { Username = "", Filter = new LastfmFilter { DataType = LastfmDataType.Tracks, TimePeriod = LastfmTimePeriod.Overall } }, fake, ctx, CancellationToken.None);
        ((BadRequest<ApiError>)r1).Value!.Code.Should().Be("MISSING_USERNAME");

        // Missing filter
        var r2 = await InvokeAsync(mi, new FetchLastfmDataRequest { Username = "user", Filter = null! }, fake, ctx, CancellationToken.None);
        ((BadRequest<ApiError>)r2).Value!.Code.Should().Be("MISSING_FILTER");

        // Custom without dates
        var r3 = await InvokeAsync(mi, new FetchLastfmDataRequest { Username = "user", Filter = new LastfmFilter { DataType = LastfmDataType.Tracks, TimePeriod = LastfmTimePeriod.Custom } }, fake, ctx, CancellationToken.None);
        ((BadRequest<ApiError>)r3).Value!.Code.Should().Be("INVALID_CUSTOM_DATES");

        // Custom with invalid format
        var r4 = await InvokeAsync(mi, new FetchLastfmDataRequest { Username = "user", Filter = new LastfmFilter { DataType = LastfmDataType.Tracks, TimePeriod = LastfmTimePeriod.Custom, CustomStartDate = "bad", CustomEndDate = "bad" } }, fake, ctx, CancellationToken.None);
        ((BadRequest<ApiError>)r4).Value!.Code.Should().Be("INVALID_DATE_FORMAT");

        // Custom invalid range
        var r5 = await InvokeAsync(mi, new FetchLastfmDataRequest { Username = "user", Filter = new LastfmFilter { DataType = LastfmDataType.Tracks, TimePeriod = LastfmTimePeriod.Custom, CustomStartDate = "2025-12-31", CustomEndDate = "2025-01-01" } }, fake, ctx, CancellationToken.None);
        ((BadRequest<ApiError>)r5).Value!.Code.Should().Be("INVALID_DATE_RANGE");
    }

    [Fact]
    public async Task PostFetchLastfmData_ServicePaths()
    {
        var mi = GetPrivate("PostFetchLastfmData");
        var ctx = ContextWithSessionCookie();

        // Service returns null -> LASTFM_FETCH_FAILED
        var fake1 = new FakeLastfmService { OnGetUserDataAsync = (u, f, ct) => Task.FromResult<LastfmDataResponse?>(null) };
        var f1 = new LastfmFilter { DataType = LastfmDataType.Tracks, TimePeriod = LastfmTimePeriod.Overall };
        var r1 = await InvokeAsync(mi, new FetchLastfmDataRequest { Username = "user", Filter = f1 }, fake1, ctx, CancellationToken.None);
        ((BadRequest<ApiError>)r1).Value!.Code.Should().Be("LASTFM_FETCH_FAILED");

        // Service throws ArgumentException -> INVALID_FILTER
        var fake2 = new FakeLastfmService { OnGetUserDataAsync = (u, f, ct) => throw new ArgumentException("bad") };
        var r2 = await InvokeAsync(mi, new FetchLastfmDataRequest { Username = "user", Filter = f1 }, fake2, ctx, CancellationToken.None);
        ((BadRequest<ApiError>)r2).Value!.Code.Should().Be("INVALID_FILTER");

        // Service throws general exception -> LASTFM_FETCH_ERROR
        var fake3 = new FakeLastfmService { OnGetUserDataAsync = (u, f, ct) => throw new InvalidOperationException("oops") };
        var r3 = await InvokeAsync(mi, new FetchLastfmDataRequest { Username = "user", Filter = f1 }, fake3, ctx, CancellationToken.None);
        var json = (JsonHttpResult<ApiError>)r3;
        json.StatusCode.Should().Be(500);
        json.Value!.Code.Should().Be("LASTFM_FETCH_ERROR");

        // Success -> Ok
        var fake4 = new FakeLastfmService { OnGetUserDataAsync = (u, f, ct) => Task.FromResult<LastfmDataResponse?>(new LastfmDataResponse { DataType = f.DataType, Tracks = [], Albums = [], Artists = [] }) };
        var r4 = await InvokeAsync(mi, new FetchLastfmDataRequest { Username = "user", Filter = f1 }, fake4, ctx, CancellationToken.None);
        r4.Should().BeOfType<Ok<LastfmDataResponse>>();
    }

    [Fact]
    public async Task PostFetchLastfmDataNormalized_ValidatesErrors()
    {
        var mi = GetPrivate("PostFetchLastfmDataNormalized");
        var ctx = ContextWithSessionCookie();
        var fake = new FakeLastfmService();

        // Missing username
        var r1 = await InvokeAsync(mi, new FetchLastfmDataRequest { Username = "", Filter = new LastfmFilter { DataType = LastfmDataType.Tracks, TimePeriod = LastfmTimePeriod.Overall } }, fake, ctx, CancellationToken.None);
        ((BadRequest<ApiError>)r1).Value!.Code.Should().Be("MISSING_USERNAME");

        // Missing filter
        var r2 = await InvokeAsync(mi, new FetchLastfmDataRequest { Username = "user", Filter = null! }, fake, ctx, CancellationToken.None);
        ((BadRequest<ApiError>)r2).Value!.Code.Should().Be("MISSING_FILTER");
    }

    [Fact]
    public async Task PostFetchLastfmDataNormalized_ServicePaths()
    {
        var mi = GetPrivate("PostFetchLastfmDataNormalized");
        var ctx = ContextWithSessionCookie();

        // Service returns null -> LASTFM_FETCH_FAILED
        var fake1 = new FakeLastfmService { OnGetUserDataNormalizedAsync = (u, f, ct) => Task.FromResult<NormalizedDataResponse?>(null) };
        var f1 = new LastfmFilter { DataType = LastfmDataType.Tracks, TimePeriod = LastfmTimePeriod.Overall };
        var r1 = await InvokeAsync(mi, new FetchLastfmDataRequest { Username = "user", Filter = f1 }, fake1, ctx, CancellationToken.None);
        ((BadRequest<ApiError>)r1).Value!.Code.Should().Be("LASTFM_FETCH_FAILED");

        // Success -> Ok
        var fake2 = new FakeLastfmService { OnGetUserDataNormalizedAsync = (u, f, ct) => Task.FromResult<NormalizedDataResponse?>(new NormalizedDataResponse { DataType = f.DataType.ToString(), Tracks = [], Albums = [], Artists = [], Source = "lastfm" }) };
        var r2 = await InvokeAsync(mi, new FetchLastfmDataRequest { Username = "user", Filter = f1 }, fake2, ctx, CancellationToken.None);
        r2.Should().BeOfType<Ok<NormalizedDataResponse>>();
    }

    [Fact]
    public async Task PostFetchSetlistFmData_ValidatesErrors()
    {
        var mi = GetPrivate("PostFetchSetlistFmData");
        var ctx = ContextWithSessionCookie();
        var fake = new FakeSetlistFmService();

        // Missing user ID
        var r1 = await InvokeAsync(mi, new FetchSetlistFmDataRequest { UserId = "", Filter = new SetlistFmFilter() }, fake, ctx, CancellationToken.None);
        ((BadRequest<ApiError>)r1).Value!.Code.Should().Be("MISSING_USER_ID");

        // Missing filter
        var r2 = await InvokeAsync(mi, new FetchSetlistFmDataRequest { UserId = "user123", Filter = null! }, fake, ctx, CancellationToken.None);
        ((BadRequest<ApiError>)r2).Value!.Code.Should().Be("MISSING_FILTER");

        // Invalid date format
        var r3 = await InvokeAsync(mi, new FetchSetlistFmDataRequest { UserId = "user123", Filter = new SetlistFmFilter { StartDate = "bad", EndDate = "bad" } }, fake, ctx, CancellationToken.None);
        ((BadRequest<ApiError>)r3).Value!.Code.Should().Be("INVALID_DATE_FORMAT");

        // Invalid date range
        var r4 = await InvokeAsync(mi, new FetchSetlistFmDataRequest { UserId = "user123", Filter = new SetlistFmFilter { StartDate = "2025-12-31", EndDate = "2025-01-01" } }, fake, ctx, CancellationToken.None);
        ((BadRequest<ApiError>)r4).Value!.Code.Should().Be("INVALID_DATE_RANGE");
    }

    [Fact]
    public async Task PostFetchSetlistFmData_ServicePaths()
    {
        var mi = GetPrivate("PostFetchSetlistFmData");
        var ctx = ContextWithSessionCookie();

        // Service throws ArgumentException -> INVALID_FILTER
        var fake1 = new FakeSetlistFmService { OnGetUserConcertsAsync = (u, f, ct) => throw new ArgumentException("bad") };
        var filter = new SetlistFmFilter();
        var r1 = await InvokeAsync(mi, new FetchSetlistFmDataRequest { UserId = "user123", Filter = filter }, fake1, ctx, CancellationToken.None);
        ((BadRequest<ApiError>)r1).Value!.Code.Should().Be("INVALID_FILTER");

        // Service throws general exception -> SETLISTFM_FETCH_ERROR
        var fake2 = new FakeSetlistFmService { OnGetUserConcertsAsync = (u, f, ct) => throw new InvalidOperationException("oops") };
        var r2 = await InvokeAsync(mi, new FetchSetlistFmDataRequest { UserId = "user123", Filter = filter }, fake2, ctx, CancellationToken.None);
        var json = (JsonHttpResult<ApiError>)r2;
        json.StatusCode.Should().Be(500);
        json.Value!.Code.Should().Be("SETLISTFM_FETCH_ERROR");

        // Success -> Ok
        var fake3 = new FakeSetlistFmService { OnGetUserConcertsAsync = (u, f, ct) => Task.FromResult(new SetlistFmDataResponse { Concerts = [], Tracks = [] }) };
        var r3 = await InvokeAsync(mi, new FetchSetlistFmDataRequest { UserId = "user123", Filter = filter }, fake3, ctx, CancellationToken.None);
        r3.Should().BeOfType<Ok<SetlistFmDataResponse>>();
    }

    [Fact]
    public async Task PostFetchSetlistFmDataNormalized_ValidatesErrors()
    {
        var mi = GetPrivate("PostFetchSetlistFmDataNormalized");
        var ctx = ContextWithSessionCookie();
        var fake = new FakeSetlistFmService();

        // Missing user ID
        var r1 = await InvokeAsync(mi, new FetchSetlistFmDataRequest { UserId = "", Filter = new SetlistFmFilter() }, fake, ctx, CancellationToken.None);
        ((BadRequest<ApiError>)r1).Value!.Code.Should().Be("MISSING_USER_ID");

        // Missing filter
        var r2 = await InvokeAsync(mi, new FetchSetlistFmDataRequest { UserId = "user123", Filter = null! }, fake, ctx, CancellationToken.None);
        ((BadRequest<ApiError>)r2).Value!.Code.Should().Be("MISSING_FILTER");
    }

    [Fact]
    public async Task PostFetchSetlistFmDataNormalized_ServicePaths()
    {
        var mi = GetPrivate("PostFetchSetlistFmDataNormalized");
        var ctx = ContextWithSessionCookie();

        // Success -> Ok
        var fake = new FakeSetlistFmService { OnGetUserConcertsNormalizedAsync = (u, f, ct) => Task.FromResult(new NormalizedDataResponse { DataType = "Tracks", Tracks = [], Albums = [], Artists = [], Source = "setlistfm" }) };
        var filter = new SetlistFmFilter();
        var r = await InvokeAsync(mi, new FetchSetlistFmDataRequest { UserId = "user123", Filter = filter }, fake, ctx, CancellationToken.None);
        r.Should().BeOfType<Ok<NormalizedDataResponse>>();
    }

    [Fact]
    public async Task PostFetchDiscogsData_ValidatesErrors()
    {
        var mi = GetPrivate("PostFetchDiscogsData");
        var ctx = ContextWithSessionCookie();
        var fake = new FakeDiscogsService();

        // Missing username
        var r1 = await InvokeAsync(mi, new FetchDiscogsDataRequest { UsernameOrCollectionId = "", Filter = new DiscogsFilter { MaxTracks = 100 } }, fake, ctx, CancellationToken.None);
        ((BadRequest<ApiError>)r1).Value!.Code.Should().Be("MISSING_USERNAME");

        // Missing filter
        var r2 = await InvokeAsync(mi, new FetchDiscogsDataRequest { UsernameOrCollectionId = "user", Filter = null! }, fake, ctx, CancellationToken.None);
        ((BadRequest<ApiError>)r2).Value!.Code.Should().Be("MISSING_FILTER");

        // Invalid year range
        var r3 = await InvokeAsync(mi, new FetchDiscogsDataRequest { UsernameOrCollectionId = "user", Filter = new DiscogsFilter { MinReleaseYear = 2025, MaxReleaseYear = 2020 } }, fake, ctx, CancellationToken.None);
        ((BadRequest<ApiError>)r3).Value!.Code.Should().Be("INVALID_YEAR_RANGE");

        // Invalid year added range
        var r4 = await InvokeAsync(mi, new FetchDiscogsDataRequest { UsernameOrCollectionId = "user", Filter = new DiscogsFilter { MinYearAdded = 2025, MaxYearAdded = 2020 } }, fake, ctx, CancellationToken.None);
        ((BadRequest<ApiError>)r4).Value!.Code.Should().Be("INVALID_YEAR_ADDED_RANGE");
    }

    [Fact]
    public async Task PostFetchDiscogsData_ServicePaths()
    {
        var mi = GetPrivate("PostFetchDiscogsData");
        var ctx = ContextWithSessionCookie();

        // Service returns null -> DISCOGS_FETCH_FAILED
        var fake1 = new FakeDiscogsService { OnGetCollectionAsync = (u, f, ct) => Task.FromResult<DiscogsDataResponse?>(null) };
        var filter1 = new DiscogsFilter { MaxTracks = 100 };
        var r1 = await InvokeAsync(mi, new FetchDiscogsDataRequest { UsernameOrCollectionId = "user", Filter = filter1 }, fake1, ctx, CancellationToken.None);
        ((BadRequest<ApiError>)r1).Value!.Code.Should().Be("DISCOGS_FETCH_FAILED");

        // Service throws ArgumentException -> INVALID_FILTER
        var fake2 = new FakeDiscogsService { OnGetCollectionAsync = (u, f, ct) => throw new ArgumentException("bad") };
        var r2 = await InvokeAsync(mi, new FetchDiscogsDataRequest { UsernameOrCollectionId = "user", Filter = filter1 }, fake2, ctx, CancellationToken.None);
        ((BadRequest<ApiError>)r2).Value!.Code.Should().Be("INVALID_FILTER");

        // Service throws general exception -> DISCOGS_FETCH_ERROR
        var fake3 = new FakeDiscogsService { OnGetCollectionAsync = (u, f, ct) => throw new InvalidOperationException("oops") };
        var r3 = await InvokeAsync(mi, new FetchDiscogsDataRequest { UsernameOrCollectionId = "user", Filter = filter1 }, fake3, ctx, CancellationToken.None);
        var json = (JsonHttpResult<ApiError>)r3;
        json.StatusCode.Should().Be(500);
        json.Value!.Code.Should().Be("DISCOGS_FETCH_ERROR");

        // Success -> Ok
        var fake4 = new FakeDiscogsService { OnGetCollectionAsync = (u, f, ct) => Task.FromResult<DiscogsDataResponse?>(new DiscogsDataResponse { Releases = [], Tracks = [] }) };
        var r4 = await InvokeAsync(mi, new FetchDiscogsDataRequest { UsernameOrCollectionId = "user", Filter = filter1 }, fake4, ctx, CancellationToken.None);
        r4.Should().BeOfType<Ok<DiscogsDataResponse>>();
    }

    [Fact]
    public async Task PostFetchDiscogsDataNormalized_ValidatesErrors()
    {
        var mi = GetPrivate("PostFetchDiscogsDataNormalized");
        var ctx = ContextWithSessionCookie();
        var fake = new FakeDiscogsService();

        // Missing username
        var r1 = await InvokeAsync(mi, new FetchDiscogsDataRequest { UsernameOrCollectionId = "", Filter = new DiscogsFilter { MaxTracks = 100 } }, fake, ctx, CancellationToken.None);
        ((BadRequest<ApiError>)r1).Value!.Code.Should().Be("MISSING_USERNAME");

        // Missing filter
        var r2 = await InvokeAsync(mi, new FetchDiscogsDataRequest { UsernameOrCollectionId = "user", Filter = null! }, fake, ctx, CancellationToken.None);
        ((BadRequest<ApiError>)r2).Value!.Code.Should().Be("MISSING_FILTER");

        // Invalid year range
        var r3 = await InvokeAsync(mi, new FetchDiscogsDataRequest { UsernameOrCollectionId = "user", Filter = new DiscogsFilter { MinReleaseYear = 2025, MaxReleaseYear = 2020 } }, fake, ctx, CancellationToken.None);
        ((BadRequest<ApiError>)r3).Value!.Code.Should().Be("INVALID_YEAR_RANGE");
    }

    [Fact]
    public async Task PostFetchDiscogsDataNormalized_ServicePaths()
    {
        var mi = GetPrivate("PostFetchDiscogsDataNormalized");
        var ctx = ContextWithSessionCookie();

        // Service returns null -> DISCOGS_FETCH_FAILED
        var fake1 = new FakeDiscogsService { OnGetCollectionNormalizedAsync = (u, f, ct) => Task.FromResult<NormalizedDataResponse?>(null) };
        var filter1 = new DiscogsFilter { MaxTracks = 100 };
        var r1 = await InvokeAsync(mi, new FetchDiscogsDataRequest { UsernameOrCollectionId = "user", Filter = filter1 }, fake1, ctx, CancellationToken.None);
        ((BadRequest<ApiError>)r1).Value!.Code.Should().Be("DISCOGS_FETCH_FAILED");

        // Service throws ArgumentException -> INVALID_FILTER
        var fake2 = new FakeDiscogsService { OnGetCollectionNormalizedAsync = (u, f, ct) => throw new ArgumentException("bad") };
        var r2 = await InvokeAsync(mi, new FetchDiscogsDataRequest { UsernameOrCollectionId = "user", Filter = filter1 }, fake2, ctx, CancellationToken.None);
        ((BadRequest<ApiError>)r2).Value!.Code.Should().Be("INVALID_FILTER");

        // Service throws general exception -> DISCOGS_FETCH_ERROR
        var fake3 = new FakeDiscogsService { OnGetCollectionNormalizedAsync = (u, f, ct) => throw new InvalidOperationException("oops") };
        var r3 = await InvokeAsync(mi, new FetchDiscogsDataRequest { UsernameOrCollectionId = "user", Filter = filter1 }, fake3, ctx, CancellationToken.None);
        var json = (JsonHttpResult<ApiError>)r3;
        json.StatusCode.Should().Be(500);
        json.Value!.Code.Should().Be("DISCOGS_FETCH_ERROR");

        // Success -> Ok
        var fake4 = new FakeDiscogsService { OnGetCollectionNormalizedAsync = (u, f, ct) => Task.FromResult<NormalizedDataResponse?>(new NormalizedDataResponse { DataType = "Tracks", Tracks = [], Albums = [], Artists = [], Source = "discogs" }) };
        var r4 = await InvokeAsync(mi, new FetchDiscogsDataRequest { UsernameOrCollectionId = "user", Filter = filter1 }, fake4, ctx, CancellationToken.None);
        r4.Should().BeOfType<Ok<NormalizedDataResponse>>();
    }

    [Fact]
    public async Task PostFetchDiscogsData_WithYearFilter()
    {
        var mi = GetPrivate("PostFetchDiscogsData");
        var ctx = ContextWithSessionCookie();

        var fake = new FakeDiscogsService { OnGetCollectionAsync = (u, f, ct) => Task.FromResult<DiscogsDataResponse?>(new DiscogsDataResponse { Releases = [], Tracks = [] }) };
        var filter = new DiscogsFilter { MinReleaseYear = 1990, MaxReleaseYear = 2020, MaxTracks = 50 };
        var r = await InvokeAsync(mi, new FetchDiscogsDataRequest { UsernameOrCollectionId = "collector", Filter = filter }, fake, ctx, CancellationToken.None);
        r.Should().BeOfType<Ok<DiscogsDataResponse>>();
    }

    [Fact]
    public async Task PostFetchDiscogsData_WithMediaFormat()
    {
        var mi = GetPrivate("PostFetchDiscogsData");
        var ctx = ContextWithSessionCookie();

        var fake = new FakeDiscogsService { OnGetCollectionAsync = (u, f, ct) => Task.FromResult<DiscogsDataResponse?>(new DiscogsDataResponse { Releases = [], Tracks = [] }) };
        var filter = new DiscogsFilter { MediaFormat = DiscogsMediaFormat.Vinyl, MaxTracks = 100 };
        var r = await InvokeAsync(mi, new FetchDiscogsDataRequest { UsernameOrCollectionId = "vinylcollector", Filter = filter }, fake, ctx, CancellationToken.None);
        r.Should().BeOfType<Ok<DiscogsDataResponse>>();
    }

    [Fact]
    public async Task PostFetchDiscogsData_WithYearAddedFilter()
    {
        var mi = GetPrivate("PostFetchDiscogsData");
        var ctx = ContextWithSessionCookie();

        var fake = new FakeDiscogsService { OnGetCollectionAsync = (u, f, ct) => Task.FromResult<DiscogsDataResponse?>(new DiscogsDataResponse { Releases = [], Tracks = [] }) };
        var filter = new DiscogsFilter { MinYearAdded = 2023, MaxYearAdded = 2025, MaxTracks = 100 };
        var r = await InvokeAsync(mi, new FetchDiscogsDataRequest { UsernameOrCollectionId = "user", Filter = filter }, fake, ctx, CancellationToken.None);
        r.Should().BeOfType<Ok<DiscogsDataResponse>>();
    }

    [Fact]
    public async Task PostFetchDiscogsDataNormalized_WithYearFilter()
    {
        var mi = GetPrivate("PostFetchDiscogsDataNormalized");
        var ctx = ContextWithSessionCookie();

        var fake = new FakeDiscogsService { OnGetCollectionNormalizedAsync = (u, f, ct) => Task.FromResult<NormalizedDataResponse?>(new NormalizedDataResponse { DataType = "Tracks", Tracks = [], Albums = [], Artists = [], Source = "discogs" }) };
        var filter = new DiscogsFilter { MinReleaseYear = 1975, MaxReleaseYear = 2000, MaxTracks = 200 };
        var r = await InvokeAsync(mi, new FetchDiscogsDataRequest { UsernameOrCollectionId = "retroenthusiast", Filter = filter }, fake, ctx, CancellationToken.None);
        r.Should().BeOfType<Ok<NormalizedDataResponse>>();
    }
}
