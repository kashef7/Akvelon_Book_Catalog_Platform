namespace App_PL.Services.LoggingServices;

public class ShutdownLoggingService
{
    public ShutdownLoggingService(IHostApplicationLifetime lifetime, ILogger<ShutdownLoggingService> logger)
    {
        lifetime.ApplicationStopping.Register(() =>
            logger.LogInformation("Application stopping: shutdown signal received, waiting for in-flight requests"));

        lifetime.ApplicationStopped.Register(() =>
            logger.LogInformation("Application stopped: shutdown complete"));
    }
}