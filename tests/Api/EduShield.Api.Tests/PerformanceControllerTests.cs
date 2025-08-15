using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;
using Moq;
using EduShield.Core.Dtos;
using EduShield.Core.Interfaces;
using EduShield.Api.Controllers;

namespace EduShield.Api.Tests;

[TestFixture]
public class PerformanceControllerTests
{
    private readonly Mock<IPerformanceService> _mockPerformanceService;
    private readonly Mock<ILogger<PerformanceController>> _mockLogger;
    private readonly PerformanceController _controller;

    public PerformanceControllerTests()
    {
        _mockPerformanceService = new Mock<IPerformanceService>();
        _mockLogger = new Mock<ILogger<PerformanceController>>();
        _controller = new PerformanceController(_mockPerformanceService.Object, _mockLogger.Object);
    }

    [Test]
    public async Task Create_ValidRequest_ReturnsCreatedResult()
    {
        // Arrange
        var request = new CreatePerformanceReq
        {
            StudentId = Guid.NewGuid(),
            FacultyId = Guid.NewGuid(),
            Subject = "Mathematics",
            Marks = 85m,
            MaxMarks = 100m,
            ExamDate = DateTime.UtcNow.AddDays(-1)
        };
        var performanceId = Guid.NewGuid();
        _mockPerformanceService.Setup(x => x.CreateAsync(request, It.IsAny<CancellationToken>()))
            .ReturnsAsync(performanceId);

        // Act
        var result = await _controller.Create(request, CancellationToken.None);

        // Assert
        Assert.That(result.Result, Is.InstanceOf<CreatedAtActionResult>());
        var createdResult = (CreatedAtActionResult)result.Result;
        Assert.That(createdResult.Value, Is.EqualTo(performanceId));
        Assert.That(createdResult.ActionName, Is.EqualTo(nameof(PerformanceController.Get)));
    }

    [Test]
    public async Task Create_ServiceThrowsArgumentException_ReturnsBadRequest()
    {
        // Arrange
        var request = new CreatePerformanceReq
        {
            StudentId = Guid.NewGuid(),
            FacultyId = Guid.NewGuid(),
            Subject = "Mathematics",
            Marks = 85m,
            MaxMarks = 100m,
            ExamDate = DateTime.UtcNow.AddDays(-1)
        };
        _mockPerformanceService.Setup(x => x.CreateAsync(request, It.IsAny<CancellationToken>()))
            .ThrowsAsync(new ArgumentException("Student not found"));

        // Act
        var result = await _controller.Create(request, CancellationToken.None);

        // Assert
        Assert.That(result.Result, Is.InstanceOf<BadRequestObjectResult>());
    }

