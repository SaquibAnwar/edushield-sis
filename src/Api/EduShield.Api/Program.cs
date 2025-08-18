using EduShield.Api.Auth;
using EduShield.Api.Auth.Handlers;
using EduShield.Api.Auth.Requirements;
using EduShield.Api.Data;
using EduShield.Api.Services;
using EduShield.Api.Middleware;
using EduShield.Core.Data;
using EduShield.Core.Interfaces;
using EduShield.Core.Mapping;
using EduShield.Core.Validators;
using FluentValidation;
using FluentValidation.AspNetCore;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authorization;
using Microsoft.EntityFrameworkCore;
using Microsoft.OpenApi.Models;
using Microsoft.AspNetCore.Mvc;
using EduShield.Api.Swagger;

using EduShield.Api.Infra;
using Microsoft.AspNetCore.RateLimiting;
using Microsoft.AspNetCore.ResponseCompression;
using OpenTelemetry.Metrics;
using Serilog;

var builder = WebApplication.CreateBuilder(args);

// Add secrets configuration file
builder.Configuration.AddJsonFile("appsettings.Secrets.json", optional: true, reloadOnChange: true);

builder.Host.UseSerilog((ctx, services, lc) => lc
    .ReadFrom.Configuration(ctx.Configuration)
    .Enrich.FromLogContext()
    .WriteTo.Console());

// Add services to the container.
builder.Services.AddControllers();
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen(c =>
{
    c.SwaggerDoc("v1", new OpenApiInfo
    {
        Title = "EduShield SIS API",
        Version = "v1",
        Description = "Student Information System API"
    });

    // Document DevAuth header usage
    c.AddSecurityDefinition("DevAuth", new OpenApiSecurityScheme
    {
        Description = "Use 'DevAuth dev-token' in the Authorization header during development",
        Name = "Authorization",
        In = ParameterLocation.Header,
        Type = SecuritySchemeType.ApiKey,
        Scheme = "DevAuth"
    });

    // Apply security to operations with [Authorize]
    c.OperationFilter<AuthorizeCheckOperationFilter>();
});

// Add AutoMapper
builder.Services.AddAutoMapper(typeof(StudentMappingProfile), typeof(FacultyMappingProfile), typeof(PerformanceMappingProfile), typeof(FeeMappingProfile), typeof(PaymentMappingProfile), typeof(UserMappingProfile), typeof(SessionMappingProfile));

// Add FluentValidation
builder.Services.AddFluentValidationAutoValidation()
                .AddFluentValidationClientsideAdapters();
builder.Services.AddValidatorsFromAssemblyContaining<CreateStudentReqValidator>();

// Add DbContext (conditional per environment or config flag)
var useInMemory = builder.Configuration.GetValue<bool>("UseInMemoryDb");
if (builder.Environment.IsEnvironment("Testing") || useInMemory)
{
    builder.Services.AddDbContext<EduShieldDbContext>(options =>
        options.UseInMemoryDatabase("TestDb").EnableSensitiveDataLogging());
}
else
{
    builder.Services.AddDbContext<EduShieldDbContext>(options =>
        options.UseNpgsql(builder.Configuration.GetConnectionString("Postgres"), 
            b => b.MigrationsAssembly("EduShield.Api")));
}

// Add Repositories
builder.Services.AddScoped<IStudentRepo, StudentRepo>();
builder.Services.AddScoped<IFacultyRepo, FacultyRepo>();
builder.Services.AddScoped<IPerformanceRepo, PerformanceRepo>();
builder.Services.AddScoped<IFeeRepo, FeeRepo>();
builder.Services.AddScoped<IUserRepo, UserRepo>();
builder.Services.AddScoped<ISessionRepo, SessionRepo>();
builder.Services.AddScoped<IAuditRepo, AuditRepo>();

// Add Services
builder.Services.AddScoped<IStudentService, StudentService>();
builder.Services.AddScoped<IFacultyService, FacultyService>();
builder.Services.AddScoped<IPerformanceService, PerformanceService>();
builder.Services.AddScoped<IFeeService, FeeService>();
builder.Services.AddScoped<IAuthService, AuthService>();
builder.Services.AddScoped<IUserService, UserService>();
builder.Services.AddScoped<ISessionService, SessionService>();
builder.Services.AddScoped<IAuditService, AuditService>();
builder.Services.AddScoped<ISecurityMonitoringService, SecurityMonitoringService>();
builder.Services.AddScoped<IConfigurationValidationService, ConfigurationValidationService>();
builder.Services.AddScoped<ITestDataSeeder, TestDataSeeder>();

