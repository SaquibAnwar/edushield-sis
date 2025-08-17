using EduShield.Api.Services;
using EduShield.Core.Interfaces;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace EduShield.Api.Controllers;

[ApiController]
[Route("api/[controller]")]
[Authorize(Policy = "SystemAdminOnly")]
public class SecurityController : ControllerBase
{
    private readonly ISecurityMonitoringService _securityMonitoringService;
    private readonly IAuditService _auditService;
    private readonly ILogger<SecurityController> _logger;

    public SecurityController(
        ISecurityMonitoringService securityMonitoringService,
        IAuditService auditService,
        ILogger<SecurityController> logger)
    {
        _securityMonitoringService = securityMonitoringService;
        _auditService = auditService;
        _logger = logger;
    }

    [HttpGet("alerts")]
    public async Task<IActionResult> GetActiveAlerts()
    {
        try
        {
            var alerts = await _securityMonitoringService.GetActiveAlertsAsync();
            return Ok(alerts);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error retrieving security alerts");
            return StatusCode(500, "An error occurred while retrieving security alerts");
        }
    }

    [HttpPost("alerts/{alertId}/resolve")]
    public async Task<IActionResult> ResolveAlert(Guid alertId)
    {
        try
        {
            var resolvedBy = User.FindFirst("email")?.Value ?? "Unknown";
            await _securityMonitoringService.ResolveAlertAsync(alertId, resolvedBy);

            await _auditService.LogAsync(
                "SecurityAlertResolved",
                $"Security alert {alertId} resolved",
                Guid.Parse(User.FindFirst("sub")?.Value ?? Guid.Empty.ToString()),
                true,
                null,
                null,
                HttpContext.Connection.RemoteIpAddress?.ToString(),
                Request.Headers["User-Agent"].ToString());

            return Ok(new { message = "Alert resolved successfully" });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error resolving security alert {AlertId}", alertId);
            return StatusCode(500, "An error occurred while resolving the alert");
        }
    }

    [HttpGet("suspicious-ips/{ipAddress}")]
    public async Task<IActionResult> CheckSuspiciousIp(string ipAddress)
    {
        try
        {
            var isSuspicious = await _securityMonitoringService.IsIpAddressSuspiciousAsync(ipAddress);
            return Ok(new { ipAddress, isSuspicious });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error checking suspicious IP {IpAddress}", ipAddress);
            return StatusCode(500, "An error occurred while checking IP address");
        }
    }

    [HttpGet("suspicious-users/{userId}")]
    public async Task<IActionResult> CheckSuspiciousUser(string userId)
    {
        try
        {
            var isSuspicious = await _securityMonitoringService.IsUserSuspiciousAsync(userId);
            return Ok(new { userId, isSuspicious });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error checking suspicious user {UserId}", userId);
            return StatusCode(500, "An error occurred while checking user");
        }
    }

    [HttpGet("audit-logs/security")]
    public async Task<IActionResult> GetSecurityEvents(int page = 1, int pageSize = 50)
    {
        try
        {
            var securityEvents = await _auditService.GetSecurityEventsAsync(null, null, (page - 1) * pageSize, pageSize);
            return Ok(securityEvents);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error retrieving security events");
            return StatusCode(500, "An error occurred while retrieving security events");
        }
    }

    [HttpGet("dashboard")]
    public async Task<IActionResult> GetSecurityDashboard()
    {
        try
        {
            var alerts = await _securityMonitoringService.GetActiveAlertsAsync();
            var recentSecurityEvents = await _auditService.GetSecurityEventsAsync(null, null, 0, 10);

            var dashboard = new SecurityDashboard
            {
                ActiveAlertsCount = alerts.Count(),
                HighSeverityAlertsCount = alerts.Count(a => a.Severity == "High" || a.Severity == "Critical"),
                RecentSecurityEvents = recentSecurityEvents.Take(5),
                AlertsByType = alerts.GroupBy(a => a.AlertType)
                    .ToDictionary(g => g.Key, g => g.Count()),
                AlertsBySeverity = alerts.GroupBy(a => a.Severity)
                    .ToDictionary(g => g.Key, g => g.Count())
            };

            return Ok(dashboard);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error retrieving security dashboard");
            return StatusCode(500, "An error occurred while retrieving security dashboard");
        }
    }

    [HttpPost("monitor/suspicious-activity")]
    public async Task<IActionResult> ReportSuspiciousActivity([FromBody] SuspiciousActivityReport report)
    {
        try
        {
            var ipAddress = HttpContext.Connection.RemoteIpAddress?.ToString() ?? "Unknown";
            
            await _securityMonitoringService.MonitorSuspiciousActivityAsync(
                report.EventType,
                report.Details,
                report.UserId,
                ipAddress);

            return Ok(new { message = "Suspicious activity reported successfully" });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error reporting suspicious activity");
            return StatusCode(500, "An error occurred while reporting suspicious activity");
        }
    }
}

public class SuspiciousActivityReport
{
    public string EventType { get; set; } = string.Empty;
    public string Details { get; set; } = string.Empty;
    public string? UserId { get; set; }
}

public class SecurityDashboard
{
    public int ActiveAlertsCount { get; set; }
    public int HighSeverityAlertsCount { get; set; }
    public IEnumerable<object> RecentSecurityEvents { get; set; } = new List<object>();
    public Dictionary<string, int> AlertsByType { get; set; } = new();
    public Dictionary<string, int> AlertsBySeverity { get; set; } = new();
}