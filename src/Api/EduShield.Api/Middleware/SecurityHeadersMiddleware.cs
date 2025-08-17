using Microsoft.Extensions.Primitives;

namespace EduShield.Api.Middleware;

public class SecurityHeadersMiddleware
{
    private readonly RequestDelegate _next;
    private readonly ILogger<SecurityHeadersMiddleware> _logger;

    public SecurityHeadersMiddleware(RequestDelegate next, ILogger<SecurityHeadersMiddleware> logger)
    {
        _next = next;
        _logger = logger;
    }

    public async Task InvokeAsync(HttpContext context)
    {
        // Add security headers
        AddSecurityHeaders(context.Response);

        await _next(context);
    }

    private static void AddSecurityHeaders(HttpResponse response)
    {
        // Prevent clickjacking
        response.Headers["X-Frame-Options"] = "DENY";

        // Prevent MIME type sniffing
        response.Headers["X-Content-Type-Options"] = "nosniff";

        // Enable XSS protection
        response.Headers["X-XSS-Protection"] = "1; mode=block";

        // Referrer policy
        response.Headers["Referrer-Policy"] = "strict-origin-when-cross-origin";

        // Content Security Policy
        response.Headers["Content-Security-Policy"] = 
            "default-src 'self'; " +
            "script-src 'self' 'unsafe-inline' 'unsafe-eval'; " +
            "style-src 'self' 'unsafe-inline'; " +
            "img-src 'self' data: https:; " +
            "font-src 'self'; " +
            "connect-src 'self'; " +
            "frame-ancestors 'none'";

        // Strict Transport Security (HTTPS only)
        if (response.HttpContext.Request.IsHttps)
        {
            response.Headers["Strict-Transport-Security"] = "max-age=31536000; includeSubDomains";
        }

        // Permissions Policy
        response.Headers["Permissions-Policy"] = 
            "camera=(), microphone=(), geolocation=(), payment=()";

        // Remove server information
        response.Headers.Remove("Server");
        response.Headers.Remove("X-Powered-By");
        response.Headers.Remove("X-AspNet-Version");
        response.Headers.Remove("X-AspNetMvc-Version");
    }
}