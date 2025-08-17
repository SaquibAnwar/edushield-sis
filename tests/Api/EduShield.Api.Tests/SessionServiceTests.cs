using EduShield.Api.Services;
using EduShield.Core.Configuration;
using EduShield.Core.Entities;
using EduShield.Core.Interfaces;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Moq;
using NUnit.Framework;

namespace EduShield.Api.Tests;

[TestFixture]
public class SessionServiceTests
{
    private Mock<ISessionRepo> _mockSessionRepo;
    private Mock<ILogger<SessionService>> _mockLogger;
    private SessionService _sessionService;

    [SetUp]
    public void Setup()
    {
        _mockSessionRepo = new Mock<ISessionRepo>();
        _mockLogger = new Mock<ILogger<SessionService>>();
        
        var mockAuthConfig = new Mock<IOptions<AuthenticationConfiguration>>();
        mockAuthConfig.Setup(x => x.Value).Returns(new AuthenticationConfiguration
        {
            SessionTimeoutMinutes = 60
        });
        
        _sessionService = new SessionService(
            _mockSessionRepo.Object,
            mockAuthConfig.Object,
            _mockLogger.Object
        );
    }

    [Test]
    public async Task CreateSessionAsync_ValidUserId_CreatesSession()
    {
        // Arrange
        var userId = Guid.NewGuid();
        var createdSession = new UserSession(
            Guid.NewGuid(),
            userId,
            "session-token",
            DateTime.UtcNow.AddHours(1),
            DateTime.UtcNow
        );

        _mockSessionRepo.Setup(x => x.CreateAsync(It.IsAny<UserSession>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(createdSession);

        // Act
        var result = await _sessionService.CreateSessionAsync(userId, "127.0.0.1", "test-user-agent", null, CancellationToken.None);

        // Assert
        Assert.That(result, Is.Not.Null);
        Assert.That(result.UserId, Is.EqualTo(userId));
        Assert.That(result.Token, Is.Not.Null.And.Not.Empty);
        
        _mockSessionRepo.Verify(x => x.CreateAsync(It.IsAny<UserSession>(), It.IsAny<CancellationToken>()), Times.Once);
    }

    [Test]
    public async Task ValidateSessionAsync_ValidToken_ReturnsSession()
    {
        // Arrange
        var sessionToken = "valid-token";
        var userSession = new UserSession(
            Guid.NewGuid(),
            Guid.NewGuid(),
            sessionToken,
            DateTime.UtcNow.AddHours(1),
            DateTime.UtcNow
        );

        _mockSessionRepo.Setup(x => x.GetByTokenAsync(sessionToken, It.IsAny<CancellationToken>()))
            .ReturnsAsync(userSession);

        // Act
        var result = await _sessionService.ValidateSessionAsync(sessionToken, CancellationToken.None);

        // Assert
        Assert.That(result, Is.Not.Null);
        Assert.That(result.Token, Is.EqualTo(sessionToken));
    }

    [Test]
    public async Task ValidateSessionAsync_ExpiredToken_ReturnsNull()
    {
        // Arrange
        var sessionToken = "expired-token";
        var expiredSession = new UserSession(
            Guid.NewGuid(),
            Guid.NewGuid(),
            sessionToken,
            DateTime.UtcNow.AddHours(-1), // Expired
            DateTime.UtcNow.AddHours(-2)
        );

        _mockSessionRepo.Setup(x => x.GetByTokenAsync(sessionToken, It.IsAny<CancellationToken>()))
            .ReturnsAsync(expiredSession);

        // Act
        var result = await _sessionService.ValidateSessionAsync(sessionToken, CancellationToken.None);

        // Assert
        Assert.That(result, Is.Null);
    }

    [Test]
    public async Task InvalidateSessionAsync_ValidSessionId_InvalidatesSession()
    {
        // Arrange
        var sessionId = Guid.NewGuid();

        _mockSessionRepo.Setup(x => x.DeleteAsync(sessionId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(true);

        // Act
        var result = await _sessionService.InvalidateSessionAsync(sessionId, CancellationToken.None);

        // Assert
        Assert.That(result, Is.True);
        _mockSessionRepo.Verify(x => x.DeleteAsync(sessionId, It.IsAny<CancellationToken>()), Times.Once);
    }

    // Test removed - CleanupExpiredSessionsAsync method doesn't exist in SessionService
}