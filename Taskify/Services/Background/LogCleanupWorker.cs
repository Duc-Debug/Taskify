
namespace Taskify.Services.Background
{
    public class LogCleanupWorker : BackgroundService
    {
        private readonly IServiceScopeFactory _serviceScopeFactory;
        private readonly ILogger<LogCleanupWorker> _logger;
        public LogCleanupWorker(IServiceScopeFactory serviceScopeFactory, ILogger<LogCleanupWorker> logger)
        {
            _serviceScopeFactory = serviceScopeFactory;
            _logger = logger;
        }

        protected override async Task ExecuteAsync(CancellationToken stoppingToken)
        {
            _logger.LogInformation("Log Cleanup Worker started");
            while (!stoppingToken.IsCancellationRequested)
            {
                try
                {
                    _logger.LogInformation("Cleaning up old Logs....");
                    using (var scope = _serviceScopeFactory.CreateScope())
                    {
                        var logService = scope.ServiceProvider.GetRequiredService<IActivityLogService>();
                        await logService.CleanupOldLogsASync();
                    }
                    _logger.LogInformation("Cleanuo finished");
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "Error occurred while cleaning logs.");
                }
                await Task.Delay(TimeSpan.FromHours(24), stoppingToken);
            }
        }
    }
}
