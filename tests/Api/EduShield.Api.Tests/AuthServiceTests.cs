using EduShield.Api.Services;
using EduShield.Core.Configuration;
using EduShield.Core.Dtos;
using EduShield.Core.Entities;
using EduShield.Core.Enums;
using EduShield.Core.Interfaces;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Moq;
using NUnit.Framework;

namespace EduShield.Api.Tests;

[TestFixture]
public class AuthServiceTests
{
    private Mock<IUserRepo> _mockUserRepo;
    private Mock<ISessionService> _mockSessionService;
    private Mock<IAuditService> _mockAuditService;
    private Mock<ILogger<AuthService>> _mockLogger;
    private AuthService _authService;

    [SetUp]
    public void Setup()
    {
        _mockUserRepo = new Mock<IUserRepo>();
        _mockSessionService = new Mock<ISessionService>();
        _mockAuditService = new Mock<IAuditService>();
        _mockLogger = new Mock<ILogger<AuthService>>();
        
        var mockAuthConfig = new Mock<IOptions<AuthenticationConfiguration>>();
        mockAuthConfig.Setup(x => x.Value).Returns(new AuthenticationConfiguration
        {
            SessionTimeoutMinutes = 60
        });
        
        _authService = new AuthService(
            _mockUserRepo.Object,
            _mockSessionService.Object,
            _mockAuditService.Object,
            mockAuthConfig.Object,
            _mockLogger.Object
        );
    }

    [Test]
    public async Task AuthenticateExternalUserAsync_NewUser_CreatesUserAndSession()
    {
        // Arrange
        var externalUserInfo = new ExternalUserInfo
        {
            Id = "external-123",
            Email = "test@example.com",
            Name = "Test User",
            Provider = AuthProvider.Google
        };

        var newUser = new User
        {
            UserId = Guid.NewGuid(),
            Email = externalUserInfo.Email,
            FirstName = "Test",
            LastName = "User",
            ExternalId = externalUserInfo.Id,
            Provider = externalUserInfo.Provider,
            Role = UserRole.Student,
            IsActive = true
        };

        var userSession = new UserSession
        {
            SessionId = Guid.NewGuid(),
            UserId = newUser.UserId,
            Token = "session-token",
            ExpiresAt = DateTime.UtcNow.AddHours(1),
            CreatedAt = DateTime.UtcNow
        };

        _mockUserRepo.Setup(x => x.GetByExternalIdAsync(externalUserInfo.Id, externalUserInfo.Provider.ToString(), It.IsAny<CancellationToken>()))
            .ReturnsAsync((User)null);
        
        _mockUserRepo.Setup(x => x.CreateAsync(It.IsAny<User>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(newUser);
        
        _mockSessionService.Setup(x => x.CreateSessionAsync(newUser.UserId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(userSession);

        // Act
        var result = await _authService.AuthenticateExternalUserAsync(externalUserInfo, "127.0.0.1", "test-agent", CancellationToken.None);

        // Assert
        Assert.That(result, Is.Not.Null);
        Assert.That(result.IsSuccess, Is.True);
        Assert.That(result.User.Email, Is.EqualTo(externalUserInfo.Email));
        
        _mockUserRepo.Verify(x => x.CreateAsync(It.IsAny<User>(), It.IsAny<CancellationToken>()), Times.Once);
        _mockSessionService.Verify(x => x.CreateSessionAsync(It.IsAny<Guid>(), It.IsAny<CancellationToken>()), Times.Once);
    }

    [Test]
    public async Task AuthenticateExternalUserAsync_ExistingUser_CreatesSession()
    {
        // Arrange
        var externalUserInfo = new ExternalUserInfo
        {
            Id = "external-123",
            Email = "test@example.com",
            Name = "Test User",
            Provider = AuthProvider.Google
        };

        var existingUser = new User
        {
            UserId = Guid.NewGuid(),
            Email = externalUserInfo.Email,
            FirstName = "Test",
            LastName = "User",
            ExternalId = externalUserInfo.Id,
            Provider = externalUserInfo.Provider,
            Role = UserRole.Student,
            IsActive = true
        };

        var userSession = new UserSession
        {
            SessionId = Guid.NewGuid(),
            UserId = existingUser.UserId,
            Token = "session-token",
            ExpiresAt = DateTime.UtcNow.AddHours(1),
            CreatedAt = DateTime.UtcNow
        };

        _mockUserRepo.Setup(x => x.GetByExternalIdAsync(externalUserInfo.Id, externalUserInfo.Provider.ToString(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(existingUser);
        
        _mockSessionService.Setup(x => x.CreateSessionAsync(existingUser.UserId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(userSession);

        // Act
        var result = await _authService.AuthenticateExternalUserAsync(externalUserInfo, "127.0.0.1", "test-agent", CancellationToken.None);

        // Assert
        Assert.That(result, Is.Not.Null);
        Assert.That(result.IsSuccess, Is.True);
        Assert.That(result.User.Email, Is.EqualTo(externalUserInfo.Email));
        
        _mockUserRepo.Verify(x => x.CreateAsync(It.IsAny<User>(), It.IsAny<CancellationToken>()), Times.Never);
        _mockSessionService.Verify(x => x.CreateSessionAsync(existingUser.UserId, It.IsAny<CancellationToken>()), Times.Once);
    }

    [Test]
    public async Task LogoutAsync_ValidSession_InvalidatesSession()
    {
        // Arrange
        var userId = Guid.NewGuid();
        var sessionId = Guid.NewGuid();
        var sessionToken = "session-token";

        _mockSessionService.Setup(x => x.InvalidateSessionAsync(sessionId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(true);

        // Act
        var result = await _authService.LogoutAsync(sessionId, sessionToken, CancellationToken.None);

        // Assert
        Assert.That(result, Is.True);
        _mockSessionService.Verify(x => x.InvalidateSessionAsync(sessionId, It.IsAny<CancellationToken>()), Times.Once);
    }
}