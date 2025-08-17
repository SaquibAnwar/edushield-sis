using EduShield.Api.Controllers;
using EduShield.Core.Entities;
using EduShield.Core.Enums;
using EduShield.Core.Interfaces;
using EduShield.Core.Dtos;
using EduShield.Core.Exceptions;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;
using Moq;
using System.Security.Claims;

namespace EduShield.Api.Tests;

[TestFixture]
public class UserControllerTests
{
    private Mock<IUserService> _mockUserService;
    private Mock<ISessionService> _mockSessionService;
    private Mock<IAuditService> _mockAuditService;
    private Mock<ILogger<UserController>> _mockLogger;
    private UserController _controller;

    [SetUp]
    public void Setup()
    {
        _mockUserService = new Mock<IUserService>();
        _mockSessionService = new Mock<ISessionService>();
        _mockAuditService = new Mock<IAuditService>();
        _mockLogger = new Mock<ILogger<UserController>>();

        _controller = new UserController(
            _mockUserService.Object,
            _mockSessionService.Object,
            _mockAuditService.Object,
            _mockLogger.Object
        );

        // Setup admin user context
        var claims = new List<Claim>
        {
            new Claim(ClaimTypes.NameIdentifier, Guid.NewGuid().ToString()),
            new Claim(ClaimTypes.Role, UserRole.SystemAdmin.ToString())
        };

        _controller.ControllerContext = new ControllerContext
        {
            HttpContext = new DefaultHttpContext
            {
                User = new ClaimsPrincipal(new ClaimsIdentity(claims, "test"))
            }
        };
    }

    [Test]
    public async Task GetAllUsers_ReturnsAllUsers()
    {
        // Arrange
        var users = new List<User>
        {
            new User
            {
                UserId = Guid.NewGuid(),
                Email = "test@example.com",
                FirstName = "Test",
                LastName = "User",
                Role = UserRole.Student,
                IsActive = true,
                Provider = AuthProvider.Google
            },
            new User
            {
                UserId = Guid.NewGuid(),
                Email = "test@example.com",
                FirstName = "Test",
                LastName = "User",
                Role = UserRole.Student,
                IsActive = true,
                Provider = AuthProvider.Google
            }
        };

        _mockUserService.Setup(x => x.GetAllAsync(It.IsAny<CancellationToken>())).ReturnsAsync(users);

        // Act
        var result = await _controller.GetAllUsers();

        // Assert
        var okResult = result.Result as OkObjectResult;
        Assert.That(okResult, Is.Not.Null);
        Assert.That(okResult.StatusCode, Is.EqualTo(200));

        var returnedUsers = okResult.Value as IEnumerable<UserProfileDto>;
        Assert.That(returnedUsers, Is.Not.Null);
        Assert.That(returnedUsers.Count(), Is.EqualTo(2));
    }

    [Test]
    public async Task GetUser_ExistingUser_ReturnsUser()
    {
        // Arrange
        var userId = Guid.NewGuid();
        var user = new User
            {
                UserId = Guid.NewGuid(),
                Email = "test@example.com",
                FirstName = "Test",
                LastName = "User",
                Role = UserRole.Student,
                IsActive = true,
                Provider = AuthProvider.Google
            };

        _mockUserService.Setup(x => x.GetByIdAsync(It.IsAny<CancellationToken>())).ReturnsAsync(user);

        // Act
        var result = await _controller.GetUser(userId);

        // Assert
        var okResult = result.Result as OkObjectResult;
        Assert.That(okResult, Is.Not.Null);
        Assert.That(okResult.StatusCode, Is.EqualTo(200));

        var returnedUser = okResult.Value as UserProfileDto;
        Assert.That(returnedUser, Is.Not.Null);
        Assert.That(returnedUser.Id, Is.EqualTo(userId));
        Assert.That(returnedUser.Email, Is.EqualTo("test@example.com"));
    }

