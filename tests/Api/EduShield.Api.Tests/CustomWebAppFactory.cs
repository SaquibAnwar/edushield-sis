using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authorization;
using EduShield.Core.Data;

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

            // Configure test authentication
            services.AddAuthentication("Test")
                .AddScheme<AuthenticationSchemeOptions, TestAuthHandler>("Test", options => { });

            // Configure test authorization
            services.AddAuthorization(options =>
            {
                options.AddPolicy("SchoolAdmin", policy =>
                    policy.RequireAuthenticatedUser());
            });
        });
    }
}
