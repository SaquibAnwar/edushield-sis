using EduShield.Core.Entities;
using EduShield.Core.Enums;
using EduShield.Core.Dtos;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.Extensions.DependencyInjection;
using System.Net.Http.Json;
using System.Text.Json;

namespace EduShield.Api.Tests;

[TestFixture]
public class AuthenticationIntegrationTests
{
    private WebApplicationFactory<Program> _factory;
    private HttpClient _client;

    [SetUp]
    public void Setup()
    {
        _factory = new CustomWebAppFactory();
        _client = _factory.CreateClient();
    }

    [TearDown]
    public void TearDown()
    {
        _client?.Dispose();
        _factory?.Dispose();
    }

    [Test]
    public async Task Login_ValidGoogleToken_ReturnsSuccessfulAuth()
    {
        // Arrange
        var loginRequest = new LoginRequest
        {
            Provider = AuthProvider.Google,
            IdToken = "mock-valid-google-token"
        };

        // Act
        var response = await _client.PostAsJsonAsync("/api/auth/login", loginRequest);

        // Assert
        Assert.That(response.IsSuccessStatusCode, Is.True);
        
        var content = await response.Content.ReadAsStringAsync();
        var authResult = JsonSerializer.Deserialize<AuthResult>(content, new JsonSerializerOptions 
        { 
            PropertyNameCaseInsensitive = true 
        });
        
        Assert.That(authResult, Is.Not.Null);
        Assert.That(authResult.IsSuccess, Is.True);
        Assert.That(authResult.SessionToken, Is.Not.Null.And.Not.Empty);
        Assert.That(authResult.User, Is.Not.Null);
    }

    [Test]
    public async Task Login_InvalidToken_ReturnsBadRequest()
    {
        // Arrange
        var loginRequest = new LoginRequest
        {
            Provider = AuthProvider.Google,
            IdToken = "invalid-token"
        };

        // Act
        var response = await _client.PostAsJsonAsync("/api/auth/login", loginRequest);

        // Assert
        Assert.That(response.StatusCode, Is.EqualTo(System.Net.HttpStatusCode.BadRequest));
    }

    [Test]
    public async Task ValidateSession_ValidToken_ReturnsUserInfo()
    {
        // Arrange - First login to get a valid session token
        var loginRequest = new LoginRequest
        {
            Provider = AuthProvider.Google,
            IdToken = "mock-valid-google-token"
        };

        var loginResponse = await _client.PostAsJsonAsync("/api/auth/login", loginRequest);
        var loginContent = await loginResponse.Content.ReadAsStringAsync();
        var authResult = JsonSerializer.Deserialize<AuthResult>(loginContent, new JsonSerializerOptions 
        { 
            PropertyNameCaseInsensitive = true 
        });

        // Act
        var response = await _client.GetAsync($"/api/auth/validate?token={authResult.SessionToken}");

        // Assert
        Assert.That(response.IsSuccessStatusCode, Is.True);
        
        var content = await response.Content.ReadAsStringAsync();
        Assert.That(content, Is.Not.Null.And.Not.Empty);
    }

    [Test]
    public async Task ValidateSession_InvalidToken_ReturnsUnauthorized()
    {
        // Arrange
        var invalidToken = "invalid-session-token";

        // Act
        var response = await _client.GetAsync($"/api/auth/validate?token={invalidToken}");

        // Assert
        Assert.That(response.StatusCode, Is.EqualTo(System.Net.HttpStatusCode.Unauthorized));
    }

    [Test]
    public async Task Logout_ValidSession_InvalidatesSession()
    {
        // Arrange - First login to get a valid session token
        var loginRequest = new LoginRequest
        {
            Provider = AuthProvider.Google,
            IdToken = "mock-valid-google-token"
        };

        var loginResponse = await _client.PostAsJsonAsync("/api/auth/login", loginRequest);
        var loginContent = await loginResponse.Content.ReadAsStringAsync();
        var authResult = JsonSerializer.Deserialize<AuthResult>(loginContent, new JsonSerializerOptions 
        { 
            PropertyNameCaseInsensitive = true 
        });

        // Add authorization header
        _client.DefaultRequestHeaders.Authorization = 
            new System.Net.Http.Headers.AuthenticationHeaderValue("Bearer", authResult.SessionToken);

        // Act
        var logoutResponse = await _client.PostAsync("/api/auth/logout", null);

        // Assert
        Assert.That(logoutResponse.IsSuccessStatusCode, Is.True);

        // Verify session is invalidated
        var validateResponse = await _client.GetAsync($"/api/auth/validate?token={authResult.SessionToken}");
        Assert.That(validateResponse.StatusCode, Is.EqualTo(System.Net.HttpStatusCode.Unauthorized));
    }

