using EduShield.Core.Exceptions;
using EduShield.Core.Interfaces;
using System.Net;
using System.Text.Json;

namespace EduShield.Api.Middleware;

public class GlobalExceptionMiddleware
{
    private readonly RequestDelegate _next;
    private readonly ILogger<GlobalExceptionMiddleware> _logger;
    private readonly IServiceProvider _serviceProvider;

    public GlobalExceptionMiddleware(
        RequestDelegate next, 
        ILogger<GlobalExceptionMiddleware> logger,
        IServiceProvider serviceProvider)
    {
        _next = next;
        _logger = logger;
        _serviceProvider = serviceProvider;
    }

    public async Task InvokeAsync(HttpContext context)
    {
        try
        {
            await _next(context);
        }
        catch (Exception ex)
        {
            await HandleExceptionAsync(context, ex);
        }
    }

    private async Task HandleExceptionAsync(HttpContext context, Exception exception)
    {
        var correlationId = context.TraceIdentifier;
        var userId = context.User?.FindFirst("sub")?.Value;
        var ipAddress = context.Connection.RemoteIpAddress?.ToString();
        var userAgent = context.Request.Headers["User-Agent"].ToString();

        // Log the exception
        _logger.LogError(exception, 
            "Unhandled exception occurred. CorrelationId: {CorrelationId}, UserId: {UserId}, IP: {IpAddress}",
            correlationId, userId, ipAddress);

        // Log security events for authentication/authorization exceptions
        if (exception is AuthenticationException || exception is UnauthorizedAccessException)
        {
            try
            {
                using var scope = _serviceProvider.CreateScope();
                var auditService = scope.ServiceProvider.GetService<IAuditService>();
                if (auditService != null)
                {
                    await auditService.LogSecurityEventAsync(
                        "AuthenticationError",
                        $"Authentication/Authorization error: {exception.Message}",
                        userId != null ? Guid.Parse(userId) : null,
                        ipAddress,
                        userAgent);
                }
            }
            catch (Exception auditEx)
            {
                _logger.LogError(auditEx, "Failed to log security event");
            }
        }

        var response = CreateErrorResponse(exception, correlationId);
        var jsonResponse = JsonSerializer.Serialize(response, new JsonSerializerOptions
        {
            PropertyNamingPolicy = JsonNamingPolicy.CamelCase
        });

        context.Response.ContentType = "application/json";
        context.Response.StatusCode = GetStatusCode(exception);

        await context.Response.WriteAsync(jsonResponse);
    }

    private static ErrorResponse CreateErrorResponse(Exception exception, string correlationId)
    {
        return exception switch
        {
            AuthenticationException authEx => new ErrorResponse
            {
                Error = "Authentication Error",
                Message = "Authentication failed. Please check your credentials.",
                CorrelationId = correlationId,
                Details = GetSafeDetails(authEx)
            },
            UnauthorizedAccessException => new ErrorResponse
            {
                Error = "Authorization Error",
                Message = "You don't have permission to access this resource.",
                CorrelationId = correlationId
            },
            ArgumentException argEx => new ErrorResponse
            {
                Error = "Invalid Request",
                Message = "The request contains invalid data.",
                CorrelationId = correlationId,
                Details = GetSafeDetails(argEx)
            },
            InvalidOperationException opEx => new ErrorResponse
            {
                Error = "Invalid Operation",
                Message = "The requested operation cannot be performed.",
                CorrelationId = correlationId,
                Details = GetSafeDetails(opEx)
            },
            TimeoutException => new ErrorResponse
            {
                Error = "Request Timeout",
                Message = "The request took too long to process. Please try again.",
                CorrelationId = correlationId
            },
            _ => new ErrorResponse
            {
                Error = "Internal Server Error",
                Message = "An unexpected error occurred. Please try again later.",
                CorrelationId = correlationId
            }
        };
    }

    private static int GetStatusCode(Exception exception)
    {
        return exception switch
        {
            AuthenticationException => (int)HttpStatusCode.Unauthorized,
            UnauthorizedAccessException => (int)HttpStatusCode.Forbidden,
            ArgumentException => (int)HttpStatusCode.BadRequest,
            InvalidOperationException => (int)HttpStatusCode.BadRequest,
            TimeoutException => (int)HttpStatusCode.RequestTimeout,
            _ => (int)HttpStatusCode.InternalServerError
        };
    }

    private static string? GetSafeDetails(Exception exception)
    {
        // Only include exception details for certain types and in development
        var isDevelopment = Environment.GetEnvironmentVariable("ASPNETCORE_ENVIRONMENT") == "Development";
        
        if (isDevelopment && (exception is ArgumentException || exception is InvalidOperationException))
        {
            return exception.Message;
        }

        return null;
    }

    private class ErrorResponse
    {
        public string Error { get; set; } = string.Empty;
        public string Message { get; set; } = string.Empty;
        public string CorrelationId { get; set; } = string.Empty;
        public string? Details { get; set; }
        public DateTime Timestamp { get; set; } = DateTime.UtcNow;
    }
}