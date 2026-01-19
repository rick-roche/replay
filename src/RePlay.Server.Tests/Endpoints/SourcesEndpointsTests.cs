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
}
