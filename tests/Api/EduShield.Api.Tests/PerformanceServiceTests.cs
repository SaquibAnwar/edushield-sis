using AutoMapper;
using EduShield.Api.Services;
using EduShield.Core.Dtos;
using EduShield.Core.Entities;
using EduShield.Core.Enums;
using EduShield.Core.Interfaces;
using EduShield.Core.Mapping;
using Microsoft.Extensions.Logging;
using Moq;

namespace EduShield.Api.Tests;

[TestFixture]
public class PerformanceServiceTests
{
    private Mock<IPerformanceRepo> _mockPerformanceRepo = default!;
    private Mock<IStudentRepo> _mockStudentRepo = default!;
    private Mock<IFacultyRepo> _mockFacultyRepo = default!;
    private Mock<ILogger<PerformanceService>> _mockLogger = default!;
    private IMapper _mapper = default!;
    private PerformanceService _service = default!;

    [SetUp]
    public void SetUp()
    {
        _mockPerformanceRepo = new Mock<IPerformanceRepo>();
        _mockStudentRepo = new Mock<IStudentRepo>();
        _mockFacultyRepo = new Mock<IFacultyRepo>();
        _mockLogger = new Mock<ILogger<PerformanceService>>();

        var config = new MapperConfiguration(cfg =>
        {
            cfg.AddProfile<PerformanceMappingProfile>();
        });
        _mapper = config.CreateMapper();

        _service = new PerformanceService(
            _mockPerformanceRepo.Object,
            _mockStudentRepo.Object,
            _mockFacultyRepo.Object,
            _mapper,
            _mockLogger.Object);
    }

