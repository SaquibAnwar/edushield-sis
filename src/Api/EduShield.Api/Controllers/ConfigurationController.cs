using EduShield.Api.Services;
using EduShield.Core.Configuration;
using EduShield.Core.Interfaces;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Options;

namespace EduShield.Api.Controllers;

[ApiController]
[Route("api/[controller]")]
[Authorize(Policy = "SystemAdminOnly")]
public class ConfigurationController : ControllerBase
{
    private readonly IConfigurationValidationService _configValidationService;
    private readonly IOptionsMonitor<AuthenticationConfiguration> _authConfig;
    private readonly IAuditService _auditService;
    private readonly ILogger<ConfigurationController> _logger;

    public ConfigurationController(
        IConfigurationValidationService configValidationService,
        IOptionsMonitor<AuthenticationConfiguration> authConfig,
        IAuditService auditService,
        ILogger<ConfigurationController> logger)
    {
        _configValidationService = configValidationService;
        _authConfig = authConfig;
        _auditService = auditService;
        _logger = logger;
    }

    [HttpGet("validate")]
    public async Task<IActionResult> ValidateConfiguration()
    {
        try
        {
            var result = await _configValidationService.ValidateAllConfigurationsAsync();
            
            await _auditService.LogAsync(
                "ConfigurationValidationRequested",
                "Configuration validation was requested",
                Guid.Parse(User.FindFirst("sub")?.Value ?? Guid.Empty.ToString()),
                true,
                null,
                null,
                HttpContext.Connection.RemoteIpAddress?.ToString(),
                Request.Headers["User-Agent"].ToString());

            return Ok(new
            {
                isValid = result.IsValid,
                hasWarnings = result.HasWarnings,
                errors = result.Errors,
                warnings = result.Warnings,
                validatedAt = DateTime.UtcNow
            });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error validating configuration");
            return StatusCode(500, "An error occurred while validating configuration");
        }
    }

    [HttpGet("validate/auth")]
    public async Task<IActionResult> ValidateAuthConfiguration()
    {
        try
        {
            var result = await _configValidationService.ValidateAuthenticationConfigurationAsync();
            
            return Ok(new
            {
                isValid = result.IsValid,
                hasWarnings = result.HasWarnings,
                errors = result.Errors,
                warnings = result.Warnings,
                validatedAt = DateTime.UtcNow
            });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error validating authentication configuration");
            return StatusCode(500, "An error occurred while validating authentication configuration");
        }
    }

    [HttpGet("issues")]
    public async Task<IActionResult> GetConfigurationIssues()
    {
        try
        {
            var issues = await _configValidationService.GetConfigurationIssuesAsync();
            return Ok(issues);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error retrieving configuration issues");
            return StatusCode(500, "An error occurred while retrieving configuration issues");
        }
    }

    [HttpGet("auth")]
    public async Task<IActionResult> GetAuthConfiguration()
    {
        try
        {
            var config = _authConfig.CurrentValue;
            
            // Return sanitized configuration (without secrets)
            var sanitizedConfig = new
            {
                sessionTimeout = config.SessionTimeout,
                requireSecureCookies = config.RequireSecureCookies,
                enableDevelopmentBypass = config.EnableDevelopmentBypass,
                providers = config.Providers?.ToDictionary(
                    kvp => kvp.Key,
                    kvp => new
                    {
                        authority = kvp.Value.Authority,
                        scopes = kvp.Value.Scopes,
                        clientIdConfigured = !string.IsNullOrEmpty(kvp.Value.ClientId),
                        clientSecretConfigured = !string.IsNullOrEmpty(kvp.Value.ClientSecret)
                    })
            };

            await _auditService.LogAsync(
                "AuthConfigurationViewed",
                "Authentication configuration was viewed",
                Guid.Parse(User.FindFirst("sub")?.Value ?? Guid.Empty.ToString()),
                true,
                null,
                null,
                HttpContext.Connection.RemoteIpAddress?.ToString(),
                Request.Headers["User-Agent"].ToString());

            return Ok(sanitizedConfig);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error retrieving authentication configuration");
            return StatusCode(500, "An error occurred while retrieving authentication configuration");
        }
    }

    [HttpGet("health-checks")]
    public async Task<IActionResult> GetHealthCheckStatus()
    {
        try
        {
            // This would typically integrate with the health check service
            // For now, return basic status
            var healthStatus = new
            {
                database = "Healthy",
                redis = "Healthy",
                authentication = "Healthy",
                lastChecked = DateTime.UtcNow
            };

            return Ok(healthStatus);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error retrieving health check status");
            return StatusCode(500, "An error occurred while retrieving health check status");
        }
    }

    [HttpPost("reload")]
    public async Task<IActionResult> ReloadConfiguration()
    {
        try
        {
            // In a real implementation, this would trigger configuration reload
            // For now, just log the action
            await _auditService.LogAsync(
                "ConfigurationReloadRequested",
                "Configuration reload was requested",
                Guid.Parse(User.FindFirst("sub")?.Value ?? Guid.Empty.ToString()),
                true,
                null,
                null,
                HttpContext.Connection.RemoteIpAddress?.ToString(),
                Request.Headers["User-Agent"].ToString());

            _logger.LogInformation("Configuration reload requested by user {UserId}", 
                User.FindFirst("sub")?.Value);

            return Ok(new { message = "Configuration reload requested", timestamp = DateTime.UtcNow });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error reloading configuration");
            return StatusCode(500, "An error occurred while reloading configuration");
        }
    }

    [HttpGet("environment")]
    public async Task<IActionResult> GetEnvironmentInfo()
    {
        try
        {
            var environmentInfo = new
            {
                environment = Environment.GetEnvironmentVariable("ASPNETCORE_ENVIRONMENT") ?? "Unknown",
                machineName = Environment.MachineName,
                osVersion = Environment.OSVersion.ToString(),
                dotNetVersion = Environment.Version.ToString(),
                workingDirectory = Environment.CurrentDirectory,
                timestamp = DateTime.UtcNow
            };

            await _auditService.LogAsync(
                "EnvironmentInfoViewed",
                "Environment information was viewed",
                Guid.Parse(User.FindFirst("sub")?.Value ?? Guid.Empty.ToString()),
                true,
                null,
                null,
                HttpContext.Connection.RemoteIpAddress?.ToString(),
                Request.Headers["User-Agent"].ToString());

            return Ok(environmentInfo);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error retrieving environment information");
            return StatusCode(500, "An error occurred while retrieving environment information");
        }
    }
}