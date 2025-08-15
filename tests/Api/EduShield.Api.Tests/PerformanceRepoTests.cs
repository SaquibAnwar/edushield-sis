using EduShield.Api.Data;
using EduShield.Core.Data;
using EduShield.Core.Entities;
using EduShield.Core.Enums;
using Microsoft.EntityFrameworkCore;

namespace EduShield.Api.Tests;

[TestFixture]
public class PerformanceRepoTests
{
    private EduShieldDbContext _context = default!;
    private PerformanceRepo _repo = default!;
    private Student _testStudent = default!;
    private Faculty _testFaculty = default!;

    [SetUp]
    public void SetUp()
    {
        var options = new DbContextOptionsBuilder<EduShieldDbContext>()
            .UseInMemoryDatabase(databaseName: Guid.NewGuid().ToString())
            .Options;

        _context = new EduShieldDbContext(options);
        _repo = new PerformanceRepo(_context);

        // Create test student and faculty
        _testStudent = new Student
        {
            Id = Guid.NewGuid(),
            FirstName = "John",
            LastName = "Doe",
            Email = "john.doe@test.com",
            PhoneNumber = "1234567890",
            DateOfBirth = new DateTime(2000, 1, 1),
            Address = "Test Address",
            EnrollmentDate = DateTime.UtcNow.AddDays(30),
            Gender = Gender.M,
            CreatedAt = DateTime.UtcNow,
            UpdatedAt = DateTime.UtcNow
        };

        _testFaculty = new Faculty(Guid.NewGuid(), "Dr. Smith", "Computer Science", "Programming", Gender.M);

        _context.Students.Add(_testStudent);
        _context.Faculty.Add(_testFaculty);
        _context.SaveChanges();
    }

    [TearDown]
    public void TearDown()
    {
        _context.Dispose();
    }

    [Test]
    public async Task CreateAsync_ValidPerformance_ReturnsCreatedPerformance()
    {
        // Arrange
        var performance = new Performance(
            Guid.NewGuid(),
            _testStudent.Id,
            _testFaculty.FacultyId,
            "Mathematics",
            85.5m,
            100m,
            DateTime.UtcNow.AddDays(-1)
        );

        // Act
        var result = await _repo.CreateAsync(performance);

        // Assert
        Assert.That(result, Is.Not.Null);
        Assert.That(result.PerformanceId, Is.EqualTo(performance.PerformanceId));
        Assert.That(result.StudentId, Is.EqualTo(_testStudent.Id));
        Assert.That(result.FacultyId, Is.EqualTo(_testFaculty.FacultyId));
        Assert.That(result.Subject, Is.EqualTo("Mathematics"));
        Assert.That(result.Marks, Is.EqualTo(85.5m));
        Assert.That(result.MaxMarks, Is.EqualTo(100m));
    }

    [Test]
    public async Task GetByIdAsync_ExistingPerformance_ReturnsPerformance()
    {
        // Arrange
        var performance = new Performance(
            Guid.NewGuid(),
            _testStudent.Id,
            _testFaculty.FacultyId,
            "Physics",
            92m,
            100m,
            DateTime.UtcNow.AddDays(-2)
        );
        await _repo.CreateAsync(performance);

        // Act
        var result = await _repo.GetByIdAsync(performance.PerformanceId);

        // Assert
        Assert.That(result, Is.Not.Null);
        Assert.That(result!.PerformanceId, Is.EqualTo(performance.PerformanceId));
        Assert.That(result.Student, Is.Not.Null);
        Assert.That(result.Faculty, Is.Not.Null);
    }

    [Test]
    public async Task GetByIdAsync_NonExistingPerformance_ReturnsNull()
    {
        // Act
        var result = await _repo.GetByIdAsync(Guid.NewGuid());

        // Assert
        Assert.That(result, Is.Null);
    }

    [Test]
    public async Task GetAllAsync_ReturnsAllPerformances()
    {
        // Arrange
        var performance1 = new Performance(
            Guid.NewGuid(),
            _testStudent.Id,
            _testFaculty.FacultyId,
            "Chemistry",
            78m,
            100m,
            DateTime.UtcNow.AddDays(-3)
        );
        var performance2 = new Performance(
            Guid.NewGuid(),
            _testStudent.Id,
            _testFaculty.FacultyId,
            "Biology",
            88m,
            100m,
            DateTime.UtcNow.AddDays(-1)
        );

        await _repo.CreateAsync(performance1);
        await _repo.CreateAsync(performance2);

        // Act
        var result = await _repo.GetAllAsync();

        // Assert
        Assert.That(result.Count(), Is.EqualTo(2));
        // Should be ordered by ExamDate descending
        Assert.That(result.First().Subject, Is.EqualTo("Biology"));
    }

