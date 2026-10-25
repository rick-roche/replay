using FluentAssertions;
using RePlay.Server.Models;
using RePlay.Server.Services;

namespace RePlay.Server.Tests.Services;

public class InMemorySessionStoreTests
{
    private readonly InMemorySessionStore _store;

    public InMemorySessionStoreTests()
    {
        _store = new InMemorySessionStore();
    }

    [Fact]
    public async Task RejectedRefresh_RemovesSessionBeforeQueuedRefreshCanRun()
    {
        var session = CreateTestSession();
        _store.StoreSession(session);
        var started = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);
        var release = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);
        var rejected = _store.RefreshSessionAsync(session.SessionId, async (_, _) =>
        {
            started.SetResult();
            await release.Task;
            throw new SpotifyRefreshTokenRejectedException();
        });
        await started.Task;
        var providerCalled = false;
        var queued = _store.RefreshSessionAsync(session.SessionId, (_, _) =>
        {
            providerCalled = true;
            return Task.FromResult(CreateTestSession());
        });
        release.SetResult();
        await Assert.ThrowsAsync<SpotifyRefreshTokenRejectedException>(() => rejected);
        var result = await queued;

        providerCalled.Should().BeFalse();
        result.Should().BeNull();
        _store.GetSession(session.SessionId).Should().BeNull();
    }

    [Fact]
    public void BackgroundCleanup_RemovesUnvisitedExpiredSessions()
    {
        var session = CreateTestSession() with { CreatedAt = DateTime.UtcNow.AddDays(-31) };
        _store.StoreSession(session);
        var cleanup = typeof(InMemorySessionStore).GetMethod("CleanupExpiredSessions",
            System.Reflection.BindingFlags.Instance | System.Reflection.BindingFlags.NonPublic);
        cleanup.Should().NotBeNull();

        cleanup!.Invoke(_store, null);

        var sessions = (System.Collections.IDictionary)typeof(InMemorySessionStore)
            .GetField("_sessions", System.Reflection.BindingFlags.Instance | System.Reflection.BindingFlags.NonPublic)!.GetValue(_store)!;
        sessions.Count.Should().Be(0);
    }

    [Fact]
    public void ExpiredApplicationSession_RemovesSessionAndConfiguration()
    {
        var session = CreateTestSession() with { CreatedAt = DateTime.UtcNow.AddDays(-31) };
        _store.StoreSession(session);
        _store.StoreSourceConfig(session.SessionId, new ExternalSourceConfig
        {
            Source = "lastfm", ConfigValue = "listener", ConfiguredAt = DateTime.UtcNow
        });

        _store.GetSession(session.SessionId).Should().BeNull();
        _store.GetSourceConfig(session.SessionId, "lastfm").Should().BeNull();
    }

    [Fact]
    public async Task Refresh_ExpiredApplicationSession_DoesNotCallProvider()
    {
        var session = CreateTestSession() with { CreatedAt = DateTime.UtcNow.AddDays(-31) };
        _store.StoreSession(session);
        var called = false;

        var refreshed = await _store.RefreshSessionAsync(session.SessionId, (_, _) =>
        {
            called = true;
            return Task.FromResult(CreateTestSession());
        });

        called.Should().BeFalse();
        refreshed.Should().BeNull();
    }

    [Fact]
    public async Task ConcurrentRefreshes_UseLatestRotatedToken_AndPreserveAbsoluteLifetime()
    {
        var session = CreateTestSession() with { CreatedAt = DateTime.UtcNow.AddDays(-20) };
        _store.StoreSession(session);
        var started = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);
        var release = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);
        var tokens = new List<string>();
        var first = _store.RefreshSessionAsync(session.SessionId, async (token, _) =>
        {
            tokens.Add(token);
            started.SetResult();
            await release.Task;
            return CreateTestSession() with { RefreshToken = "rotated" };
        });
        await started.Task;
        var second = _store.RefreshSessionAsync(session.SessionId, (token, _) =>
        {
            tokens.Add(token);
            return Task.FromResult(CreateTestSession() with { RefreshToken = "rotated-again" });
        });
        tokens.Should().Equal("test-refresh-token");
        release.SetResult();
        await Task.WhenAll(first, second);

        tokens.Should().Equal("test-refresh-token", "rotated");
        _store.GetSession(session.SessionId)!.CreatedAt.Should().Be(session.CreatedAt);
    }

    [Fact]
    public void StoreSession_ShouldStoreSession()
    {
        // Arrange
        var session = CreateTestSession();

        // Act
        _store.StoreSession(session);

        // Assert
        var retrieved = _store.GetSession(session.SessionId);
        retrieved.Should().NotBeNull();
        retrieved.Should().BeEquivalentTo(session);
    }

    [Fact]
    public void GetSession_ShouldReturnNull_WhenSessionDoesNotExist()
    {
        // Act
        var session = _store.GetSession("non-existent-id");

        // Assert
        session.Should().BeNull();
    }

    [Fact]
    public void RemoveSession_ShouldRemoveSession()
    {
        // Arrange
        var session = CreateTestSession();
        _store.StoreSession(session);

        // Act
        _store.RemoveSession(session.SessionId);

        // Assert
        var retrieved = _store.GetSession(session.SessionId);
        retrieved.Should().BeNull();
    }

    [Fact]
    public void StoreSession_ShouldOverwriteExistingSession()
    {
        // Arrange
        var sessionId = "test-session-id";
        var session1 = CreateTestSession(sessionId, "token1");
        var session2 = CreateTestSession(sessionId, "token2");

        // Act
        _store.StoreSession(session1);
        _store.StoreSession(session2);

        // Assert
        var retrieved = _store.GetSession(sessionId);
        retrieved.Should().NotBeNull();
        retrieved!.AccessToken.Should().Be("token2");
    }

    private static AuthSession CreateTestSession(string? sessionId = null, string? accessToken = null)
    {
        return new AuthSession
        {
            SessionId = sessionId ?? Guid.NewGuid().ToString(),
            AccessToken = accessToken ?? "test-access-token",
            RefreshToken = "test-refresh-token",
            ExpiresAt = DateTime.UtcNow.AddHours(1),
            User = new SpotifyUser
            {
                Id = "user123",
                DisplayName = "Test User",
                Email = "test@example.com"
            }
        };
    }
}