    [Test]
    public async Task GetUser_NonExistingUser_ReturnsNotFound()
    {
        // Arrange
        var userId = Guid.NewGuid();

        _mockUserService.Setup(x => x.GetByIdAsync(userId))
            .ReturnsAsync((User?)null);

        // Act
        var result = await _controller.GetUser(userId);

        // Assert
        var notFoundResult = result as NotFoundResult;
        Assert.That(notFoundResult, Is.Not.Null);
        Assert.That(notFoundResult.StatusCode, Is.EqualTo(404));
    }

    [Test]
    public async Task CreateUser_ValidRequest_ReturnsCreatedUser()
    {
        // Arrange
        var createRequest = new CreateUserRequest { Email = "test@example.com", FirstName = "Test", LastName = "User", Role = UserRole.Student };

        var createdUser = new User
            {
                UserId = Guid.NewGuid(),
                Email = "test@example.com",
                FirstName = "Test",
                LastName = "User",
                Role = UserRole.Student,
                IsActive = true,
                Provider = AuthProvider.Google
            };

        _mockUserService.Setup(x => x.CreateAsync(It.IsAny<CancellationToken>())).ReturnsAsync(createdUser);

        // Act
        var result = await _controller.CreateUser(createRequest);

        // Assert
        var createdResult = result as CreatedAtActionResult;
        Assert.That(createdResult, Is.Not.Null);
        Assert.That(createdResult.StatusCode, Is.EqualTo(201));

        var returnedUser = createdResult.Value as UserProfileDto;
        Assert.That(returnedUser, Is.Not.Null);
        Assert.That(returnedUser.Email, Is.EqualTo(createRequest.Email));
        Assert.That(returnedUser.Name, Is.EqualTo(createRequest.Name));
        Assert.That(returnedUser.Role, Is.EqualTo(createRequest.Role));
    }

    [Test]
    public async Task CreateUser_InvalidRequest_ReturnsBadRequest()
    {
        // Arrange
        var createRequest = new CreateUserRequest { Email = "test@example.com", FirstName = "Test", LastName = "User", Role = UserRole.Student };

        _controller.ModelState.AddModelError("Email", "Email is required");

        // Act
        var result = await _controller.CreateUser(createRequest);

        // Assert
        var badRequestResult = result as BadRequestObjectResult;
        Assert.That(badRequestResult, Is.Not.Null);
        Assert.That(badRequestResult.StatusCode, Is.EqualTo(400));
    }

    [Test]
    public async Task UpdateUser_ValidRequest_ReturnsUpdatedUser()
    {
        // Arrange
        var userId = Guid.NewGuid();
        var updateRequest = new UpdateUserRequest("Updated Name");

        var updatedUser = new User
            {
                UserId = Guid.NewGuid(),
                Email = "test@example.com",
                FirstName = "Test",
                LastName = "User",
                Role = UserRole.Student,
                IsActive = true,
                Provider = AuthProvider.Google
            };

        _mockUserService.Setup(x => x.UpdateAsync(It.IsAny<CancellationToken>())).ReturnsAsync(updatedUser);

        // Act
        var result = await _controller.UpdateUser(userId, updateRequest);

        // Assert
        var okResult = result.Result as OkObjectResult;
        Assert.That(okResult, Is.Not.Null);
        Assert.That(okResult.StatusCode, Is.EqualTo(200));

        var returnedUser = okResult.Value as UserProfileDto;
        Assert.That(returnedUser, Is.Not.Null);
        Assert.That(returnedUser.Name, Is.EqualTo(updateRequest.Name));
        Assert.That(returnedUser.Role, Is.EqualTo(updateRequest.Role));
        Assert.That(returnedUser.IsActive, Is.EqualTo(updateRequest.IsActive));
    }

