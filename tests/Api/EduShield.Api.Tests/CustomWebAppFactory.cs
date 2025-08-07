using EduShield.Core.Data;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;

namespace EduShield.Api.Tests;

public class CustomWebAppFactory : WebApplicationFactory<Program>
{
    protected override void ConfigureWebHost(IWebHostBuilder builder)
    {
        builder.UseEnvironment("Testing");
        
        builder.ConfigureServices(services =>
        {
            // Remove existing database-related service descriptors
            var descriptorsToRemove = new List<ServiceDescriptor>();
            
            foreach (var service in services)
            {
                if (service.ServiceType == typeof(DbContextOptions<EduShieldDbContext>) ||
                    service.ServiceType == typeof(DbContextOptions) ||
                    service.ServiceType == typeof(EduShieldDbContext) ||
                    service.ServiceType.Name.Contains("Npgsql") ||
                    service.ServiceType.Name.Contains("PostgreSQL") ||
                    service.ServiceType.ToString().Contains("Npgsql") ||
                    service.ServiceType.ToString().Contains("PostgreSQL"))
                {
                    descriptorsToRemove.Add(service);
                }
            }

            foreach (var descriptor in descriptorsToRemove)
            {
                services.Remove(descriptor);
            }

            // Add in-memory database for testing with unique name per test
            services.AddDbContext<EduShieldDbContext>(options =>
            {
                options.UseInMemoryDatabase($"EduShield_Test_{Guid.NewGuid()}");
                options.EnableSensitiveDataLogging();
            });
        });
    }
}
