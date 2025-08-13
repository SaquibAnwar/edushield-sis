using EduShield.Api.Auth;
using EduShield.Api.Data;
using EduShield.Api.Services;
using EduShield.Core.Data;
using EduShield.Core.Interfaces;
using EduShield.Core.Mapping;
using EduShield.Core.Validators;
using FluentValidation;
using FluentValidation.AspNetCore;
using Microsoft.AspNetCore.Authentication;
using Microsoft.EntityFrameworkCore;
using Microsoft.OpenApi.Models;
using Microsoft.AspNetCore.Mvc;
using EduShield.Api.Swagger;
using EduShield.Api.Endpoints;
using EduShield.Api.Infra;
using Microsoft.AspNetCore.RateLimiting;
using Microsoft.AspNetCore.ResponseCompression;
using OpenTelemetry.Metrics;
using Serilog;
using EduShield.Api.Infra;
using Microsoft.AspNetCore.RateLimiting;
using Microsoft.AspNetCore.ResponseCompression;

var builder = WebApplication.CreateBuilder(args);
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
builder.Services.AddAutoMapper(typeof(StudentMappingProfile));

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
        options.UseNpgsql(builder.Configuration.GetConnectionString("Postgres")));
}

// Add Repositories
builder.Services.AddScoped<IStudentRepo, StudentRepo>();

// Add Services
builder.Services.AddScoped<IStudentService, StudentService>();

// Add Authentication
builder.Services.AddAuthentication("DevAuth")
    .AddScheme<AuthenticationSchemeOptions, DevAuthHandler>("DevAuth", options => { });

// Add Authorization
builder.Services.AddAuthorization();

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
app.UseAuthorization();

app.MapControllers();
app.MapStudentQueryEndpoints();

app.MapHealthChecks("/healthz/live");
app.MapHealthChecks("/healthz/ready");
app.MapHealthChecks("/health");

// Ensure database is created and migrations are applied (skip during Testing)
if (!app.Environment.IsEnvironment("Testing"))
{
    using var scope = app.Services.CreateScope();
    var context = scope.ServiceProvider.GetRequiredService<EduShieldDbContext>();
    context.Database.Migrate();
}

app.Run();

public partial class Program { }
