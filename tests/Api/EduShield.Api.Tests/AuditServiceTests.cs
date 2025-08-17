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
public class AuditServiceTests
{
    private Mock<IAuditRepo> _mockAuditRepo;
    private Mock<ILogger<AuditService>> _mockLogger;
    private AuditService _auditService;

    [SetUp]
    public void Setup()
    {
        _mockAuditRepo = new Mock<IAuditRepo>();
        _mockLogger = new Mock<ILogger<AuditService>>();
        
        var mockAuthConfig = new Mock<IOptions<AuthenticationConfiguration>>();
        mockAuthConfig.Setup(x => x.Value).Returns(new AuthenticationConfiguration());
        
        _auditService = new AuditService(
            _mockAuditRepo.Object,
            mockAuthConfig.Object,
            _mockLogger.Object
        );
    }

    [Test]
    public async Task LogAsync_ValidInput_CreatesAuditLog()
    {
        // Arrange
        var action = "UserLogin";
        var details = "User logged in successfully";
        var userId = Guid.NewGuid();
        var ipAddress = "127.0.0.1";
        var userAgent = "test-agent";

        var createdAuditLog = new AuditLog
        {
            AuditId = Guid.NewGuid(),
            Action = action,
            Resource = "User",
            IpAddress = ipAddress,
            UserAgent = userAgent,
            Success = true,
            AdditionalData = details,
            UserId = userId,
            CreatedAt = DateTime.UtcNow
        };

        _mockAuditRepo.Setup(x => x.CreateAsync(It.IsAny<AuditLog>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(createdAuditLog);

        // Act
        await _auditService.LogAsync(action, details, userId, ipAddress, userAgent);

        // Assert
        _mockAuditRepo.Verify(x => x.CreateAsync(It.Is<AuditLog>(log =>
            log.Action == action &&
            log.AdditionalData == details &&
            log.UserId == userId &&
            log.IpAddress == ipAddress &&
            log.UserAgent == userAgent), It.IsAny<CancellationToken>()), Times.Once);
    }

    [Test]
    public async Task LogAsync_WithoutUserId_CreatesAuditLog()
    {
        // Arrange
        var action = "SystemEvent";
        var details = "System maintenance started";
        var ipAddress = "127.0.0.1";
        var userAgent = "system";

        var createdAuditLog = new AuditLog
        {
            AuditId = Guid.NewGuid(),
            Action = action,
            Resource = "System",
            IpAddress = ipAddress,
            UserAgent = userAgent,
            Success = true,
            AdditionalData = details,
            UserId = null,
            CreatedAt = DateTime.UtcNow
        };

        _mockAuditRepo.Setup(x => x.CreateAsync(It.IsAny<AuditLog>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(createdAuditLog);

        // Act
        await _auditService.LogAsync(action, details, null, ipAddress, userAgent);

        // Assert
        _mockAuditRepo.Verify(x => x.CreateAsync(It.Is<AuditLog>(log =>
            log.Action == action &&
            log.AdditionalData == details &&
            log.UserId == null &&
            log.IpAddress == ipAddress &&
            log.UserAgent == userAgent), It.IsAny<CancellationToken>()), Times.Once);
    }

    [Test]
    public async Task GetUserAuditLogsAsync_ValidUserId_ReturnsUserLogs()
    {
        // Arrange
        var userId = Guid.NewGuid();
        var auditLogs = new List<AuditLog>
        {
            new AuditLog { AuditId = Guid.NewGuid(), UserId = userId, Action = "UserLogin", CreatedAt = DateTime.UtcNow },
            new AuditLog { AuditId = Guid.NewGuid(), UserId = userId, Action = "UserLogout", CreatedAt = DateTime.UtcNow.AddMinutes(-30) }
        };

        _mockAuditRepo.Setup(x => x.GetByUserIdAsync(userId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(auditLogs);

        // Act
        var result = await _auditService.GetUserAuditLogsAsync(userId, CancellationToken.None);

        // Assert
        Assert.That(result, Is.Not.Null);
        Assert.That(result.Count(), Is.EqualTo(2));
        Assert.That(result.All(log => log.UserId == userId), Is.True);
    }
}