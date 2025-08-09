using EduShield.Core.Data;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

namespace EduShield.Api.Tests;

public class CustomWebAppFactory : WebApplicationFactory<Program>
{
    protected override void ConfigureWebHost(IWebHostBuilder builder)
    {
        builder.UseEnvironment("Testing");
        
        // Clear any existing configuration and use only test-specific configuration
        builder.ConfigureAppConfiguration((context, config) =>
        {
            config.Sources.Clear();
            config.AddInMemoryCollection(new Dictionary<string, string?>
            {
                {"ConnectionStrings:Postgres", ""}
            });
        });

        builder.ConfigureServices((context, services) =>
        {
            // Find and remove ALL database-related services with a comprehensive approach
            var descriptorsToRemove = new List<ServiceDescriptor>();
            
            foreach (var service in services.ToList())
            {
                var shouldRemove = false;
                var serviceTypeName = service.ServiceType?.FullName ?? "";
                var implementationTypeName = service.ImplementationType?.FullName ?? "";
                var assemblyName = service.ServiceType?.Assembly?.GetName()?.Name ?? "";
                
                // Remove DbContext and DbContextOptions services
                if (service.ServiceType == typeof(DbContextOptions<EduShieldDbContext>) ||
                    service.ServiceType == typeof(DbContextOptions) ||
                    service.ServiceType == typeof(EduShieldDbContext))
                {
                    shouldRemove = true;
                }
                
                // Remove Npgsql and PostgreSQL services
                if (serviceTypeName.Contains("Npgsql") ||
                    serviceTypeName.Contains("PostgreSQL") ||
                    implementationTypeName.Contains("Npgsql") ||
                    implementationTypeName.Contains("PostgreSQL") ||
                    assemblyName.Contains("Npgsql") ||
                    assemblyName.Contains("PostgreSQL"))
                {
                    shouldRemove = true;
                }
                
                // Remove EF Core internal services that might be causing conflicts
                if (serviceTypeName.Contains("Microsoft.EntityFrameworkCore") &&
                    (serviceTypeName.Contains("Database") ||
                     serviceTypeName.Contains("Provider") ||
                     serviceTypeName.Contains("Connection") ||
                     serviceTypeName.Contains("Migration")))
                {
                    shouldRemove = true;
                }
                
                if (shouldRemove)
                {
                    descriptorsToRemove.Add(service);
                }
            }

            // Remove all identified services
            foreach (var descriptor in descriptorsToRemove)
            {
                services.Remove(descriptor);
            }

            // Add ONLY in-memory database for testing with a fresh registration
            services.AddDbContext<EduShieldDbContext>(options =>
            {
                options.UseInMemoryDatabase($"EduShield_Test_{Guid.NewGuid()}");
                options.EnableSensitiveDataLogging();
                options.EnableDetailedErrors();
            }, ServiceLifetime.Scoped);
        });
    }
}
