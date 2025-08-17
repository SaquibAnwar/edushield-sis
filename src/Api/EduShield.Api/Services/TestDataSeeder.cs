using EduShield.Core.Data;
using EduShield.Core.Entities;
using EduShield.Core.Enums;
using EduShield.Core.Security;
using Microsoft.EntityFrameworkCore;

namespace EduShield.Api.Services;

public interface ITestDataSeeder
{
    Task SeedTestUsersAsync();
    Task SeedTestDataAsync();
    Task CleanupTestDataAsync();
}

public class TestDataSeeder : ITestDataSeeder
{
    private readonly EduShieldDbContext _context;
    private readonly ILogger<TestDataSeeder> _logger;

    public TestDataSeeder(EduShieldDbContext context, ILogger<TestDataSeeder> logger)
    {
        _context = context;
        _logger = logger;
    }

    public async Task SeedTestUsersAsync()
    {
        if (await _context.Users.AnyAsync())
        {
            _logger.LogInformation("Test users already exist, skipping seeding");
            return;
        }

        var testUsers = new List<User>
        {
            // System Admin
            new User
            {
                UserId = Guid.NewGuid(),
                Email = "admin@edushield.test",
                FirstName = "System",
                LastName = "Administrator",
                Role = UserRole.SystemAdmin,
                Provider = AuthProvider.Custom,
                ExternalId = "admin-001",
                IsActive = true
            },

            // School Admin
            new User
            {
                UserId = Guid.NewGuid(),
                Email = "schooladmin@edushield.test",
                FirstName = "School",
                LastName = "Administrator",
                Role = UserRole.SchoolAdmin,
                Provider = AuthProvider.Custom,
                ExternalId = "schooladmin-001",
                IsActive = true
            },

            // Teacher
            new User
            {
                UserId = Guid.NewGuid(),
                Email = "teacher@edushield.test",
                FirstName = "Test",
                LastName = "Teacher",
                Role = UserRole.Teacher,
                Provider = AuthProvider.Custom,
                ExternalId = "teacher-001",
                IsActive = true
            },

            // Parent
            new User
            {
                UserId = Guid.NewGuid(),
                Email = "parent@edushield.test",
                FirstName = "Test",
                LastName = "Parent",
                Role = UserRole.Parent,
                Provider = AuthProvider.Custom,
                ExternalId = "parent-001",
                IsActive = true
            },

            // Student
            new User
            {
                UserId = Guid.NewGuid(),
                Email = "student@edushield.test",
                FirstName = "Test",
                LastName = "Student",
                Role = UserRole.Student,
                Provider = AuthProvider.Custom,
                ExternalId = "student-001",
                IsActive = true
            },

            // Inactive User for testing
            new User
            {
                UserId = Guid.NewGuid(),
                Email = "inactive@edushield.test",
                FirstName = "Inactive",
                LastName = "User",
                Role = UserRole.Student,
                Provider = AuthProvider.Custom,
                ExternalId = "inactive-001",
                IsActive = false
            }
        };

        await _context.Users.AddRangeAsync(testUsers);
        await _context.SaveChangesAsync();

        _logger.LogInformation("Seeded {Count} test users", testUsers.Count);
    }

