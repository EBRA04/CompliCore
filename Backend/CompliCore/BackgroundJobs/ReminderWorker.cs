namespace CompliCore.BackgroundJobs;

public class ReminderWorker : BackgroundService
{
    private readonly IServiceScopeFactory _scopeFactory;
    private readonly IConfiguration _config;
    private readonly ILogger<ReminderWorker> _logger;

    public ReminderWorker(IServiceScopeFactory scopeFactory, IConfiguration config, ILogger<ReminderWorker> logger)
    {
        _scopeFactory = scopeFactory;
        _config = config;
        _logger = logger;
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        if (!_config.GetValue("Reminders:Enabled", true)) return;

        await RunSafe(stoppingToken);

        using var timer = new PeriodicTimer(TimeSpan.FromHours(24));
        while (await timer.WaitForNextTickAsync(stoppingToken))
        {
            await RunSafe(stoppingToken);
        }
    }

    private async Task RunSafe(CancellationToken ct)
    {
        try
        {
            using var scope = _scopeFactory.CreateScope();
            var reminderService = scope.ServiceProvider.GetRequiredService<CompliCore.Services.ReminderService>();
            var count = await reminderService.RunOnceAsync(ct);
            _logger.LogInformation("Reminder run created {Count} notifications", count);
        }
        catch (Exception ex) when (ex is not OperationCanceledException)
        {
            _logger.LogError(ex, "Reminder run failed");
        }
    }
}