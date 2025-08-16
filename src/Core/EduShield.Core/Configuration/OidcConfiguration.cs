namespace EduShield.Core.Configuration;

public class OidcConfiguration
{
    public string Authority { get; set; } = string.Empty;
    public string ClientId { get; set; } = string.Empty;
    public string ClientSecret { get; set; } = string.Empty;
    public string[] Scopes { get; set; } = ["openid", "profile", "email"];
    public string ResponseType { get; set; } = "code";
    public bool RequireHttpsMetadata { get; set; } = true;
    public TimeSpan TokenValidationTimeout { get; set; } = TimeSpan.FromMinutes(5);
    public string? RedirectUri { get; set; }
}