using FluentAssertions;
using RePlay.Server.Models;

namespace RePlay.Server.Tests.Models;

public class AuthSessionTests
{
    [Fact]
    public void IsExpired_ShouldReturnTrue_WhenExpired()
    {
        // Arrange
        var session = new AuthSession
        {
            SessionId = "test",
            AccessToken = "token",
            RefreshToken = "refresh",
            ExpiresAt = DateTime.UtcNow.AddMinutes(-1), // Expired 1 minute ago
            User = new SpotifyUser { Id = "user123", DisplayName = "Test" }
        };

        // Act
        var isExpired = session.IsExpired();

        // Assert
        isExpired.Should().BeTrue();
    }

    [Fact]
    public void IsExpired_ShouldReturnTrue_WhenWithinBuffer()
    {
        // Arrange
        var session = new AuthSession
        {
            SessionId = "test",
            AccessToken = "token",
            RefreshToken = "refresh",
            ExpiresAt = DateTime.UtcNow.AddMinutes(3), // Expires in 3 minutes
            User = new SpotifyUser { Id = "user123", DisplayName = "Test" }
        };

        // Act
        var isExpired = session.IsExpired(); // Default buffer is 5 minutes

        // Assert
        isExpired.Should().BeTrue();
    }

    [Fact]
    public void IsExpired_ShouldReturnFalse_WhenNotExpired()
    {
        // Arrange
        var session = new AuthSession
        {
            SessionId = "test",
            AccessToken = "token",
            RefreshToken = "refresh",
            ExpiresAt = DateTime.UtcNow.AddHours(1),
            User = new SpotifyUser { Id = "user123", DisplayName = "Test" }
        };

        // Act
        var isExpired = session.IsExpired();

        // Assert
        isExpired.Should().BeFalse();
    }

    [Fact]
    public void IsExpired_ShouldUseCustomBuffer()
    {
        // Arrange
        var session = new AuthSession
        {
            SessionId = "test",
            AccessToken = "token",
            RefreshToken = "refresh",
            ExpiresAt = DateTime.UtcNow.AddMinutes(8), // Expires in 8 minutes
            User = new SpotifyUser { Id = "user123", DisplayName = "Test" }
        };

        // Act
        var isExpired = session.IsExpired(TimeSpan.FromMinutes(10));

        // Assert
        isExpired.Should().BeTrue();
    }
}