    public async Task SeedTestDataAsync()
    {
        await SeedTestUsersAsync();

        // Seed test students
        if (!await _context.Students.AnyAsync())
        {
            var studentUser = await _context.Users.FirstAsync(u => u.Role == UserRole.Student);

            var testStudents = new List<Student>
            {
                new Student
                {
                    Id = Guid.NewGuid(),
                    FirstName = "Test",
                    LastName = "Student 1",
                    Email = "student1@edushield.test",
                    PhoneNumber = "123-456-7890",
                    DateOfBirth = DateTime.UtcNow.AddYears(-16),
                    Address = "123 Test St",
                    EnrollmentDate = DateTime.UtcNow.AddMonths(-6),
                    Gender = Gender.M,
                    UserId = studentUser.UserId,
                    CreatedAt = DateTime.UtcNow
                },
                new Student
                {
                    Id = Guid.NewGuid(),
                    FirstName = "Test",
                    LastName = "Student 2",
                    Email = "student2@edushield.test",
                    PhoneNumber = "123-456-7891",
                    DateOfBirth = DateTime.UtcNow.AddYears(-17),
                    Address = "124 Test St",
                    EnrollmentDate = DateTime.UtcNow.AddMonths(-8),
                    Gender = Gender.F,
                    CreatedAt = DateTime.UtcNow
                }
            };

            await _context.Students.AddRangeAsync(testStudents);
        }

        // Seed test faculty
        if (!await _context.Faculty.AnyAsync())
        {
            var teacherUser = await _context.Users.FirstAsync(u => u.Role == UserRole.Teacher);

            var testFaculty = new List<Faculty>
            {
                new Faculty
                {
                    FacultyId = Guid.NewGuid(),
                    Name = "Test Teacher 1",
                    Department = "Mathematics",
                    Subject = "Algebra",
                    Gender = Gender.M,
                    UserId = teacherUser.UserId
                },
                new Faculty
                {
                    FacultyId = Guid.NewGuid(),
                    Name = "Test Teacher 2",
                    Department = "Science",
                    Subject = "Physics",
                    Gender = Gender.F
                }
            };

            await _context.Faculty.AddRangeAsync(testFaculty);
        }

        // Seed test sessions for testing session management
        var users = await _context.Users.Take(3).ToListAsync();
        if (!await _context.UserSessions.AnyAsync())
        {
            var testSessions = new List<UserSession>();

            foreach (var user in users)
            {
                testSessions.Add(new UserSession
                {
                    SessionId = Guid.NewGuid(),
                    UserId = user.UserId,
                    SessionToken = Guid.NewGuid().ToString("N"),
                    IpAddress = "127.0.0.1",
                    UserAgent = "Test Agent",
                    ExpiresAt = DateTime.UtcNow.AddHours(24),
                    IsActive = true
                });
            }

            await _context.UserSessions.AddRangeAsync(testSessions);
        }

        // Seed test audit logs
        if (!await _context.AuditLogs.AnyAsync())
        {
            var testAuditLogs = new List<AuditLog>();

            foreach (var user in users)
            {
                testAuditLogs.AddRange(new[]
                {
                    new AuditLog
                    {
                        AuditId = Guid.NewGuid(),
                        Action = "UserLogin",
                        Resource = "User logged in successfully",
                        UserId = user.UserId,
                        IpAddress = "127.0.0.1",
                        UserAgent = "Test Agent",
                        Success = true
                    },
                    new AuditLog
                    {
                        AuditId = Guid.NewGuid(),
                        Action = "ProfileViewed",
                        Resource = "User viewed their profile",
                        UserId = user.UserId,
                        IpAddress = "127.0.0.1",
                        UserAgent = "Test Agent",
                        Success = true
                    }
                });
            }

            await _context.AuditLogs.AddRangeAsync(testAuditLogs);
        }

        await _context.SaveChangesAsync();
        _logger.LogInformation("Test data seeding completed");
    }

    public async Task CleanupTestDataAsync()
    {
        // Remove test data in reverse dependency order
        var testEmails = new[] 
        { 
            "admin@edushield.test", 
            "schooladmin@edushield.test", 
            "teacher@edushield.test", 
            "parent@edushield.test", 
            "student@edushield.test", 
            "inactive@edushield.test",
            "student1@edushield.test",
            "student2@edushield.test",
            "teacher1@edushield.test",
            "teacher2@edushield.test"
        };

        // Remove audit logs
        var testAuditLogs = await _context.AuditLogs
            .Where(al => al.User != null && testEmails.Contains(al.User.Email))
            .ToListAsync();
        _context.AuditLogs.RemoveRange(testAuditLogs);

        // Remove sessions
        var testSessions = await _context.UserSessions
            .Where(s => s.User != null && testEmails.Contains(s.User.Email))
            .ToListAsync();
        _context.UserSessions.RemoveRange(testSessions);

        // Remove students
        var testStudents = await _context.Students
            .Where(s => testEmails.Contains(s.Email))
            .ToListAsync();
        _context.Students.RemoveRange(testStudents);

        // Remove faculty
        var testFaculty = await _context.Faculty
            .Where(f => f.User != null && testEmails.Contains(f.User.Email))
            .ToListAsync();
        _context.Faculty.RemoveRange(testFaculty);

        // Remove users
        var testUsers = await _context.Users
            .Where(u => testEmails.Contains(u.Email))
            .ToListAsync();
        _context.Users.RemoveRange(testUsers);

        await _context.SaveChangesAsync();
        _logger.LogInformation("Test data cleanup completed");
    }
}