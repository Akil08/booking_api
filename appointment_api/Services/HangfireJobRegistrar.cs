using Hangfire;

namespace appointment_api.Services;

public class HangfireJobRegistrar : IHostedService
{
    public Task StartAsync(CancellationToken cancellationToken)
    {
        // Register recurring job safely (no circular dependency)
        RecurringJob.AddOrUpdate<DailyJobService>(
            "daily-reset",
            service => service.RunDailyResetAsync(),
            "0 3 * * *",
            timeZone: TimeZoneInfo.Utc
        );

        return Task.CompletedTask;
    }

    public Task StopAsync(CancellationToken cancellationToken)
    {
        return Task.CompletedTask;
    }
}