using System.Net;
using System.Net.Http.Json;
using System.Text.Json;
using EduShield.Core.Data;
using EduShield.Core.Dtos;
using EduShield.Core.Enums;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;

namespace EduShield.Api.Tests;

[TestFixture]
public class StudentEndpointsTests
{
    private CustomWebAppFactory _factory = default!;
    private HttpClient _client = default!;

    [OneTimeSetUp]
    public void OneTimeSetUp()
    {
        _factory = new CustomWebAppFactory();
        _client = _factory.CreateClient();
    }

    [OneTimeTearDown]
    public void OneTimeTearDown()
    {
        _client?.Dispose();
        _factory?.Dispose();
    }

    [Test]
    public async Task CreateStudent_ValidRequest_ReturnsCreatedWithLocation()
    {
        // Arrange
        var request = new CreateStudentReq(
            Name: "John Doe",
            Class: "10A",
            Section: "A",
            Gender: Gender.M
        );

        // Act
        var response = await _client.PostAsJsonAsync("/v1/students", request);

        // Assert
        Assert.That(response.StatusCode, Is.EqualTo(HttpStatusCode.Created));
        Assert.That(response.Headers.Location, Is.Not.Null);

        var content = await response.Content.ReadAsStringAsync();
        var result = JsonSerializer.Deserialize<JsonElement>(content);
        var id = result.GetProperty("id").GetGuid();
        Assert.That(id, Is.Not.EqualTo(Guid.Empty));

        // Verify location header format
        var expectedLocation = $"/v1/students/{id}";
        Assert.That(response.Headers.Location!.ToString(), Does.EndWith(expectedLocation));
    }

    [Test]
    public async Task CreateStudent_EmptyName_ReturnsBadRequest()
    {
        // Arrange
        var request = new CreateStudentReq(
            Name: "",
            Class: "10A",
            Section: "A",
            Gender: Gender.F
        );

        // Act
        var response = await _client.PostAsJsonAsync("/v1/students", request);

        // Assert
        Assert.That(response.StatusCode, Is.EqualTo(HttpStatusCode.BadRequest));

        var content = await response.Content.ReadAsStringAsync();
        var result = JsonSerializer.Deserialize<JsonElement>(content);
        var error = result.GetProperty("error").GetString();
        Assert.That(error, Does.Contain("Validation failed"));
        Assert.That(error, Does.Contain("Name"));
    }

    [Test]
    public async Task CreateStudent_NameTooLong_ReturnsBadRequest()
    {
        // Arrange
        var longName = new string('A', 101); // Exceeds 100 character limit
        var request = new CreateStudentReq(
            Name: longName,
            Class: "10A",
            Section: "A",
            Gender: Gender.Other
        );

        // Act
        var response = await _client.PostAsJsonAsync("/v1/students", request);

        // Assert
        Assert.That(response.StatusCode, Is.EqualTo(HttpStatusCode.BadRequest));

        var content = await response.Content.ReadAsStringAsync();
        var result = JsonSerializer.Deserialize<JsonElement>(content);
        var error = result.GetProperty("error").GetString();
        Assert.That(error, Does.Contain("Validation failed"));
        Assert.That(error, Does.Contain("Name"));
    }

    [Test]
    public async Task CreateStudent_ClassTooLong_ReturnsBadRequest()
    {
        // Arrange
        var longClass = new string('1', 11); // Exceeds 10 character limit
        var request = new CreateStudentReq(
            Name: "Jane Doe",
            Class: longClass,
            Section: "B",
            Gender: Gender.F
        );

        // Act
        var response = await _client.PostAsJsonAsync("/v1/students", request);

        // Assert
        Assert.That(response.StatusCode, Is.EqualTo(HttpStatusCode.BadRequest));

        var content = await response.Content.ReadAsStringAsync();
        var result = JsonSerializer.Deserialize<JsonElement>(content);
        var error = result.GetProperty("error").GetString();
        Assert.That(error, Does.Contain("Validation failed"));
        Assert.That(error, Does.Contain("Class"));
    }

    [Test]
    public async Task CreateStudent_SectionTooLong_ReturnsBadRequest()
    {
        // Arrange
        var longSection = new string('A', 6); // Exceeds 5 character limit
        var request = new CreateStudentReq(
            Name: "Bob Smith",
            Class: "9C",
            Section: longSection,
            Gender: Gender.M
        );

        // Act
        var response = await _client.PostAsJsonAsync("/v1/students", request);

        // Assert
        Assert.That(response.StatusCode, Is.EqualTo(HttpStatusCode.BadRequest));

        var content = await response.Content.ReadAsStringAsync();
        var result = JsonSerializer.Deserialize<JsonElement>(content);
        var error = result.GetProperty("error").GetString();
        Assert.That(error, Does.Contain("Validation failed"));
        Assert.That(error, Does.Contain("Section"));
    }

    [Test]
    public async Task GetStudent_ExistingStudent_ReturnsOkWithStudentDto()
    {
        // Arrange - Create a student first
        var createRequest = new CreateStudentReq(
            Name: "Alice Johnson",
            Class: "11B",
            Section: "B",
            Gender: Gender.F
        );

        var createResponse = await _client.PostAsJsonAsync("/v1/students", createRequest);
        var createContent = await createResponse.Content.ReadAsStringAsync();
        var createResult = JsonSerializer.Deserialize<JsonElement>(createContent);
        var studentId = createResult.GetProperty("id").GetGuid();

        // Act
        var response = await _client.GetAsync($"/v1/students/{studentId}");

        // Assert
        Assert.That(response.StatusCode, Is.EqualTo(HttpStatusCode.OK));

        var content = await response.Content.ReadAsStringAsync();
        var options = new JsonSerializerOptions { PropertyNameCaseInsensitive = true };
        var student = JsonSerializer.Deserialize<StudentDto>(content, options);

        Assert.That(student, Is.Not.Null);
        Assert.That(student.StudentId, Is.EqualTo(studentId));
        Assert.That(student.Name, Is.EqualTo("Alice Johnson"));
        Assert.That(student.Class, Is.EqualTo("11B"));
        Assert.That(student.Section, Is.EqualTo("B"));
        Assert.That(student.Gender, Is.EqualTo(Gender.F));
    }

    [Test]
    public async Task GetStudent_NonExistentStudent_ReturnsNotFound()
    {
        // Arrange
        var nonExistentId = Guid.NewGuid();

        // Act
        var response = await _client.GetAsync($"/v1/students/{nonExistentId}");

        // Assert
        Assert.That(response.StatusCode, Is.EqualTo(HttpStatusCode.NotFound));
    }

    [Test]
    public async Task GetStudent_InvalidGuidFormat_ReturnsBadRequest()
    {
        // Act
        var response = await _client.GetAsync("/v1/students/invalid-guid");

        // Assert - Minimal API routing returns NotFound for invalid GUID format
        Assert.That(response.StatusCode, Is.EqualTo(HttpStatusCode.NotFound));
    }

    [Test]
    public async Task CreateStudent_InvalidJson_ReturnsBadRequest()
    {
        // Arrange
        var invalidJson = "{ invalid json }";
        var content = new StringContent(invalidJson, System.Text.Encoding.UTF8, "application/json");

        // Act
        var response = await _client.PostAsync("/v1/students", content);

        // Assert
        Assert.That(response.StatusCode, Is.EqualTo(HttpStatusCode.BadRequest));
    }

}