    [Test]
    public async Task GetProfile_AuthenticatedUser_ReturnsProfile()
    {
        // Arrange - First login to get a valid session token
        var loginRequest = new LoginRequest
        {
            Provider = AuthProvider.Google,
            IdToken = "mock-valid-google-token"
        };

        var loginResponse = await _client.PostAsJsonAsync("/api/auth/login", loginRequest);
        var loginContent = await loginResponse.Content.ReadAsStringAsync();
        var authResult = JsonSerializer.Deserialize<AuthResult>(loginContent, new JsonSerializerOptions 
        { 
            PropertyNameCaseInsensitive = true 
        });

        // Add authorization header
        _client.DefaultRequestHeaders.Authorization = 
            new System.Net.Http.Headers.AuthenticationHeaderValue("Bearer", authResult.SessionToken);

        // Act
        var response = await _client.GetAsync("/api/auth/profile");

        // Assert
        Assert.That(response.IsSuccessStatusCode, Is.True);
        
        var content = await response.Content.ReadAsStringAsync();
        var profile = JsonSerializer.Deserialize<UserProfileDto>(content, new JsonSerializerOptions 
        { 
            PropertyNameCaseInsensitive = true 
        });
        
        Assert.That(profile, Is.Not.Null);
        Assert.That(profile.Email, Is.Not.Null.And.Not.Empty);
        Assert.That(profile.Name, Is.Not.Null.And.Not.Empty);
    }

    [Test]
    public async Task GetProfile_UnauthenticatedUser_ReturnsUnauthorized()
    {
        // Act
        var response = await _client.GetAsync("/api/auth/profile");

        // Assert
        Assert.That(response.StatusCode, Is.EqualTo(System.Net.HttpStatusCode.Unauthorized));
    }

    [Test]
    public async Task UserManagement_SystemAdminOperations_WorksCorrectly()
    {
        // Arrange - Login as system admin
        var loginRequest = new LoginRequest
        {
            Provider = AuthProvider.Google,
            IdToken = "mock-valid-admin-token"
        };

        var loginResponse = await _client.PostAsJsonAsync("/api/auth/login", loginRequest);
        var loginContent = await loginResponse.Content.ReadAsStringAsync();
        var authResult = JsonSerializer.Deserialize<AuthResult>(loginContent, new JsonSerializerOptions 
        { 
            PropertyNameCaseInsensitive = true 
        });

        _client.DefaultRequestHeaders.Authorization = 
            new System.Net.Http.Headers.AuthenticationHeaderValue("Bearer", authResult.SessionToken);

        // Act & Assert - Get all users
        var getAllResponse = await _client.GetAsync("/api/users");
        Assert.That(getAllResponse.IsSuccessStatusCode, Is.True);

        // Act & Assert - Create new user
        var createUserRequest = new CreateUserRequest { Email = "test@example.com", FirstName = "Test", LastName = "User", Role = UserRole.Student };

        var createResponse = await _client.PostAsJsonAsync("/api/users", createUserRequest);
        Assert.That(createResponse.IsSuccessStatusCode, Is.True);

        var createContent = await createResponse.Content.ReadAsStringAsync();
        var createdUser = JsonSerializer.Deserialize<UserProfileDto>(createContent, new JsonSerializerOptions 
        { 
            PropertyNameCaseInsensitive = true 
        });

        Assert.That(createdUser, Is.Not.Null);
        Assert.That(createdUser.Email, Is.EqualTo("newuser@example.com"));

        // Act & Assert - Update user
        var updateRequest = new UpdateUserRequest("Updated Name");

        var updateResponse = await _client.PutAsJsonAsync($"/api/users/{createdUser.Id}", updateRequest);
        Assert.That(updateResponse.IsSuccessStatusCode, Is.True);

        // Act & Assert - Get user by ID
        var getUserResponse = await _client.GetAsync($"/api/users/{createdUser.Id}");
        Assert.That(getUserResponse.IsSuccessStatusCode, Is.True);

        var getUserContent = await getUserResponse.Content.ReadAsStringAsync();
        var retrievedUser = JsonSerializer.Deserialize<UserProfileDto>(getUserContent, new JsonSerializerOptions 
        { 
            PropertyNameCaseInsensitive = true 
        });

        Assert.That(retrievedUser, Is.Not.Null);
        Assert.That(retrievedUser.Name, Is.EqualTo("Updated Name"));
        Assert.That(retrievedUser.Role, Is.EqualTo(UserRole.SchoolAdmin));
    }

    [Test]
    public async Task Authorization_StudentAccessingOwnData_Succeeds()
    {
        // Arrange - Login as student
        var loginRequest = new LoginRequest
        {
            Provider = AuthProvider.Google,
            IdToken = "mock-valid-student-token"
        };

        var loginResponse = await _client.PostAsJsonAsync("/api/auth/login", loginRequest);
        var loginContent = await loginResponse.Content.ReadAsStringAsync();
        var authResult = JsonSerializer.Deserialize<AuthResult>(loginContent, new JsonSerializerOptions 
        { 
            PropertyNameCaseInsensitive = true 
        });

        _client.DefaultRequestHeaders.Authorization = 
            new System.Net.Http.Headers.AuthenticationHeaderValue("Bearer", authResult.SessionToken);

        // Act - Try to access own profile
        var response = await _client.GetAsync("/api/auth/profile");

        // Assert
        Assert.That(response.IsSuccessStatusCode, Is.True);
    }

