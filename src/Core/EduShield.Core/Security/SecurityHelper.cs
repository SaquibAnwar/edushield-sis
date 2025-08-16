using System.Net;
using System.Security.Cryptography;
using System.Text;

namespace EduShield.Core.Security;

public static class SecurityHelper
{
    public static string HashPassword(string password, string salt)
    {
        using var sha256 = SHA256.Create();
        var saltedPassword = password + salt;
        var hashedBytes = sha256.ComputeHash(Encoding.UTF8.GetBytes(saltedPassword));
        return Convert.ToBase64String(hashedBytes);
    }

    public static string GenerateSalt()
    {
        using var rng = RandomNumberGenerator.Create();
        var bytes = new byte[16];
        rng.GetBytes(bytes);
        return Convert.ToBase64String(bytes);
    }

    public static bool IsValidEmail(string email)
    {
        try
        {
            var addr = new System.Net.Mail.MailAddress(email);
            return addr.Address == email;
        }
        catch
        {
            return false;
        }
    }

    public static string SanitizeIpAddress(string? ipAddress)
    {
        if (string.IsNullOrEmpty(ipAddress))
            return "Unknown";

        if (IPAddress.TryParse(ipAddress, out var ip))
        {
            return ip.ToString();
        }

        return "Invalid";
    }

    public static string SanitizeUserAgent(string? userAgent)
    {
        if (string.IsNullOrEmpty(userAgent))
            return "Unknown";

        // Truncate very long user agents
        if (userAgent.Length > 500)
            return userAgent.Substring(0, 500) + "...";

        return userAgent;
    }

    public static bool IsSecureConnection(string scheme)
    {
        return string.Equals(scheme, "https", StringComparison.OrdinalIgnoreCase);
    }
}