    [Test]
    public async Task Create_ServiceThrowsException_ReturnsInternalServerError()
    {
        // Arrange
        var request = new CreatePerformanceReq
        {
            StudentId = Guid.NewGuid(),
            FacultyId = Guid.NewGuid(),
            Subject = "Mathematics",
            Marks = 85m,
            MaxMarks = 100m,
            ExamDate = DateTime.UtcNow.AddDays(-1)
        };
        _mockPerformanceService.Setup(x => x.CreateAsync(request, It.IsAny<CancellationToken>()))
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
        var performanceId = Guid.NewGuid();
        var performanceDto = new PerformanceDto
        {
            PerformanceId = performanceId,
            StudentId = Guid.NewGuid(),
            FacultyId = Guid.NewGuid(),
            Subject = "Mathematics",
            Marks = 85m,
            MaxMarks = 100m,
            ExamDate = DateTime.UtcNow.AddDays(-1),
            Percentage = 85m
        };
        _mockPerformanceService.Setup(x => x.GetAsync(performanceId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(performanceDto);

        // Act
        var result = await _controller.Get(performanceId, CancellationToken.None);

        // Assert
        Assert.That(result.Result, Is.InstanceOf<OkObjectResult>());
        var okResult = (OkObjectResult)result.Result;
        var returnedPerformance = (PerformanceDto)okResult.Value;
        Assert.That(returnedPerformance.PerformanceId, Is.EqualTo(performanceId));
    }

    [Test]
    public async Task Get_InvalidId_ReturnsNotFound()
    {
        // Arrange
        var performanceId = Guid.NewGuid();
        _mockPerformanceService.Setup(x => x.GetAsync(performanceId, It.IsAny<CancellationToken>()))
            .ReturnsAsync((PerformanceDto?)null);

        // Act
        var result = await _controller.Get(performanceId, CancellationToken.None);

        // Assert
        Assert.That(result.Result, Is.InstanceOf<NotFoundResult>());
    }

    [Test]
    public async Task GetAll_ReturnsOkResult()
    {
        // Arrange
        var performances = new List<PerformanceDto>
        {
            new() { PerformanceId = Guid.NewGuid(), Subject = "Math", Marks = 85m, MaxMarks = 100m, Percentage = 85m },
            new() { PerformanceId = Guid.NewGuid(), Subject = "Science", Marks = 92m, MaxMarks = 100m, Percentage = 92m }
        };
        _mockPerformanceService.Setup(x => x.GetAllAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync(performances);

        // Act
        var result = await _controller.GetAll(CancellationToken.None);

        // Assert
        Assert.That(result.Result, Is.InstanceOf<OkObjectResult>());
        var okResult = (OkObjectResult)result.Result;
        var returnedPerformances = (IEnumerable<PerformanceDto>)okResult.Value;
        Assert.That(returnedPerformances.Count(), Is.EqualTo(2));
    }

    [Test]
    public async Task GetByStudent_ReturnsOkResult()
    {
        // Arrange
        var studentId = Guid.NewGuid();
        var performances = new List<PerformanceDto>
        {
            new() { PerformanceId = Guid.NewGuid(), StudentId = studentId, Subject = "Math", Marks = 85m, MaxMarks = 100m, Percentage = 85m }
        };
        _mockPerformanceService.Setup(x => x.GetByStudentIdAsync(studentId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(performances);

        // Act
        var result = await _controller.GetByStudent(studentId, CancellationToken.None);

        // Assert
        Assert.That(result.Result, Is.InstanceOf<OkObjectResult>());
        var okResult = (OkObjectResult)result.Result;
        var returnedPerformances = (IEnumerable<PerformanceDto>)okResult.Value;
        Assert.That(returnedPerformances.Count(), Is.EqualTo(1));
        Assert.That(returnedPerformances.First().StudentId, Is.EqualTo(studentId));
    }

    [Test]
    public async Task GetByFaculty_ReturnsOkResult()
    {
        // Arrange
        var facultyId = Guid.NewGuid();
        var performances = new List<PerformanceDto>
        {
            new() { PerformanceId = Guid.NewGuid(), FacultyId = facultyId, Subject = "Math", Marks = 85m, MaxMarks = 100m, Percentage = 85m }
        };
        _mockPerformanceService.Setup(x => x.GetByFacultyIdAsync(facultyId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(performances);

        // Act
        var result = await _controller.GetByFaculty(facultyId, CancellationToken.None);

        // Assert
        Assert.That(result.Result, Is.InstanceOf<OkObjectResult>());
        var okResult = (OkObjectResult)result.Result;
        var returnedPerformances = (IEnumerable<PerformanceDto>)okResult.Value;
        Assert.That(returnedPerformances.Count(), Is.EqualTo(1));
        Assert.That(returnedPerformances.First().FacultyId, Is.EqualTo(facultyId));
    }

    [Test]
    public async Task Update_ValidId_ReturnsNoContent()
    {
        // Arrange
        var performanceId = Guid.NewGuid();
        var request = new CreatePerformanceReq
        {
            StudentId = Guid.NewGuid(),
            FacultyId = Guid.NewGuid(),
            Subject = "Updated Mathematics",
            Marks = 90m,
            MaxMarks = 100m,
            ExamDate = DateTime.UtcNow.AddDays(-1)
        };
        _mockPerformanceService.Setup(x => x.UpdateAsync(performanceId, request, It.IsAny<CancellationToken>()))
            .ReturnsAsync(true);

        // Act
        var result = await _controller.Update(performanceId, request, CancellationToken.None);

        // Assert
        Assert.That(result, Is.InstanceOf<NoContentResult>());
    }

    [Test]
    public async Task Update_InvalidId_ReturnsNotFound()
    {
        // Arrange
        var performanceId = Guid.NewGuid();
        var request = new CreatePerformanceReq
        {
            StudentId = Guid.NewGuid(),
            FacultyId = Guid.NewGuid(),
            Subject = "Mathematics",
            Marks = 85m,
            MaxMarks = 100m,
            ExamDate = DateTime.UtcNow.AddDays(-1)
        };
        _mockPerformanceService.Setup(x => x.UpdateAsync(performanceId, request, It.IsAny<CancellationToken>()))
            .ReturnsAsync(false);

        // Act
        var result = await _controller.Update(performanceId, request, CancellationToken.None);

        // Assert
        Assert.That(result, Is.InstanceOf<NotFoundResult>());
    }

    [Test]
    public async Task Delete_ValidId_ReturnsNoContent()
    {
        // Arrange
        var performanceId = Guid.NewGuid();
        _mockPerformanceService.Setup(x => x.DeleteAsync(performanceId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(true);

        // Act
        var result = await _controller.Delete(performanceId, CancellationToken.None);

        // Assert
        Assert.That(result, Is.InstanceOf<NoContentResult>());
    }

    [Test]
    public async Task Delete_InvalidId_ReturnsNotFound()
    {
        // Arrange
        var performanceId = Guid.NewGuid();
        _mockPerformanceService.Setup(x => x.DeleteAsync(performanceId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(false);

        // Act
        var result = await _controller.Delete(performanceId, CancellationToken.None);

        // Assert
        Assert.That(result, Is.InstanceOf<NotFoundResult>());
    }
}