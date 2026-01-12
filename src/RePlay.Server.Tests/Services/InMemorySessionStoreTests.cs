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
