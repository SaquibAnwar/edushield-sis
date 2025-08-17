using EduShield.Api.Services;
using EduShield.Core.Dtos;
using EduShield.Core.Entities;
using EduShield.Core.Enums;
using EduShield.Core.Interfaces;
using Microsoft.Extensions.Logging;
using Moq;
using NUnit.Framework;

namespace EduShield.Api.Tests;

[TestFixture]
public class UserServiceTests
{
    private Mock<IUserRepo> _mockUserRepo;
    private Mock<IAuditService> _mockAuditService;
    private Mock<ILogger<UserService>> _mockLogger;
    private UserService _userService;

    [SetUp]
    public void Setup()
    {
        _mockUserRepo = new Mock<IUserRepo>();
        _mockAuditService = new Mock<IAuditService>();
        _mockLogger = new Mock<ILogger<UserService>>();
        
        _userService = new UserService(
            _mockUserRepo.Object,
            _mockAuditService.Object,
            _mockLogger.Object
        );
    }

    [Test]
    public async Task CreateUserFromExternalAsync_ValidInfo_CreatesUser()
    {
        // Arrange
        var externalUserInfo = new ExternalUserInfo
        {
            Id = "external-123",
            Email = "test@example.com",
            Name = "Test User",
            Provider = AuthProvider.Google
        };

        var createdUser = new User
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

        _mockUserRepo.Setup(x => x.CreateAsync(It.IsAny<User>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(createdUser);

        // Act
        var result = await _userService.CreateUserFromExternalAsync(externalUserInfo, CancellationToken.None);

        // Assert
        Assert.That(result, Is.Not.Null);
        Assert.That(result.Email, Is.EqualTo(externalUserInfo.Email));
        Assert.That(result.ExternalId, Is.EqualTo(externalUserInfo.Id));
        
        _mockUserRepo.Verify(x => x.CreateAsync(It.IsAny<User>(), It.IsAny<CancellationToken>()), Times.Once);
    }

    [Test]
    public async Task UpdateUserAsync_ValidRequest_UpdatesUser()
    {
        // Arrange
        var userId = Guid.NewGuid();
        var updateRequest = new UpdateUserRequest
        {
            Email = "updated@example.com",
            FirstName = "Updated",
            LastName = "User",
            Role = UserRole.Teacher,
            IsActive = true
        };

        var existingUser = new User
        {
            UserId = userId,
            Email = "old@example.com",
            FirstName = "Old",
            LastName = "User",
            Role = UserRole.Student,
            IsActive = true
        };

        var updatedUser = new User
        {
            UserId = userId,
            Email = updateRequest.Email,
            FirstName = updateRequest.FirstName,
            LastName = updateRequest.LastName,
            Role = updateRequest.Role.Value,
            IsActive = updateRequest.IsActive.Value
        };

        _mockUserRepo.Setup(x => x.GetByIdAsync(userId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(existingUser);
        
        _mockUserRepo.Setup(x => x.UpdateAsync(It.IsAny<User>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(updatedUser);

        // Act
        var result = await _userService.UpdateUserAsync(userId, updateRequest, CancellationToken.None);

        // Assert
        Assert.That(result, Is.Not.Null);
        Assert.That(result.Email, Is.EqualTo(updateRequest.Email));
        Assert.That(result.FirstName, Is.EqualTo(updateRequest.FirstName));
        
        _mockUserRepo.Verify(x => x.UpdateAsync(It.IsAny<User>(), It.IsAny<CancellationToken>()), Times.Once);
    }

    [Test]
    public async Task GetUserByIdAsync_ExistingUser_ReturnsUser()
    {
        // Arrange
        var userId = Guid.NewGuid();
        var user = new User
        {
            UserId = userId,
            Email = "test@example.com",
            FirstName = "Test",
            LastName = "User",
            Role = UserRole.Student,
            IsActive = true
        };

        _mockUserRepo.Setup(x => x.GetByIdAsync(userId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(user);

        // Act
        var result = await _userService.GetUserByIdAsync(userId, CancellationToken.None);

        // Assert
        Assert.That(result, Is.Not.Null);
        Assert.That(result.UserId, Is.EqualTo(userId));
        Assert.That(result.Email, Is.EqualTo(user.Email));
    }

    [Test]
    public async Task GetUserByIdAsync_NonExistingUser_ReturnsNull()
    {
        // Arrange
        var userId = Guid.NewGuid();

        _mockUserRepo.Setup(x => x.GetByIdAsync(userId, It.IsAny<CancellationToken>()))
            .ReturnsAsync((User)null);

        // Act
        var result = await _userService.GetUserByIdAsync(userId, CancellationToken.None);

        // Assert
        Assert.That(result, Is.Null);
    }
}