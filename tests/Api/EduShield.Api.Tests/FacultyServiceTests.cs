using Moq;
using AutoMapper;
using EduShield.Core.Dtos;
using EduShield.Core.Entities;
using EduShield.Core.Interfaces;
using EduShield.Api.Services;
using EduShield.Core.Enums;

namespace EduShield.Api.Tests;

[TestFixture]
public class FacultyServiceTests
{
    private readonly Mock<IFacultyRepo> _mockFacultyRepo;
    private readonly Mock<IMapper> _mockMapper;
    private readonly FacultyService _service;

    public FacultyServiceTests()
    {
        _mockFacultyRepo = new Mock<IFacultyRepo>();
        _mockMapper = new Mock<IMapper>();
        _service = new FacultyService(_mockFacultyRepo.Object, _mockMapper.Object);
    }

    [Test]
    public async Task CreateAsync_ValidRequest_ReturnsFacultyId()
    {
        // Arrange
        var request = new CreateFacultyReq
        {
            Name = "John Doe",
            Department = "Computer Science",
            Subject = "Programming",
            Gender = Gender.M
        };
        var faculty = new Faculty
        {
            FacultyId = Guid.NewGuid(),
            Name = request.Name,
            Department = request.Department,
            Subject = request.Subject,
            Gender = request.Gender
        };
        var facultyDto = new FacultyDto
        {
            FacultyId = faculty.FacultyId,
            Name = faculty.Name,
            Department = faculty.Department,
            Subject = faculty.Subject,
            Gender = faculty.Gender
        };

        _mockMapper.Setup(x => x.Map<Faculty>(request)).Returns(faculty);
        _mockFacultyRepo.Setup(x => x.CreateAsync(It.IsAny<CancellationToken>())).ReturnsAsync(faculty);

        // Act
        var result = await _service.CreateAsync(request, CancellationToken.None);

        // Assert
        Assert.That(result, Is.EqualTo(faculty.FacultyId));
        _mockFacultyRepo.Verify(x => x.CreateAsync(faculty, It.IsAny<CancellationToken>()), Times.Once);
    }

    [Test]
    public async Task GetAsync_ValidId_ReturnsFacultyDto()
    {
        // Arrange
        var facultyId = Guid.NewGuid();
        var faculty = new Faculty
        {
            FacultyId = facultyId,
            Name = "John Doe",
            Department = "Computer Science",
            Subject = "Programming",
            Gender = Gender.M
        };
        var facultyDto = new FacultyDto
        {
            FacultyId = faculty.FacultyId,
            Name = faculty.Name,
            Department = faculty.Department,
            Subject = faculty.Subject,
            Gender = faculty.Gender
        };

        _mockFacultyRepo.Setup(x => x.GetByIdAsync(It.IsAny<CancellationToken>())).ReturnsAsync(faculty);
        _mockMapper.Setup(x => x.Map<FacultyDto>(faculty)).Returns(facultyDto);

        // Act
        var result = await _service.GetAsync(facultyId, CancellationToken.None);

        // Assert
        Assert.That(result, Is.Not.Null);
        Assert.That(result.FacultyId, Is.EqualTo(facultyId));
        Assert.That(result.Name, Is.EqualTo(faculty.Name));
    }

    [Test]
    public async Task GetAsync_InvalidId_ReturnsNull()
    {
        // Arrange
        var facultyId = Guid.NewGuid();
        _mockFacultyRepo.Setup(x => x.GetByIdAsync(facultyId))
            .ReturnsAsync((Faculty?)null);

        // Act
        var result = await _service.GetAsync(facultyId, CancellationToken.None);

        // Assert
        Assert.That(result, Is.Null);
    }

