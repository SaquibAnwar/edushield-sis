using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using Moq;
using EduShield.Core.Data;
using EduShield.Core.Entities;
using EduShield.Core.Interfaces;
using EduShield.Api.Data;
using EduShield.Core.Enums;

namespace EduShield.Api.Tests;

[TestFixture]
public class FacultyRepoTests : IDisposable
{
    private readonly DbContextOptions<EduShieldDbContext> _options;
    private readonly EduShieldDbContext _context;
    private readonly FacultyRepo _repo;

    public FacultyRepoTests()
    {
        _options = new DbContextOptionsBuilder<EduShieldDbContext>()
            .UseInMemoryDatabase(databaseName: Guid.NewGuid().ToString())
            .Options;
        _context = new EduShieldDbContext(_options);
        _repo = new FacultyRepo(_context);
    }

    [SetUp]
    public void Setup()
    {
        _context.Database.EnsureDeleted();
        _context.Database.EnsureCreated();
    }

    [Test]
    public async Task CreateAsync_ValidFaculty_ReturnsCreatedFaculty()
    {
        // Arrange
        var faculty = new Faculty
        {
            FacultyId = Guid.NewGuid(),
            Name = "John Doe",
            Department = "Computer Science",
            Subject = "Programming",
            Gender = Gender.M
        };

        // Act
        var result = await _repo.CreateAsync(faculty, CancellationToken.None);

        // Assert
        Assert.That(result, Is.Not.Null);
        Assert.That(result.FacultyId, Is.EqualTo(faculty.FacultyId));
        Assert.That(result.Name, Is.EqualTo(faculty.Name));
        Assert.That(result.Department, Is.EqualTo(faculty.Department));
        Assert.That(result.Subject, Is.EqualTo(faculty.Subject));
        Assert.That(result.Gender, Is.EqualTo(faculty.Gender));
    }

    [Test]
    public async Task GetByIdAsync_ExistingFaculty_ReturnsFaculty()
    {
        // Arrange
        var faculty = new Faculty
        {
            FacultyId = Guid.NewGuid(),
            Name = "John Doe",
            Department = "Computer Science",
            Subject = "Programming",
            Gender = Gender.M
        };
        await _context.Faculty.AddAsync(faculty);
        await _context.SaveChangesAsync();

        // Act
        var result = await _repo.GetByIdAsync(faculty.FacultyId, CancellationToken.None);

        // Assert
        Assert.That(result, Is.Not.Null);
        Assert.That(result.FacultyId, Is.EqualTo(faculty.FacultyId));
        Assert.That(result.Name, Is.EqualTo(faculty.Name));
    }

    [Test]
    public async Task GetByIdAsync_NonExistingFaculty_ReturnsNull()
    {
        // Arrange
        var facultyId = Guid.NewGuid();

        // Act
        var result = await _repo.GetByIdAsync(facultyId, CancellationToken.None);

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
        await _context.Faculty.AddRangeAsync(faculties);
        await _context.SaveChangesAsync();

        // Act
        var result = await _repo.GetAllAsync(CancellationToken.None);

        // Assert
        Assert.That(result, Is.Not.Null);
        Assert.That(result.Count(), Is.EqualTo(2));
        Assert.That(result, Has.Some.Matches<Faculty>(f => f.FirstName == "John"));
        Assert.That(result, Has.Some.Matches<Faculty>(f => f.FirstName == "Jane"));
    }

    [Test]
    public async Task UpdateAsync_ExistingFaculty_UpdatesSuccessfully()
    {
        // Arrange
        var faculty = new Faculty
        {
            FacultyId = Guid.NewGuid(),
            Name = "John Doe",
            Department = "Computer Science",
            Subject = "Programming",
            Gender = Gender.M
        };
        await _context.Faculty.AddAsync(faculty);
        await _context.SaveChangesAsync();

        // // // // //  // Fixed: Use object initializer // Fixed: Use object initializer // Fixed: Use object initializer // Fixed: Use constructor or factory method // Fixed: Use TestHelpers
        faculty.Department = "Updated Department";

        // Act
        var result = await _repo.UpdateAsync(faculty, CancellationToken.None);

        // Assert
        Assert.That(result, Is.Not.Null);
        Assert.That(result.Name, Is.EqualTo("John Doe Updated"));
        Assert.That(result.Department, Is.EqualTo("Updated Department"));

        // Verify in database
        var updatedFaculty = await _context.Faculty.FindAsync(faculty.FacultyId);
        Assert.That(updatedFaculty, Is.Not.Null);
        Assert.That(updatedFaculty.Name, Is.EqualTo("John Doe Updated"));
        Assert.That(updatedFaculty.Department, Is.EqualTo("Updated Department"));
    }

    [Test]
    public async Task DeleteAsync_ExistingFaculty_DeletesSuccessfully()
    {
        // Arrange
        var faculty = new Faculty
        {
            FacultyId = Guid.NewGuid(),
            Name = "John Doe",
            Department = "Computer Science",
            Subject = "Programming",
            Gender = Gender.M
        };
        await _context.Faculty.AddAsync(faculty);
        await _context.SaveChangesAsync();

        // Act
        await _repo.DeleteAsync(faculty.FacultyId, CancellationToken.None);

        // Assert
        var deletedFaculty = await _context.Faculty.FindAsync(faculty.FacultyId);
        Assert.That(deletedFaculty, Is.Null);
    }

    [Test]
    public async Task DeleteAsync_NonExistingFaculty_DoesNotThrowException()
    {
        // Arrange
        var facultyId = Guid.NewGuid();

        // Act & Assert
        await _repo.DeleteAsync(facultyId, CancellationToken.None);
        // Should not throw exception
    }

    public void Dispose()
    {
        _context.Database.EnsureDeleted();
        _context.Dispose();
    }
}
