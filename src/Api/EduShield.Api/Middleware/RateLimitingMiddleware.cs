using System.Collections.Concurrent;
using System.Net;

namespace EduShield.Api.Middleware;

public class RateLimitingMiddleware
{
    private readonly RequestDelegate _next;
    private readonly ILogger<RateLimitingMiddleware> _logger;
    private readonly ConcurrentDictionary<string, ClientRequestInfo> _clients = new();
    private readonly Timer _cleanupTimer;

    // Rate limiting configuration
    private const int MaxRequestsPerMinute = 60;
    private const int MaxAuthRequestsPerMinute = 10;
    private const int BlockDurationMinutes = 15;

    public RateLimitingMiddleware(RequestDelegate next, ILogger<RateLimitingMiddleware> logger)
    {
        _next = next;
        _logger = logger;

        // Cleanup expired entries every 5 minutes
        _cleanupTimer = new Timer(CleanupExpiredEntries, null, TimeSpan.FromMinutes(5), TimeSpan.FromMinutes(5));
    }

    public async Task InvokeAsync(HttpContext context)
    {
        var clientId = GetClientIdentifier(context);
        var isAuthEndpoint = IsAuthenticationEndpoint(context.Request.Path);

        if (IsRateLimited(clientId, isAuthEndpoint))
        {
            _logger.LogWarning("Rate limit exceeded for client {ClientId} on path {Path}", 
                clientId, context.Request.Path);

            context.Response.StatusCode = (int)HttpStatusCode.TooManyRequests;
            context.Response.Headers["Retry-After"] = "900"; // 15 minutes
            
            await context.Response.WriteAsync("Rate limit exceeded. Please try again later.");
            return;
        }

        await _next(context);
    }

    private string GetClientIdentifier(HttpContext context)
    {
        // Use IP address as primary identifier
        var ipAddress = context.Connection.RemoteIpAddress?.ToString() ?? "unknown";
        
        // For authenticated requests, also consider user ID
        var userId = context.User?.FindFirst("sub")?.Value;
        if (!string.IsNullOrEmpty(userId))
        {
            return $"{ipAddress}:{userId}";
        }

        return ipAddress;
    }

    private bool IsAuthenticationEndpoint(PathString path)
    {
        return path.StartsWithSegments("/api/auth") || 
               path.StartsWithSegments("/api/users/login");
    }

    private bool IsRateLimited(string clientId, bool isAuthEndpoint)
    {
        var now = DateTime.UtcNow;
        var clientInfo = _clients.GetOrAdd(clientId, _ => new ClientRequestInfo());

        lock (clientInfo)
        {
            // Check if client is currently blocked
            if (clientInfo.BlockedUntil.HasValue && now < clientInfo.BlockedUntil.Value)
            {
                return true;
            }

            // Reset counters if a minute has passed
            if (now - clientInfo.LastReset > TimeSpan.FromMinutes(1))
            {
                clientInfo.RequestCount = 0;
                clientInfo.AuthRequestCount = 0;
                clientInfo.LastReset = now;
                clientInfo.BlockedUntil = null;
            }

            // Increment counters
            clientInfo.RequestCount++;
            if (isAuthEndpoint)
            {
                clientInfo.AuthRequestCount++;
            }

            // Check limits
            var generalLimitExceeded = clientInfo.RequestCount > MaxRequestsPerMinute;
            var authLimitExceeded = isAuthEndpoint && clientInfo.AuthRequestCount > MaxAuthRequestsPerMinute;

            if (generalLimitExceeded || authLimitExceeded)
            {
                // Block the client
                clientInfo.BlockedUntil = now.AddMinutes(BlockDurationMinutes);
                
                _logger.LogWarning("Client {ClientId} blocked until {BlockedUntil}. " +
                    "General requests: {RequestCount}, Auth requests: {AuthRequestCount}",
                    clientId, clientInfo.BlockedUntil, clientInfo.RequestCount, clientInfo.AuthRequestCount);

                return true;
            }

            return false;
        }
    }

    private void CleanupExpiredEntries(object? state)
    {
        var now = DateTime.UtcNow;
        var expiredKeys = new List<string>();

        foreach (var kvp in _clients)
        {
            var clientInfo = kvp.Value;
            lock (clientInfo)
            {
                // Remove entries that haven't been accessed in the last hour
                if (now - clientInfo.LastReset > TimeSpan.FromHours(1) && 
                    (!clientInfo.BlockedUntil.HasValue || now > clientInfo.BlockedUntil.Value))
                {
                    expiredKeys.Add(kvp.Key);
                }
            }
        }

        foreach (var key in expiredKeys)
        {
            _clients.TryRemove(key, out _);
        }

        if (expiredKeys.Count > 0)
        {
            _logger.LogDebug("Cleaned up {Count} expired rate limiting entries", expiredKeys.Count);
        }
    }

    public void Dispose()
    {
        _cleanupTimer?.Dispose();
    }

    private class ClientRequestInfo
    {
        public int RequestCount { get; set; }
        public int AuthRequestCount { get; set; }
        public DateTime LastReset { get; set; } = DateTime.UtcNow;
        public DateTime? BlockedUntil { get; set; }
    }
}