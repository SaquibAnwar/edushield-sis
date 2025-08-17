using EduShield.Core.Configuration;
using EduShield.Core.Interfaces;
using Microsoft.Extensions.Options;

namespace EduShield.Api.Services;

public interface IConfigurationValidationService
{
    Task<ConfigurationValidationResult> ValidateAuthenticationConfigurationAsync();
    Task<ConfigurationValidationResult> ValidateAllConfigurationsAsync();
    Task<IEnumerable<ConfigurationIssue>> GetConfigurationIssuesAsync();
}

public class ConfigurationValidationService : IConfigurationValidationService
{
    private readonly IOptionsMonitor<AuthenticationConfiguration> _authConfig;
    private readonly IConfiguration _configuration;
    private readonly IAuditService _auditService;
    private readonly ILogger<ConfigurationValidationService> _logger;

    public ConfigurationValidationService(
        IOptionsMonitor<AuthenticationConfiguration> authConfig,
        IConfiguration configuration,
        IAuditService auditService,
        ILogger<ConfigurationValidationService> logger)
    {
        _authConfig = authConfig;
        _configuration = configuration;
        _auditService = auditService;
        _logger = logger;
    }

    public async Task<ConfigurationValidationResult> ValidateAuthenticationConfigurationAsync()
    {
        var result = new ConfigurationValidationResult();
        var config = _authConfig.CurrentValue;

        if (config == null)
        {
            result.AddError("Authentication configuration is missing");
            return result;
        }

        // Validate session settings
        if (config.SessionTimeout <= TimeSpan.Zero)
        {
            result.AddError("Session timeout must be greater than 0");
        }

        // Validate OIDC providers
        if (config.Providers != null)
        {
            foreach (var provider in config.Providers)
            {
                ValidateOidcProvider(provider.Key, provider.Value, result);
            }
        }

        // Validate security settings
        if (config.RequireSecureCookies && !IsHttpsConfigured())
        {
            result.AddWarning("Secure cookies are required but HTTPS is not configured");
        }

        await LogConfigurationValidationResultAsync("AuthenticationConfiguration", result);
        return result;
    }

    public async Task<ConfigurationValidationResult> ValidateAllConfigurationsAsync()
    {
        var result = new ConfigurationValidationResult();

        // Validate authentication configuration
        var authResult = await ValidateAuthenticationConfigurationAsync();
        result.Merge(authResult);

        // Validate database configuration
        var dbResult = ValidateDatabaseConfiguration();
        result.Merge(dbResult);

        // Validate Redis configuration
        var redisResult = ValidateRedisConfiguration();
        result.Merge(redisResult);

        // Validate logging configuration
        var loggingResult = ValidateLoggingConfiguration();
        result.Merge(loggingResult);

        await LogConfigurationValidationResultAsync("AllConfigurations", result);
        return result;
    }

    public async Task<IEnumerable<ConfigurationIssue>> GetConfigurationIssuesAsync()
    {
        var issues = new List<ConfigurationIssue>();
        var result = await ValidateAllConfigurationsAsync();

        issues.AddRange(result.Errors.Select(error => new ConfigurationIssue
        {
            Type = "Error",
            Message = error,
            Severity = "High",
            Category = "Configuration"
        }));

        issues.AddRange(result.Warnings.Select(warning => new ConfigurationIssue
        {
            Type = "Warning",
            Message = warning,
            Severity = "Medium",
            Category = "Configuration"
        }));

        return issues;
    }

    private void ValidateOidcProvider(string providerName, OidcConfiguration config, ConfigurationValidationResult result)
    {
        if (string.IsNullOrEmpty(config.ClientId))
        {
            result.AddError($"{providerName} provider: Client ID is required");
        }

        if (string.IsNullOrEmpty(config.ClientSecret))
        {
            result.AddError($"{providerName} provider: Client secret is required");
        }

        if (string.IsNullOrEmpty(config.Authority))
        {
            result.AddError($"{providerName} provider: Authority URL is required");
        }
        else if (!Uri.TryCreate(config.Authority, UriKind.Absolute, out var authorityUri) || 
                 (!authorityUri.Scheme.Equals("https", StringComparison.OrdinalIgnoreCase)))
        {
            result.AddError($"{providerName} provider: Authority must be a valid HTTPS URL");
        }

        if (config.Scopes == null || !config.Scopes.Any())
        {
            result.AddWarning($"{providerName} provider: No scopes configured");
        }
        else
        {
            var requiredScopes = new[] { "openid", "email", "profile" };
            var missingScopes = requiredScopes.Except(config.Scopes, StringComparer.OrdinalIgnoreCase);
            if (missingScopes.Any())
            {
                result.AddWarning($"{providerName} provider: Missing recommended scopes: {string.Join(", ", missingScopes)}");
            }
        }
    }

