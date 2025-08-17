namespace EduShield.Core.Configuration;

public class AuthenticationConfiguration
{
    public Dictionary<string, OidcConfiguration> Providers { get; set; } = new();
    public string DefaultProvider { get; set; } = "Google";
    public TimeSpan SessionTimeout { get; set; } = TimeSpan.FromHours(8);
    public bool EnableAuditLogging { get; set; } = true;
    public bool AllowMultipleSessions { get; set; } = false;
    
    // Legacy properties for backward compatibility
    public int SessionTimeoutMinutes 
    { 
        get => (int)SessionTimeout.TotalMinutes;
        set => SessionTimeout = TimeSpan.FromMinutes(value);
    }
    
    public int MaxConcurrentSessions { get; set; } = 5;
    public string CookieName { get; set; } = "EduShield.Auth";
    public bool RequireSecureCookies { get; set; } = true;
    public bool EnableDevelopmentBypass { get; set; } = false;
}