using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;
using Moq;
using EduShield.Core.Dtos;
using EduShield.Core.Interfaces;
using EduShield.Api.Controllers;
using EduShield.Core.Enums;

namespace EduShield.Api.Tests;

[TestFixture]
public class FacultyControllerTests
{
    private readonly Mock<IFacultyService> _mockFacultyService;
    private readonly Mock<ILogger<FacultyController>> _mockLogger;
    private readonly FacultyController _controller;

    public FacultyControllerTests()
    {
        _mockFacultyService = new Mock<IFacultyService>();
        _mockLogger = new Mock<ILogger<FacultyController>>();
        _controller = new FacultyController(_mockFacultyService.Object, _mockLogger.Object);
    }

    [Test]
    public async Task Create_ValidRequest_ReturnsCreatedResult()
    {
        // Arrange
        var request = new CreateFacultyReq
        {
            Name = "John Doe",
            Department = "Computer Science",
            Subject = "Programming",
            Gender = Gender.M
        };
        var facultyId = Guid.NewGuid();
        _mockFacultyService.Setup(x => x.CreateAsync(request, It.IsAny<CancellationToken>()))
            .ReturnsAsync(facultyId);

        // Act
        var result = await _controller.Create(request, CancellationToken.None);

        // Assert
        Assert.That(result.Result, Is.InstanceOf<CreatedAtActionResult>());
        var createdResult = (CreatedAtActionResult)result.Result;
        Assert.That(createdResult.Value, Is.EqualTo(facultyId));
        Assert.That(createdResult.ActionName, Is.EqualTo(nameof(FacultyController.Get)));
    }

    [Test]
    public async Task Create_ServiceThrowsException_ReturnsInternalServerError()
    {
        // Arrange
        var request = new CreateFacultyReq
        {
            Name = "John Doe",
            Department = "Computer Science",
            Subject = "Programming",
            Gender = Gender.M
        };
        _mockFacultyService.Setup(x => x.CreateAsync(request, It.IsAny<CancellationToken>()))
            .ThrowsAsync(new Exception("Service error"));

        // Act
        var result = await _controller.Create(request, CancellationToken.None);

        // Assert
        Assert.That(result.Result, Is.InstanceOf<ObjectResult>());
        var statusResult = (ObjectResult)result.Result;
        Assert.That(statusResult.StatusCode, Is.EqualTo(500));
    }

    [Test]
    public async Task Get_ValidId_ReturnsOkResult()
    {
        // Arrange
        var facultyId = Guid.NewGuid();
        var facultyDto = new FacultyDto
        {
            FacultyId = facultyId,
            Name = "John Doe",
            Department = "Computer Science",
            Subject = "Programming",
            Gender = Gender.M
        };
        _mockFacultyService.Setup(x => x.GetAsync(facultyId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(facultyDto);

        // Act
        var result = await _controller.Get(facultyId, CancellationToken.None);

        // Assert
        Assert.That(result.Result, Is.InstanceOf<OkObjectResult>());
        var okResult = (OkObjectResult)result.Result;
        var returnedFaculty = (FacultyDto)okResult.Value;
        Assert.That(returnedFaculty.FacultyId, Is.EqualTo(facultyId));
    }

    [Test]
    public async Task Get_InvalidId_ReturnsNotFound()
    {
        // Arrange
        var facultyId = Guid.NewGuid();
        _mockFacultyService.Setup(x => x.GetAsync(facultyId, It.IsAny<CancellationToken>()))
            .ReturnsAsync((FacultyDto?)null);

        // Act
        var result = await _controller.Get(facultyId, CancellationToken.None);

        // Assert
        Assert.That(result.Result, Is.InstanceOf<NotFoundResult>());
    }

    [Test]
    public async Task GetAll_ReturnsOkResult()
    {
        // Arrange
        var faculties = new List<FacultyDto>
        {
            new() { FacultyId = Guid.NewGuid(), Name = "John Doe", Department = "CS", Subject = "Programming", Gender = Gender.M },
            new() { FacultyId = Guid.NewGuid(), Name = "Jane Smith", Department = "Math", Subject = "Calculus", Gender = Gender.F }
        };
        _mockFacultyService.Setup(x => x.GetAllAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync(faculties);

        // Act
        var result = await _controller.GetAll(CancellationToken.None);

        // Assert
        Assert.That(result.Result, Is.InstanceOf<OkObjectResult>());
        var okResult = (OkObjectResult)result.Result;
        var returnedFaculties = (IEnumerable<FacultyDto>)okResult.Value;
        Assert.That(returnedFaculties.Count(), Is.EqualTo(2));
    }

    [Test]
    public async Task Update_ValidId_ReturnsNoContent()
    {
        // Arrange
        var facultyId = Guid.NewGuid();
        var request = new CreateFacultyReq
        {
            Name = "John Doe Updated",
            Department = "Computer Science",
            Subject = "Programming",
            Gender = Gender.M
        };
        _mockFacultyService.Setup(x => x.UpdateAsync(facultyId, request, It.IsAny<CancellationToken>()))
            .ReturnsAsync(true);

        // Act
        var result = await _controller.Update(facultyId, request, CancellationToken.None);

        // Assert
        Assert.That(result, Is.InstanceOf<NoContentResult>());
    }

    [Test]
    public async Task Update_InvalidId_ReturnsNotFound()
    {
        // Arrange
        var facultyId = Guid.NewGuid();
        var request = new CreateFacultyReq
        {
            Name = "John Doe Updated",
            Department = "Computer Science",
            Subject = "Programming",
            Gender = Gender.M
        };
        _mockFacultyService.Setup(x => x.UpdateAsync(facultyId, request, It.IsAny<CancellationToken>()))
            .ReturnsAsync(false);

        // Act
        var result = await _controller.Update(facultyId, request, CancellationToken.None);

        // Assert
        Assert.That(result, Is.InstanceOf<NotFoundResult>());
    }

    [Test]
    public async Task Delete_ValidId_ReturnsNoContent()
    {
        // Arrange
        var facultyId = Guid.NewGuid();
        _mockFacultyService.Setup(x => x.DeleteAsync(facultyId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(true);

        // Act
        var result = await _controller.Delete(facultyId, CancellationToken.None);

        // Assert
        Assert.That(result, Is.InstanceOf<NoContentResult>());
    }

    [Test]
    public async Task Delete_InvalidId_ReturnsNotFound()
    {
        // Arrange
        var facultyId = Guid.NewGuid();
        _mockFacultyService.Setup(x => x.DeleteAsync(facultyId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(false);

        // Act
        var result = await _controller.Delete(facultyId, CancellationToken.None);

        // Assert
        Assert.That(result, Is.InstanceOf<NotFoundResult>());
    }
}
