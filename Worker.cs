using EbarimtCheckerService.Services;

public class Worker : BackgroundService
{
    private readonly IServiceScopeFactory _scopeFactory;
    private readonly ILogger<Worker> _logger;

    public Worker(IServiceScopeFactory scopeFactory, ILogger<Worker> logger)
    {
        _scopeFactory = scopeFactory;
        _logger = logger;
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        while (!stoppingToken.IsCancellationRequested)
        {
            using (var scope = _scopeFactory.CreateScope())
            {
                var monitor = scope.ServiceProvider.GetRequiredService<ApiMonitorService>();
                var sync = scope.ServiceProvider.GetRequiredService<DataSyncService>();

                _logger.LogInformation("✅ Starting check + sync...");

                await monitor.CheckHealthAsync();
                await sync.FetchAndSaveNewDataAsync();
            }

            await Task.Delay(TimeSpan.FromMinutes(10), stoppingToken);
        }
    }
}