// Add Authentication Configuration
builder.Services.Configure<EduShield.Core.Configuration.AuthenticationConfiguration>(
    builder.Configuration.GetSection("Authentication"));

// Add Authentication Services
builder.Services.AddScoped<AuthCallbackHandler>();
builder.Services.AddHostedService<SessionCleanupService>();

// Add Authentication
var useDevAuth = builder.Configuration.GetValue<bool>("Auth:UseDevAuth");
if (!builder.Environment.IsEnvironment("Testing"))
{
    // Only add authentication for non-testing environments
    // Testing environment authentication is configured in CustomWebAppFactory
    if (useDevAuth)
    {
        builder.Services.AddAuthentication("DevAuth")
            .AddScheme<AuthenticationSchemeOptions, DevAuthHandler>("DevAuth", options => { });
    }
    else
    {
        builder.Services.AddAuthentication("ProductionAuth")
            .AddScheme<AuthenticationSchemeOptions, ProductionAuthHandler>("ProductionAuth", options => { });
    }
}

// Add Authorization
builder.Services.AddAuthorization(options =>
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
    
    // Resource-based policies
    options.AddPolicy("StudentAccess", policy => 
        policy.Requirements.Add(new StudentAccessRequirement()));
    
    options.AddPolicy("FacultyAccess", policy => 
        policy.Requirements.Add(new FacultyAccessRequirement()));
    
    options.AddPolicy("FeeAccess", policy => 
        policy.Requirements.Add(new FeeAccessRequirement()));
    
    options.AddPolicy("PerformanceAccess", policy => 
        policy.Requirements.Add(new PerformanceAccessRequirement()));
});

// Add Authorization Handlers
builder.Services.AddScoped<IAuthorizationHandler, StudentResourceAuthorizationHandler>();
builder.Services.AddScoped<IAuthorizationHandler, FacultyResourceAuthorizationHandler>();
builder.Services.AddScoped<IAuthorizationHandler, FeeResourceAuthorizationHandler>();
builder.Services.AddScoped<IAuthorizationHandler, PerformanceResourceAuthorizationHandler>();

// Validate Authentication Configuration
var authConfig = builder.Configuration.GetSection("Authentication").Get<EduShield.Core.Configuration.AuthenticationConfiguration>();
if (authConfig != null && !useDevAuth)
{
    // Validate required configuration for production
    if (authConfig.Providers?.ContainsKey("Google") == true)
    {
        var googleConfig = authConfig.Providers["Google"];
        if (string.IsNullOrEmpty(googleConfig.ClientId) || string.IsNullOrEmpty(googleConfig.ClientSecret))
        {
            throw new InvalidOperationException("Google OAuth configuration is incomplete. ClientId and ClientSecret are required.");
        }
    }
    
    if (authConfig.Providers?.ContainsKey("Microsoft") == true)
    {
        var microsoftConfig = authConfig.Providers["Microsoft"];
        if (string.IsNullOrEmpty(microsoftConfig.ClientId) || string.IsNullOrEmpty(microsoftConfig.ClientSecret))
        {
            throw new InvalidOperationException("Microsoft OAuth configuration is incomplete. ClientId and ClientSecret are required.");
        }
    }
}

// Configure secure cookie settings based on environment
if (authConfig != null)
{
    authConfig.RequireSecureCookies = builder.Environment.IsProduction();
    authConfig.EnableDevelopmentBypass = builder.Environment.IsDevelopment();
}

// Consistent 400 shape for model validation errors
builder.Services.Configure<ApiBehaviorOptions>(options =>
{
    options.InvalidModelStateResponseFactory = context =>
    {
        var problem = new ProblemDetails
        {
            Status = StatusCodes.Status400BadRequest,
            Title = "Validation failed",
            Detail = "One or more validation errors occurred.",
            Instance = context.HttpContext.Request.Path
        };
        problem.Extensions["traceId"] = context.HttpContext.TraceIdentifier;
        problem.Extensions["errors"] = context.ModelState
            .Where(kvp => kvp.Value?.Errors.Count > 0)
            .ToDictionary(kvp => kvp.Key, kvp => kvp.Value!.Errors.Select(e => e.ErrorMessage).ToArray());

        var result = new ObjectResult(problem)
        {
            StatusCode = StatusCodes.Status400BadRequest
        };
        result.ContentTypes.Add("application/problem+json");
        return result;
    };
});