    [Test]
    public async Task CreateAsync_ValidRequest_ReturnsPerformanceId()
    {
        // Arrange
        var studentId = Guid.NewGuid();
        var facultyId = Guid.NewGuid();
        var request = new CreatePerformanceReq
        {
            StudentId = studentId,
            FacultyId = facultyId,
            Subject = "Mathematics",
            Marks = 85m,
            MaxMarks = 100m,
            ExamDate = DateTime.UtcNow.AddDays(-1)
        };

        var student = new Student
        {
            Id = studentId,
            FirstName = "John",
            LastName = "Doe",
            Email = "john@test.com",
            PhoneNumber = "1234567890",
            DateOfBirth = new DateTime(2000, 1, 1),
            Address = "Test Address",
            EnrollmentDate = DateTime.UtcNow.AddDays(30),
            Gender = Gender.M,
            CreatedAt = DateTime.UtcNow,
            UpdatedAt = DateTime.UtcNow
        };

        var faculty = new Faculty(facultyId, "Dr. Smith", "Mathematics", "Algebra", Gender.M);

        _mockStudentRepo.Setup(x => x.GetByIdAsync(It.IsAny<CancellationToken>())).ReturnsAsync(student);
        _mockFacultyRepo.Setup(x => x.GetByIdAsync(It.IsAny<CancellationToken>())).ReturnsAsync(faculty);
        _mockPerformanceRepo.Setup(x => x.CreateAsync(It.IsAny<Performance>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync((Performance p, CancellationToken ct) => p);

        // Act
        var result = await _service.CreateAsync(request);

        // Assert
        Assert.That(result, Is.Not.EqualTo(Guid.Empty));
        _mockPerformanceRepo.Verify(x => x.CreateAsync(It.IsAny<Performance>(), It.IsAny<CancellationToken>()), Times.Once);
    }

    [Test]
    public async Task CreateAsync_InvalidStudentId_ThrowsArgumentException()
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

        _mockStudentRepo.Setup(x => x.GetByIdAsync(It.IsAny<Guid>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync((Student?)null);

        // Act & Assert
        var ex = Assert.ThrowsAsync<ArgumentException>(async () => await _service.CreateAsync(request));
        Assert.That(ex.Message, Does.Contain("Student with ID"));
    }

    [Test]
    public async Task CreateAsync_InvalidFacultyId_ThrowsArgumentException()
    {
        // Arrange
        var studentId = Guid.NewGuid();
        var request = new CreatePerformanceReq
        {
            StudentId = studentId,
            FacultyId = Guid.NewGuid(),
            Subject = "Mathematics",
            Marks = 85m,
            MaxMarks = 100m,
            ExamDate = DateTime.UtcNow.AddDays(-1)
        };

        var student = new Student
        {
            Id = studentId,
            FirstName = "John",
            LastName = "Doe",
            Email = "john@test.com",
            PhoneNumber = "1234567890",
            DateOfBirth = new DateTime(2000, 1, 1),
            Address = "Test Address",
            EnrollmentDate = DateTime.UtcNow.AddDays(30),
            Gender = Gender.M,
            CreatedAt = DateTime.UtcNow,
            UpdatedAt = DateTime.UtcNow
        };

        _mockStudentRepo.Setup(x => x.GetByIdAsync(It.IsAny<CancellationToken>())).ReturnsAsync(student);
        _mockFacultyRepo.Setup(x => x.GetByIdAsync(It.IsAny<Guid>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync((Faculty?)null);

        // Act & Assert
        var ex = Assert.ThrowsAsync<ArgumentException>(async () => await _service.CreateAsync(request));
        Assert.That(ex.Message, Does.Contain("Faculty with ID"));
    }

    [Test]
    public async Task GetAsync_ExistingPerformance_ReturnsPerformanceDto()
    {
        // Arrange
        var performanceId = Guid.NewGuid();
        var performance = new Performance(
            performanceId,
            Guid.NewGuid(),
            Guid.NewGuid(),
            "Physics",
            90m,
            100m,
            DateTime.UtcNow.AddDays(-1)
        );

        _mockPerformanceRepo.Setup(x => x.GetByIdAsync(It.IsAny<CancellationToken>())).ReturnsAsync(performance);

        // Act
        var result = await _service.GetAsync(performanceId);

        // Assert
        Assert.That(result, Is.Not.Null);
        Assert.That(result!.PerformanceId, Is.EqualTo(performanceId));
        Assert.That(result.Subject, Is.EqualTo("Physics"));
        Assert.That(result.Marks, Is.EqualTo(90m));
        Assert.That(result.Percentage, Is.EqualTo(90m));
    }

    [Test]
    public async Task GetAsync_NonExistingPerformance_ReturnsNull()
    {
        // Arrange
        _mockPerformanceRepo.Setup(x => x.GetByIdAsync(It.IsAny<Guid>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync((Performance?)null);

        // Act
        var result = await _service.GetAsync(Guid.NewGuid());

        // Assert
        Assert.That(result, Is.Null);
    }

    [Test]
    public async Task GetAllAsync_ReturnsAllPerformances()
    {
        // Arrange
        var performances = new List<Performance>
        {
            new(Guid.NewGuid(), Guid.NewGuid(), Guid.NewGuid(), "Math", 85m, 100m, DateTime.UtcNow.AddDays(-1)),
            new(Guid.NewGuid(), Guid.NewGuid(), Guid.NewGuid(), "Science", 92m, 100m, DateTime.UtcNow.AddDays(-2))
        };

        _mockPerformanceRepo.Setup(x => x.GetAllAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync(performances);

        // Act
        var result = await _service.GetAllAsync();

        // Assert
        Assert.That(result.Count(), Is.EqualTo(2));
    }

    [Test]
    public async Task GetByStudentIdAsync_ReturnsStudentPerformances()
    {
        // Arrange
        var studentId = Guid.NewGuid();
        var performances = new List<Performance>
        {
            new(Guid.NewGuid(), studentId, Guid.NewGuid(), "Math", 85m, 100m, DateTime.UtcNow.AddDays(-1))
        };

        _mockPerformanceRepo.Setup(x => x.GetByStudentIdAsync(It.IsAny<CancellationToken>())).ReturnsAsync(performances);

        // Act
        var result = await _service.GetByStudentIdAsync(studentId);

        // Assert
        Assert.That(result.Count(), Is.EqualTo(1));
        Assert.That(result.First().StudentId, Is.EqualTo(studentId));
    }

    [Test]
    public async Task GetByFacultyIdAsync_ReturnsFacultyPerformances()
    {
        // Arrange
        var facultyId = Guid.NewGuid();
        var performances = new List<Performance>
        {
            new(Guid.NewGuid(), Guid.NewGuid(), facultyId, "Math", 85m, 100m, DateTime.UtcNow.AddDays(-1))
        };

        _mockPerformanceRepo.Setup(x => x.GetByFacultyIdAsync(It.IsAny<CancellationToken>())).ReturnsAsync(performances);

        // Act
        var result = await _service.GetByFacultyIdAsync(facultyId);

        // Assert
        Assert.That(result.Count(), Is.EqualTo(1));
        Assert.That(result.First().FacultyId, Is.EqualTo(facultyId));
    }

    [Test]
    public async Task UpdateAsync_ValidRequest_ReturnsTrue()
    {
        // Arrange
        var performanceId = Guid.NewGuid();
        var studentId = Guid.NewGuid();
        var facultyId = Guid.NewGuid();
        var request = new CreatePerformanceReq
        {
            StudentId = studentId,
            FacultyId = facultyId,
            Subject = "Updated Math",
            Marks = 95m,
            MaxMarks = 100m,
            ExamDate = DateTime.UtcNow.AddDays(-1)
        };

        var student = new Student
        {
            Id = studentId,
            FirstName = "John",
            LastName = "Doe",
            Email = "john@test.com",
            PhoneNumber = "1234567890",
            DateOfBirth = new DateTime(2000, 1, 1),
            Address = "Test Address",
            EnrollmentDate = DateTime.UtcNow.AddDays(30),
            Gender = Gender.M,
            CreatedAt = DateTime.UtcNow,
            UpdatedAt = DateTime.UtcNow
        };

        var faculty = new Faculty(facultyId, "Dr. Smith", "Mathematics", "Algebra", Gender.M);
        var updatedPerformance = new Performance(performanceId, studentId, facultyId, "Updated Math", 95m, 100m, DateTime.UtcNow.AddDays(-1));

        _mockStudentRepo.Setup(x => x.GetByIdAsync(It.IsAny<CancellationToken>())).ReturnsAsync(student);
        _mockFacultyRepo.Setup(x => x.GetByIdAsync(It.IsAny<CancellationToken>())).ReturnsAsync(faculty);
        _mockPerformanceRepo.Setup(x => x.UpdateAsync(It.IsAny<Performance>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(updatedPerformance);

        // Act
        var result = await _service.UpdateAsync(performanceId, request);

        // Assert
        Assert.That(result, Is.True);
    }

    [Test]
    public async Task UpdateAsync_NonExistingPerformance_ReturnsFalse()
    {
        // Arrange
        var performanceId = Guid.NewGuid();
        var studentId = Guid.NewGuid();
        var facultyId = Guid.NewGuid();
        var request = new CreatePerformanceReq
        {
            StudentId = studentId,
            FacultyId = facultyId,
            Subject = "Math",
            Marks = 85m,
            MaxMarks = 100m,
            ExamDate = DateTime.UtcNow.AddDays(-1)
        };

        var student = new Student
        {
            Id = studentId,
            FirstName = "John",
            LastName = "Doe",
            Email = "john@test.com",
            PhoneNumber = "1234567890",
            DateOfBirth = new DateTime(2000, 1, 1),
            Address = "Test Address",
            EnrollmentDate = DateTime.UtcNow.AddDays(30),
            Gender = Gender.M,
            CreatedAt = DateTime.UtcNow,
            UpdatedAt = DateTime.UtcNow
        };

        var faculty = new Faculty(facultyId, "Dr. Smith", "Mathematics", "Algebra", Gender.M);

        _mockStudentRepo.Setup(x => x.GetByIdAsync(It.IsAny<CancellationToken>())).ReturnsAsync(student);
        _mockFacultyRepo.Setup(x => x.GetByIdAsync(It.IsAny<CancellationToken>())).ReturnsAsync(faculty);
        _mockPerformanceRepo.Setup(x => x.UpdateAsync(It.IsAny<Performance>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync((Performance?)null);

        // Act
        var result = await _service.UpdateAsync(performanceId, request);

        // Assert
        Assert.That(result, Is.False);
    }

    [Test]
    public async Task DeleteAsync_ExistingPerformance_ReturnsTrue()
    {
        // Arrange
        var performanceId = Guid.NewGuid();
        _mockPerformanceRepo.Setup(x => x.DeleteAsync(It.IsAny<CancellationToken>())).ReturnsAsync(true);

        // Act
        var result = await _service.DeleteAsync(performanceId);

        // Assert
        Assert.That(result, Is.True);
    }

    [Test]
    public async Task DeleteAsync_NonExistingPerformance_ReturnsFalse()
    {
        // Arrange
        var performanceId = Guid.NewGuid();
        _mockPerformanceRepo.Setup(x => x.DeleteAsync(It.IsAny<CancellationToken>())).ReturnsAsync(false);

        // Act
        var result = await _service.DeleteAsync(performanceId);

        // Assert
        Assert.That(result, Is.False);
    }

}