    [Test]
    public async Task GetAllAsync_ReturnsAllFaculties()
    {
        // Arrange
        var faculties = new List<Faculty>
        {
            new() { FacultyId = Guid.NewGuid(), Name = "John Doe", Department = "CS", Subject = "Programming", Gender = Gender.M },
            new() { FacultyId = Guid.NewGuid(), Name = "Jane Smith", Department = "Math", Subject = "Calculus", Gender = Gender.F }
        };
        var facultyDtos = faculties.Select(f => new FacultyDto
        {
            FacultyId = f.FacultyId,
            Name = f.Name,
            Department = f.Department,
            Subject = f.Subject,
            Gender = f.Gender
        }).ToList();

        _mockFacultyRepo.Setup(x => x.GetAllAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync(faculties);
        _mockMapper.Setup(x => x.Map<IEnumerable<FacultyDto>>(faculties)).Returns(facultyDtos);

        // Act
        var result = await _service.GetAllAsync(CancellationToken.None);

        // Assert
        Assert.That(result, Is.Not.Null);
        Assert.That(result.Count(), Is.EqualTo(2));
    }

    [Test]
    public async Task UpdateAsync_ValidId_ReturnsTrue()
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
        var existingFaculty = new Faculty
        {
            FacultyId = facultyId,
            Name = "John Doe",
            Department = "Computer Science",
            Subject = "Programming",
            Gender = Gender.M
        };

        _mockFacultyRepo.Setup(x => x.GetByIdAsync(It.IsAny<CancellationToken>())).ReturnsAsync(existingFaculty);
        _mockFacultyRepo.Setup(x => x.UpdateAsync(It.IsAny<CancellationToken>())).ReturnsAsync(existingFaculty);

        // Act
        var result = await _service.UpdateAsync(facultyId, request, CancellationToken.None);

        // Assert
        Assert.That(result, Is.True);
        Assert.That(existingFaculty.Name, Is.EqualTo(request.Name));
        Assert.That(existingFaculty.Department, Is.EqualTo(request.Department));
        Assert.That(existingFaculty.Subject, Is.EqualTo(request.Subject));
        Assert.That(existingFaculty.Gender, Is.EqualTo(request.Gender));
        _mockFacultyRepo.Verify(x => x.UpdateAsync(existingFaculty, It.IsAny<CancellationToken>()), Times.Once);
    }

    [Test]
    public async Task UpdateAsync_InvalidId_ReturnsFalse()
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

        _mockFacultyRepo.Setup(x => x.GetByIdAsync(facultyId))
            .ReturnsAsync((Faculty?)null);

        // Act
        var result = await _service.UpdateAsync(facultyId, request, CancellationToken.None);

        // Assert
        Assert.That(result, Is.False);
        _mockFacultyRepo.Verify(x => x.UpdateAsync(It.IsAny<Faculty>(), It.IsAny<CancellationToken>()), Times.Never);
    }

    [Test]
    public async Task DeleteAsync_ValidId_ReturnsTrue()
    {
        // Arrange
        var facultyId = Guid.NewGuid();
        var existingFaculty = new Faculty
        {
            FacultyId = facultyId,
            Name = "John Doe",
            Department = "Computer Science",
            Subject = "Programming",
            Gender = Gender.M
        };

        _mockFacultyRepo.Setup(x => x.GetByIdAsync(It.IsAny<CancellationToken>())).ReturnsAsync(existingFaculty);
        _mockFacultyRepo.Setup(x => x.DeleteAsync(facultyId))
            .Returns(Task.CompletedTask);

        // Act
        var result = await _service.DeleteAsync(facultyId, CancellationToken.None);

        // Assert
        Assert.That(result, Is.True);
        _mockFacultyRepo.Verify(x => x.DeleteAsync(facultyId, It.IsAny<CancellationToken>()), Times.Once);
    }

    [Test]
    public async Task DeleteAsync_InvalidId_ReturnsFalse()
    {
        // Arrange
        var facultyId = Guid.NewGuid();
        _mockFacultyRepo.Setup(x => x.GetByIdAsync(facultyId))
            .ReturnsAsync((Faculty?)null);

        // Act
        var result = await _service.DeleteAsync(facultyId, CancellationToken.None);

        // Assert
        Assert.That(result, Is.False);
        _mockFacultyRepo.Verify(x => x.DeleteAsync(It.IsAny<Guid>(), It.IsAny<CancellationToken>()), Times.Never);
    }

}