using System.Security.Cryptography;
using System.Text;

namespace EduShield.Api.Infra;

public static class ETagHelper
{
    public static string Compute(string input)
    {
        using var sha = SHA256.Create();
        var bytes = sha.ComputeHash(Encoding.UTF8.GetBytes(input));
        return $"W/\"{Convert.ToBase64String(bytes)}\"";
    }
}