    [Test]
    public async Task GetByStudentIdAsync_ReturnsStudentPerformances()
    {
        // Arrange
        var otherStudent = new Student
        {
            Id = Guid.NewGuid(),
            FirstName = "Jane",
            LastName = "Smith",
            Email = "jane.smith@test.com",
            PhoneNumber = "0987654321",
            DateOfBirth = new DateTime(2001, 1, 1),
            Address = "Other Address",
            EnrollmentDate = DateTime.UtcNow.AddDays(30),
            Gender = Gender.F,
            CreatedAt = DateTime.UtcNow,
            UpdatedAt = DateTime.UtcNow
        };
        _context.Students.Add(otherStudent);
        await _context.SaveChangesAsync();

        var performance1 = new Performance(
            Guid.NewGuid(),
            _testStudent.Id,
            _testFaculty.FacultyId,
            "Math",
            85m,
            100m,
            DateTime.UtcNow.AddDays(-1)
        );
        var performance2 = new Performance(
            Guid.NewGuid(),
            otherStudent.Id,
            _testFaculty.FacultyId,
            "Math",
            90m,
            100m,
            DateTime.UtcNow.AddDays(-1)
        );

        await _repo.CreateAsync(performance1);
        await _repo.CreateAsync(performance2);

        // Act
        var result = await _repo.GetByStudentIdAsync(_testStudent.Id);

        // Assert
        Assert.That(result.Count(), Is.EqualTo(1));
        Assert.That(result.First().StudentId, Is.EqualTo(_testStudent.Id));
    }

    [Test]
    public async Task GetByFacultyIdAsync_ReturnsFacultyPerformances()
    {
        // Arrange
        var otherFaculty = new Faculty(Guid.NewGuid(), "Dr. Johnson", "Mathematics", "Algebra", Gender.F);
        _context.Faculty.Add(otherFaculty);
        await _context.SaveChangesAsync();

        var performance1 = new Performance(
            Guid.NewGuid(),
            _testStudent.Id,
            _testFaculty.FacultyId,
            "Programming",
            85m,
            100m,
            DateTime.UtcNow.AddDays(-1)
        );
        var performance2 = new Performance(
            Guid.NewGuid(),
            _testStudent.Id,
            otherFaculty.FacultyId,
            "Algebra",
            90m,
            100m,
            DateTime.UtcNow.AddDays(-1)
        );

        await _repo.CreateAsync(performance1);
        await _repo.CreateAsync(performance2);

        // Act
        var result = await _repo.GetByFacultyIdAsync(_testFaculty.FacultyId);

        // Assert
        Assert.That(result.Count(), Is.EqualTo(1));
        Assert.That(result.First().FacultyId, Is.EqualTo(_testFaculty.FacultyId));
    }

    [Test]
    public async Task UpdateAsync_ExistingPerformance_UpdatesSuccessfully()
    {
        // Arrange
        var performance = new Performance(
            Guid.NewGuid(),
            _testStudent.Id,
            _testFaculty.FacultyId,
            "History",
            75m,
            100m,
            DateTime.UtcNow.AddDays(-1)
        );
        await _repo.CreateAsync(performance);

        var updatedPerformance = new Performance(
            performance.PerformanceId,
            performance.StudentId,
            performance.FacultyId,
            "Updated History",
            85m,
            performance.MaxMarks,
            performance.ExamDate
        );

        // Act
        var result = await _repo.UpdateAsync(updatedPerformance);

        // Assert
        Assert.That(result, Is.Not.Null);
        Assert.That(result!.Subject, Is.EqualTo("Updated History"));
        Assert.That(result.Marks, Is.EqualTo(85m));
    }

    [Test]
    public async Task UpdateAsync_NonExistingPerformance_ReturnsNull()
    {
        // Arrange
        var performance = new Performance(
            Guid.NewGuid(),
            _testStudent.Id,
            _testFaculty.FacultyId,
            "NonExistent",
            75m,
            100m,
            DateTime.UtcNow.AddDays(-1)
        );

        // Act
        var result = await _repo.UpdateAsync(performance);

        // Assert
        Assert.That(result, Is.Null);
    }

    [Test]
    public async Task DeleteAsync_ExistingPerformance_DeletesSuccessfully()
    {
        // Arrange
        var performance = new Performance(
            Guid.NewGuid(),
            _testStudent.Id,
            _testFaculty.FacultyId,
            "Geography",
            80m,
            100m,
            DateTime.UtcNow.AddDays(-1)
        );
        await _repo.CreateAsync(performance);

        // Act
        var result = await _repo.DeleteAsync(performance.PerformanceId);

        // Assert
        Assert.That(result, Is.True);
        var deletedPerformance = await _repo.GetByIdAsync(performance.PerformanceId);
        Assert.That(deletedPerformance, Is.Null);
    }

    [Test]
    public async Task DeleteAsync_NonExistingPerformance_ReturnsFalse()
    {
        // Act
        var result = await _repo.DeleteAsync(Guid.NewGuid());

        // Assert
        Assert.That(result, Is.False);
    }
}