    [Test]
    public async Task UpdateUser_NonExistingUser_ReturnsNotFound()
    {
        // Arrange
        var userId = Guid.NewGuid();
        var updateRequest = new UpdateUserRequest("Updated Name");

        _mockUserService.Setup(x => x.UpdateAsync(userId, updateRequest))
            .ThrowsAsync(new AuthenticationException("User not found"));

        // Act
        var result = await _controller.UpdateUser(userId, updateRequest);

        // Assert
        var notFoundResult = result.Result as NotFoundObjectResult;
        Assert.That(notFoundResult, Is.Not.Null);
        Assert.That(notFoundResult.StatusCode, Is.EqualTo(404));
    }

    [Test]
    public async Task DeactivateUser_ExistingUser_ReturnsOk()
    {
        // Arrange
        var userId = Guid.NewGuid();

        // Act
        var result = await _controller.DeactivateUser(userId);

        // Assert
        var okResult = result.Result as OkObjectResult;
        Assert.That(okResult, Is.Not.Null);
        Assert.That(okResult.StatusCode, Is.EqualTo(200));

        _mockUserService.Verify(x => x.DeactivateAsync(userId), Times.Once);
    }

    [Test]
    public async Task DeactivateUser_NonExistingUser_ReturnsNotFound()
    {
        // Arrange
        var userId = Guid.NewGuid();

        _mockUserService.Setup(x => x.DeactivateAsync(userId))
            .ThrowsAsync(new AuthenticationException("User not found"));

        // Act
        var result = await _controller.DeactivateUser(userId);

        // Assert
        var notFoundResult = result.Result as NotFoundObjectResult;
        Assert.That(notFoundResult, Is.Not.Null);
        Assert.That(notFoundResult.StatusCode, Is.EqualTo(404));
    }

    [Test]
    public async Task GetUsersByRole_ValidRole_ReturnsUsers()
    {
        // Arrange
        var role = UserRole.Teacher;
        var teachers = new List<User>
        {
            new User
            {
                UserId = Guid.NewGuid(),
                Email = "test@example.com",
                FirstName = "Test",
                LastName = "User",
                Role = UserRole.Student,
                IsActive = true,
                Provider = AuthProvider.Google
            },
            new User
            {
                UserId = Guid.NewGuid(),
                Email = "test@example.com",
                FirstName = "Test",
                LastName = "User",
                Role = UserRole.Student,
                IsActive = true,
                Provider = AuthProvider.Google
            }
        };

        _mockUserService.Setup(x => x.GetByRoleAsync(It.IsAny<CancellationToken>())).ReturnsAsync(teachers);

        // Act
        var result = await _controller.GetUsersByRole(role);

        // Assert
        var okResult = result.Result as OkObjectResult;
        Assert.That(okResult, Is.Not.Null);
        Assert.That(okResult.StatusCode, Is.EqualTo(200));

        var returnedUsers = okResult.Value as IEnumerable<UserProfileDto>;
        Assert.That(returnedUsers, Is.Not.Null);
        Assert.That(returnedUsers.Count(), Is.EqualTo(2));
        Assert.That(returnedUsers.All(u => u.Role == UserRole.Teacher), Is.True);
    }

    [Test]
    public async Task GetUserSessions_ValidUserId_ReturnsSessions()
    {
        // Arrange
        var userId = Guid.NewGuid();
        var sessions = new List<UserSession>
        {
            new UserSession
            {
                SessionId = Guid.NewGuid(),
                UserId = Guid.NewGuid(),
                Token = "test-token",
                ExpiresAt = DateTime.UtcNow.AddHours(1),
                CreatedAt = DateTime.UtcNow
            },
            new UserSession
            {
                SessionId = Guid.NewGuid(),
                UserId = Guid.NewGuid(),
                Token = "test-token",
                ExpiresAt = DateTime.UtcNow.AddHours(1),
                CreatedAt = DateTime.UtcNow
            }
        };

        _mockSessionService.Setup(x => x.GetActiveSessionsAsync(It.IsAny<CancellationToken>())).ReturnsAsync(sessions);

        // Act
        var result = await _controller.GetUserSessions(userId);

        // Assert
        var okResult = result.Result as OkObjectResult;
        Assert.That(okResult, Is.Not.Null);
        Assert.That(okResult.StatusCode, Is.EqualTo(200));

        var returnedSessions = okResult.Value as IEnumerable<UserSession>;
        Assert.That(returnedSessions, Is.Not.Null);
        Assert.That(returnedSessions.Count(), Is.EqualTo(2));
    }