    [Test]
    public async Task Authorization_StudentAccessingAdminEndpoint_Fails()
    {
        // Arrange - Login as student
        var loginRequest = new LoginRequest
        {
            Provider = AuthProvider.Google,
            IdToken = "mock-valid-student-token"
        };

        var loginResponse = await _client.PostAsJsonAsync("/api/auth/login", loginRequest);
        var loginContent = await loginResponse.Content.ReadAsStringAsync();
        var authResult = JsonSerializer.Deserialize<AuthResult>(loginContent, new JsonSerializerOptions 
        { 
            PropertyNameCaseInsensitive = true 
        });

        _client.DefaultRequestHeaders.Authorization = 
            new System.Net.Http.Headers.AuthenticationHeaderValue("Bearer", authResult.SessionToken);

        // Act - Try to access admin endpoint
        var response = await _client.GetAsync("/api/users");

        // Assert
        Assert.That(response.StatusCode, Is.EqualTo(System.Net.HttpStatusCode.Forbidden));
    }

    [Test]
    public async Task SessionManagement_MultipleLogins_CreatesMultipleSessions()
    {
        // Arrange
        var loginRequest = new LoginRequest
        {
            Provider = AuthProvider.Google,
            IdToken = "mock-valid-google-token"
        };

        // Act - Login multiple times
        var response1 = await _client.PostAsJsonAsync("/api/auth/login", loginRequest);
        var response2 = await _client.PostAsJsonAsync("/api/auth/login", loginRequest);

        // Assert
        Assert.That(response1.IsSuccessStatusCode, Is.True);
        Assert.That(response2.IsSuccessStatusCode, Is.True);

        var content1 = await response1.Content.ReadAsStringAsync();
        var content2 = await response2.Content.ReadAsStringAsync();

        var authResult1 = JsonSerializer.Deserialize<AuthResult>(content1, new JsonSerializerOptions 
        { 
            PropertyNameCaseInsensitive = true 
        });
        var authResult2 = JsonSerializer.Deserialize<AuthResult>(content2, new JsonSerializerOptions 
        { 
            PropertyNameCaseInsensitive = true 
        });

        // Verify different session tokens
        Assert.That(authResult1.SessionToken, Is.Not.EqualTo(authResult2.SessionToken));

        // Verify both sessions are valid
        var validate1 = await _client.GetAsync($"/api/auth/validate?token={authResult1.SessionToken}");
        var validate2 = await _client.GetAsync($"/api/auth/validate?token={authResult2.SessionToken}");

        Assert.That(validate1.IsSuccessStatusCode, Is.True);
        Assert.That(validate2.IsSuccessStatusCode, Is.True);
    }

    [Test]
    public async Task AuditLogging_AuthenticationEvents_AreLogged()
    {
        // Arrange - Login as admin to access audit logs
        var adminLoginRequest = new LoginRequest
        {
            Provider = AuthProvider.Google,
            IdToken = "mock-valid-admin-token"
        };

        var adminLoginResponse = await _client.PostAsJsonAsync("/api/auth/login", adminLoginRequest);
        var adminLoginContent = await adminLoginResponse.Content.ReadAsStringAsync();
        var adminAuthResult = JsonSerializer.Deserialize<AuthResult>(adminLoginContent, new JsonSerializerOptions 
        { 
            PropertyNameCaseInsensitive = true 
        });

        _client.DefaultRequestHeaders.Authorization = 
            new System.Net.Http.Headers.AuthenticationHeaderValue("Bearer", adminAuthResult.SessionToken);

        // Act - Perform some authentication actions
        var userLoginRequest = new LoginRequest
        {
            Provider = AuthProvider.Google,
            IdToken = "mock-valid-google-token"
        };

        await _client.PostAsJsonAsync("/api/auth/login", userLoginRequest);

        // Wait a bit for audit logs to be written
        await Task.Delay(100);

        // Assert - Check audit logs
        var auditResponse = await _client.GetAsync("/api/users/audit-logs?page=1&pageSize=10");
        Assert.That(auditResponse.IsSuccessStatusCode, Is.True);

        var auditContent = await auditResponse.Content.ReadAsStringAsync();
        var auditLogs = JsonSerializer.Deserialize<IEnumerable<AuditLog>>(auditContent, new JsonSerializerOptions 
        { 
            PropertyNameCaseInsensitive = true 
        });

        Assert.That(auditLogs, Is.Not.Null);
        Assert.That(auditLogs.Any(log => log.Action.Contains("Login")), Is.True);
    }

}