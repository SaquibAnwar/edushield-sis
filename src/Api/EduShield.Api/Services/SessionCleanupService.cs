using EduShield.Core.Interfaces;

namespace EduShield.Api.Services;

public class SessionCleanupService : BackgroundService
{
    private readonly IServiceProvider _serviceProvider;
    private readonly ILogger<SessionCleanupService> _logger;
    private readonly TimeSpan _cleanupInterval = TimeSpan.FromHours(1); // Run cleanup every hour

    public SessionCleanupService(IServiceProvider serviceProvider, ILogger<SessionCleanupService> logger)
    {
        _serviceProvider = serviceProvider;
        _logger = logger;
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        while (!stoppingToken.IsCancellationRequested)
        {
            try
            {
                using var scope = _serviceProvider.CreateScope();
                var sessionService = scope.ServiceProvider.GetRequiredService<ISessionService>();
                
                await sessionService.CleanupExpiredSessionsAsync(stoppingToken);
                
                _logger.LogDebug("Session cleanup completed successfully");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error occurred during session cleanup");
            }

            await Task.Delay(_cleanupInterval, stoppingToken);
        }
    }
}