    [Test]
    public async Task InvalidateUserSessions_ValidUserId_ReturnsOk()
    {
        // Arrange
        var userId = Guid.NewGuid();

        // Act
        var result = await _controller.InvalidateUserSessions(userId);

        // Assert
        var okResult = result.Result as OkObjectResult;
        Assert.That(okResult, Is.Not.Null);
        Assert.That(okResult.StatusCode, Is.EqualTo(200));

        _mockSessionService.Verify(x => x.InvalidateAllUserSessionsAsync(userId), Times.Once);
    }

    [Test]
    public async Task GetUserAuditLogs_ValidUserId_ReturnsAuditLogs()
    {
        // Arrange
        var userId = Guid.NewGuid();
        var auditLogs = new List<AuditLog>
        {
            new AuditLog
            {
                AuditId = Guid.NewGuid(),
                Action = "TestAction",
                Resource = "TestResource",
                IpAddress = "127.0.0.1",
                UserAgent = "test-agent",
                Success = true,
                CreatedAt = DateTime.UtcNow
            },
            new AuditLog
            {
                AuditId = Guid.NewGuid(),
                Action = "TestAction",
                Resource = "TestResource",
                IpAddress = "127.0.0.1",
                UserAgent = "test-agent",
                Success = true,
                CreatedAt = DateTime.UtcNow
            }
        };

        _mockAuditService.Setup(x => x.GetUserAuditLogsAsync(It.IsAny<CancellationToken>())).ReturnsAsync(auditLogs);

        // Act
        var result = await _controller.GetUserAuditLogs(userId, 1, 50);

        // Assert
        var okResult = result.Result as OkObjectResult;
        Assert.That(okResult, Is.Not.Null);
        Assert.That(okResult.StatusCode, Is.EqualTo(200));

        var returnedLogs = okResult.Value as IEnumerable<AuditLog>;
        Assert.That(returnedLogs, Is.Not.Null);
        Assert.That(returnedLogs.Count(), Is.EqualTo(2));
        Assert.That(returnedLogs.All(log => log.UserId == userId), Is.True);
    }

    [Test]
    public async Task GetAllAuditLogs_ReturnsAllAuditLogs()
    {
        // Arrange
        var auditLogs = new List<AuditLog>
        {
            new AuditLog
            {
                AuditId = Guid.NewGuid(),
                Action = "TestAction",
                Resource = "TestResource",
                IpAddress = "127.0.0.1",
                UserAgent = "test-agent",
                Success = true,
                CreatedAt = DateTime.UtcNow
            },
            new AuditLog
            {
                AuditId = Guid.NewGuid(),
                Action = "TestAction",
                Resource = "TestResource",
                IpAddress = "127.0.0.1",
                UserAgent = "test-agent",
                Success = true,
                CreatedAt = DateTime.UtcNow
            },
            new AuditLog
            {
                AuditId = Guid.NewGuid(),
                Action = "TestAction",
                Resource = "TestResource",
                IpAddress = "127.0.0.1",
                UserAgent = "test-agent",
                Success = true,
                CreatedAt = DateTime.UtcNow
            }
        };

        _mockAuditService.Setup(x => x.GetAuditLogsAsync(It.IsAny<CancellationToken>())).ReturnsAsync(auditLogs);

        // Act
        var result = await _controller.GetAllAuditLogs(1, 50);

        // Assert
        var okResult = result.Result as OkObjectResult;
        Assert.That(okResult, Is.Not.Null);
        Assert.That(okResult.StatusCode, Is.EqualTo(200));

        var returnedLogs = okResult.Value as IEnumerable<AuditLog>;
        Assert.That(returnedLogs, Is.Not.Null);
        Assert.That(returnedLogs.Count(), Is.EqualTo(3));
    }
}