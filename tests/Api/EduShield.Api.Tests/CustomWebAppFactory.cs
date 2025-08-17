using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authorization;
using Microsoft.Extensions.Configuration;
using EduShield.Core.Data;
using EduShield.Core.Entities;
using EduShield.Core.Enums;

namespace EduShield.Api.Tests;

public class CustomWebAppFactory : WebApplicationFactory<Program>
{
    protected override void ConfigureWebHost(IWebHostBuilder builder)
    {
        builder.UseEnvironment("Testing");
        builder.ConfigureServices(services =>
        {
            // Remove the existing DbContext registration
            var descriptor = services.SingleOrDefault(
                d => d.ServiceType == typeof(DbContextOptions<EduShieldDbContext>));
            if (descriptor != null)
            {
                services.Remove(descriptor);
            }

            // Add in-memory database for testing, replace any existing provider
            services.AddDbContext<EduShieldDbContext>(options =>
                options.UseInMemoryDatabase("TestDb")
                       .EnableSensitiveDataLogging());

            // Configure test authorization to match production policies
            services.AddAuthorization(options =>
            {
                // Role-based policies
                options.AddPolicy("StudentOnly", policy => 
                    policy.RequireRole("Student"));
                
                options.AddPolicy("ParentOnly", policy => 
                    policy.RequireRole("Parent"));
                
                options.AddPolicy("TeacherOnly", policy => 
                    policy.RequireRole("Teacher"));
                
                options.AddPolicy("SchoolAdminOnly", policy => 
                    policy.RequireRole("SchoolAdmin"));
                
                options.AddPolicy("SystemAdminOnly", policy => 
                    policy.RequireRole("SystemAdmin"));
                
                // Hierarchical policies
                options.AddPolicy("TeacherOrAdmin", policy => 
                    policy.RequireRole("Teacher", "SchoolAdmin", "SystemAdmin"));
                
                options.AddPolicy("AdminOnly", policy => 
                    policy.RequireRole("SchoolAdmin", "SystemAdmin"));
                
                // For testing, we'll simplify resource-based policies to just require authentication
                options.AddPolicy("StudentAccess", policy => 
                    policy.RequireAuthenticatedUser());
                
                options.AddPolicy("FacultyAccess", policy => 
                    policy.RequireAuthenticatedUser());
                
                options.AddPolicy("FeeAccess", policy => 
                    policy.RequireAuthenticatedUser());
                
                options.AddPolicy("PerformanceAccess", policy => 
                    policy.RequireAuthenticatedUser());
            });
        });

        // Override authentication configuration
        builder.ConfigureAppConfiguration((context, config) =>
        {
            config.AddInMemoryCollection(new Dictionary<string, string?>
            {
                ["Auth:UseDevAuth"] = "false", // Disable dev auth
                ["UseInMemoryDb"] = "true"
            });
        });

        builder.ConfigureServices(services =>
        {
            // Remove any existing authentication services that might have been added
            var authServices = services.Where(s => 
                s.ServiceType.FullName?.Contains("Authentication") == true).ToList();
            foreach (var service in authServices)
            {
                services.Remove(service);
            }

            // Add test authentication scheme as the default
            services.AddAuthentication(options =>
            {
                options.DefaultScheme = "Test";
                options.DefaultAuthenticateScheme = "Test";
                options.DefaultChallengeScheme = "Test";
            })
            .AddScheme<TestAuthenticationSchemeOptions, TestAuthenticationHandler>("Test", options => 
            {
                options.DefaultRole = UserRole.SchoolAdmin;
            });

            // Seed test data
            var serviceProvider = services.BuildServiceProvider();
            using var scope = serviceProvider.CreateScope();
            var context = scope.ServiceProvider.GetRequiredService<EduShieldDbContext>();
            SeedTestData(context);
        });
    }

    private static void SeedTestData(EduShieldDbContext context)
    {
        // Ensure database is created
        context.Database.EnsureCreated();

        // Check if test user already exists
        if (!context.Users.Any(u => u.ExternalId == "test-user-id"))
        {
            var testUserId = new Guid("12345678-1234-1234-1234-123456789012");
            var testUser = new User
            {
                UserId = Guid.NewGuid(),
                Email = "test@example.com",
                FirstName = "Test",
                LastName = "User",
                Role = UserRole.Student,
                IsActive = true,
                Provider = AuthProvider.Google
            };

            context.Users.Add(testUser);
            context.SaveChanges();
        }
    }
}
