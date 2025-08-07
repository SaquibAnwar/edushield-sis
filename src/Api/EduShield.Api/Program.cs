using EduShield.Api.Data;
using EduShield.Api.Services;
using EduShield.Core.Data;
using EduShield.Core.Dtos;
using EduShield.Core.Interfaces;
using EduShield.Core.Mapping;
using EduShield.Core.Validators;
using FluentValidation;
using FluentValidation.AspNetCore;
using Microsoft.EntityFrameworkCore;
using Serilog;

// Configure Serilog
Log.Logger = new LoggerConfiguration()
    .Enrich.FromLogContext()
    .WriteTo.Console()
    .CreateBootstrapLogger();

var builder = WebApplication.CreateBuilder(args);

// Configure Serilog
builder.Host.UseSerilog((ctx, lc) => lc
    .ReadFrom.Configuration(ctx.Configuration)
    .Enrich.FromLogContext()
    .WriteTo.Console());

// Add database context
var cs = builder.Configuration.GetConnectionString("Postgres") ??
         "Host=localhost;Port=5432;Database=edushield;Username=postgres;Password=secret";
builder.Services.AddDbContext<EduShieldDbContext>(o =>
    o.UseNpgsql(cs));

// Add health checks
builder.Services.AddHealthChecks()
    .AddNpgSql(cs, name: "postgres")
    .AddDbContextCheck<EduShieldDbContext>("efcore");

// Add AutoMapper
builder.Services.AddAutoMapper(typeof(StudentMappingProfile));

// Add FluentValidation
builder.Services.AddFluentValidationAutoValidation()
                .AddFluentValidationClientsideAdapters();
builder.Services.AddValidatorsFromAssemblyContaining<CreateStudentReqValidator>();

// Add repositories and services
builder.Services.AddScoped<IStudentRepo, StudentRepo>();
builder.Services.AddScoped<IStudentService, StudentService>();

// Add services to the container.
// Learn more about configuring Swagger/OpenAPI at https://aka.ms/aspnetcore/swashbuckle
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();

var app = builder.Build();

// Configure the HTTP request pipeline.
if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
}

app.UseHttpsRedirection();

var summaries = new[]
{
    "Freezing", "Bracing", "Chilly", "Cool", "Mild", "Warm", "Balmy", "Hot", "Sweltering", "Scorching"
};

// Map health check endpoint
app.MapHealthChecks("/healthz");

// Map student endpoints
app.MapPost("/v1/students", async (CreateStudentReq req, IStudentService svc, ILogger<Program> log, CancellationToken ct) =>
{
    try
    {
        var id = await svc.CreateAsync(req, ct);
        return Results.Created($"/v1/students/{id}", new { id });
    }
    catch (ArgumentException ex)
    {
        log.LogWarning("Validation error creating student: {Error}", ex.Message);
        return Results.BadRequest(new { error = ex.Message });
    }
    catch (Exception ex)
    {
        log.LogError(ex, "Error creating student");
        return Results.StatusCode(500);
    }
})
.WithName("CreateStudent")
.WithOpenApi()
.WithSummary("Create a new student")
.WithDescription("Creates a new student with the provided information");

app.MapGet("/v1/students/{id:guid}", async (Guid id, IStudentService svc, CancellationToken ct) =>
    await svc.GetAsync(id, ct)
        is { } dto ? Results.Ok(dto) : Results.NotFound())
.WithName("GetStudent")
.WithOpenApi()
.WithSummary("Get student by ID")
.WithDescription("Retrieves a student by their unique identifier");

app.MapGet("/weatherforecast", () =>
{
    var forecast = Enumerable.Range(1, 5).Select(index =>
        new WeatherForecast
        (
            DateOnly.FromDateTime(DateTime.Now.AddDays(index)),
            Random.Shared.Next(-20, 55),
            summaries[Random.Shared.Next(summaries.Length)]
        ))
        .ToArray();
    return forecast;
})
.WithName("GetWeatherForecast")
.WithOpenApi();

app.Run();

record WeatherForecast(DateOnly Date, int TemperatureC, string? Summary)
{
    public int TemperatureF => 32 + (int)(TemperatureC / 0.5556);
}

// Make Program accessible for testing
public partial class Program;