// Add Health Checks
var healthChecks = builder.Services.AddHealthChecks();
if (!builder.Environment.IsEnvironment("Testing"))
{
    healthChecks.AddDbContextCheck<EduShieldDbContext>();
    
    // Add authentication health checks
    if (!useDevAuth && authConfig?.Providers != null)
    {
        foreach (var provider in authConfig.Providers)
        {
            healthChecks.AddCheck($"auth-{provider.Key.ToLower()}", () => 
            {
                // Basic configuration validation
                if (string.IsNullOrEmpty(provider.Value.ClientId) || 
                    string.IsNullOrEmpty(provider.Value.Authority))
                {
                    return Microsoft.Extensions.Diagnostics.HealthChecks.HealthCheckResult.Unhealthy(
                        $"{provider.Key} authentication provider is not properly configured");
                }
                
                return Microsoft.Extensions.Diagnostics.HealthChecks.HealthCheckResult.Healthy(
                    $"{provider.Key} authentication provider is configured");
            });
        }
    }
}

// Add CORS
builder.Services.AddCors(options =>
{
    options.AddPolicy("AllowAll", policy =>
    {
        policy.AllowAnyOrigin()
              .AllowAnyMethod()
              .AllowAnyHeader();
    });
});

// Distributed cache (Redis)
builder.Services.AddStackExchangeRedisCache(o =>
    o.Configuration = builder.Configuration.GetValue<string>("Redis:ConnectionString") ?? "localhost:6379");
builder.Services.AddSingleton<ICacheService, DistributedCacheService>();

// Rate limiting & compression
builder.Services.AddResponseCompression(opts =>
{
    opts.EnableForHttps = true;
    opts.MimeTypes = Microsoft.AspNetCore.ResponseCompression.ResponseCompressionDefaults.MimeTypes
        .Concat(new[] { "application/json" });
});
builder.Services.AddRateLimiter(o =>
    o.AddFixedWindowLimiter("global", opt =>
    {
        opt.Window = TimeSpan.FromMinutes(1);
        opt.PermitLimit = 300;
        opt.QueueLimit = 0;
    }));

// Correlation
builder.Services.AddSingleton<CorrelationMiddleware>();

// OpenTelemetry (minimal metrics)
builder.Services.AddOpenTelemetry()
    .WithMetrics(b => b.AddAspNetCoreInstrumentation()
                       .AddHttpClientInstrumentation()
                       .AddConsoleExporter());

var app = builder.Build();

// Configure the HTTP request pipeline.
if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
}

if (!app.Environment.IsEnvironment("Testing"))
{
    app.UseHttpsRedirection();
}

app.UseGlobalProblemDetails();
app.UseMiddleware<CorrelationMiddleware>();
app.UseMiddleware<SecurityHeadersMiddleware>();
app.UseMiddleware<RateLimitingMiddleware>();
app.UseMiddleware<GlobalExceptionMiddleware>();
app.UseSerilogRequestLogging();
app.UseRateLimiter();
app.UseResponseCompression();
app.UseCors("AllowAll");

// Add global exception handling middleware
app.Use(async (context, next) =>
{
    try
    {
        await next();
    }
    catch (Exception ex)
    {
        // Log the exception
        var logger = context.RequestServices.GetRequiredService<ILogger<Program>>();
        logger.LogError(ex, "An unhandled exception occurred: {Message}", ex.Message);
        
        // Return detailed error in development and testing
        if (app.Environment.IsDevelopment() || app.Environment.IsEnvironment("Testing"))
        {
            context.Response.StatusCode = 500;
            context.Response.ContentType = "application/json";
            var errorResponse = new
            {
                error = ex.Message,
                details = ex.ToString(),
                stackTrace = ex.StackTrace
            };
            await context.Response.WriteAsJsonAsync(errorResponse);
        }
        else
        {
            context.Response.StatusCode = 500;
            context.Response.ContentType = "application/json";
            var errorResponse = new { error = "An error occurred while processing your request." };
            await context.Response.WriteAsJsonAsync(errorResponse);
        }
    }
});

app.UseAuthentication();

// Add JWT validation middleware for production auth (skip in testing)
if (!useDevAuth && !app.Environment.IsEnvironment("Testing"))
{
    app.UseMiddleware<JwtValidationMiddleware>();
}

app.UseAuthorization();

app.MapControllers();

app.MapHealthChecks("/api/v1/health/live");
app.MapHealthChecks("/api/v1/health/ready");

// Ensure database is created and migrations are applied (skip during Testing)
// Temporarily disabled for demo purposes
if (!app.Environment.IsEnvironment("Testing"))
{
    using var scope = app.Services.CreateScope();
    var context = scope.ServiceProvider.GetRequiredService<EduShieldDbContext>();
    
    // For in-memory database, ensure it's created
    if (context.Database.IsInMemory())
    {
        context.Database.EnsureCreated();
    }
    else
    {
        // For PostgreSQL, apply migrations
        context.Database.Migrate();
    }
}

app.Run();

public partial class Program { }