    private ConfigurationValidationResult ValidateDatabaseConfiguration()
    {
        var result = new ConfigurationValidationResult();
        var connectionString = _configuration.GetConnectionString("Postgres");

        if (string.IsNullOrEmpty(connectionString))
        {
            result.AddError("Database connection string is missing");
        }
        else
        {
            try
            {
                // Basic connection string validation
                var builder = new Npgsql.NpgsqlConnectionStringBuilder(connectionString);
                
                if (string.IsNullOrEmpty(builder.Host))
                {
                    result.AddError("Database host is missing from connection string");
                }

                if (string.IsNullOrEmpty(builder.Database))
                {
                    result.AddError("Database name is missing from connection string");
                }

                if (string.IsNullOrEmpty(builder.Username))
                {
                    result.AddWarning("Database username is missing from connection string");
                }

                if (builder.SslMode == Npgsql.SslMode.Disable)
                {
                    result.AddWarning("Database SSL is not enabled - consider enabling for production");
                }
            }
            catch (Exception ex)
            {
                result.AddError($"Invalid database connection string: {ex.Message}");
            }
        }

        return result;
    }

    private ConfigurationValidationResult ValidateRedisConfiguration()
    {
        var result = new ConfigurationValidationResult();
        var redisConnectionString = _configuration.GetValue<string>("Redis:ConnectionString");

        if (string.IsNullOrEmpty(redisConnectionString))
        {
            result.AddWarning("Redis connection string is missing - caching will be limited");
        }

        return result;
    }

    private ConfigurationValidationResult ValidateLoggingConfiguration()
    {
        var result = new ConfigurationValidationResult();
        var loggingSection = _configuration.GetSection("Serilog");

        if (!loggingSection.Exists())
        {
            result.AddWarning("Serilog configuration is missing");
        }

        return result;
    }

    private bool IsHttpsConfigured()
    {
        var urls = _configuration.GetValue<string>("ASPNETCORE_URLS");
        return !string.IsNullOrEmpty(urls) && urls.Contains("https://", StringComparison.OrdinalIgnoreCase);
    }

    private async Task LogConfigurationValidationResultAsync(string configurationType, ConfigurationValidationResult result)
    {
        if (result.HasErrors || result.HasWarnings)
        {
            var message = $"Configuration validation for {configurationType}: " +
                         $"{result.Errors.Count} errors, {result.Warnings.Count} warnings";

            _logger.LogWarning(message);

            await _auditService.LogAsync(
                "ConfigurationValidation",
                message,
                null,
                true,
                null,
                null,
                "System",
                "ConfigurationValidationService");
        }
    }
}

public class ConfigurationValidationResult
{
    public List<string> Errors { get; } = new();
    public List<string> Warnings { get; } = new();

    public bool HasErrors => Errors.Any();
    public bool HasWarnings => Warnings.Any();
    public bool IsValid => !HasErrors;

    public void AddError(string error)
    {
        Errors.Add(error);
    }

    public void AddWarning(string warning)
    {
        Warnings.Add(warning);
    }

    public void Merge(ConfigurationValidationResult other)
    {
        Errors.AddRange(other.Errors);
        Warnings.AddRange(other.Warnings);
    }


}

public class ConfigurationIssue
{
    public string Type { get; set; } = string.Empty;
    public string Message { get; set; } = string.Empty;
    public string Severity { get; set; } = string.Empty;
    public string Category { get; set; } = string.Empty;
    public DateTime DetectedAt { get; set; } = DateTime.UtcNow;
}