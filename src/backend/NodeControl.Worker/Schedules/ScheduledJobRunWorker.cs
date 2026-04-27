using NodeControl.Application.Schedules;

namespace NodeControl.Worker.Schedules;

public sealed class ScheduledJobRunWorker(
    IServiceScopeFactory serviceScopeFactory,
    ILogger<ScheduledJobRunWorker> logger) : BackgroundService
{
    private static readonly TimeSpan PollInterval = TimeSpan.FromSeconds(30);

    public async Task<int> ExecuteOnceAsync(CancellationToken cancellationToken = default)
    {
        using var scope = serviceScopeFactory.CreateScope();
        var scheduleService = scope.ServiceProvider.GetRequiredService<ScheduledJobRunService>();
        return await scheduleService.EnqueueDueSchedulesAsync(cancellationToken);
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        while (!stoppingToken.IsCancellationRequested)
        {
            try
            {
                var enqueuedCount = await ExecuteOnceAsync(stoppingToken);
                if (enqueuedCount > 0)
                {
                    logger.LogInformation("Enqueued {JobRunCount} scheduled JobRuns.", enqueuedCount);
                }
            }
            catch (OperationCanceledException) when (stoppingToken.IsCancellationRequested)
            {
                throw;
            }
            catch (Exception exception)
            {
                logger.LogError(exception, "Scheduled JobRun polling failed.");
            }

            await Task.Delay(PollInterval, stoppingToken);
        }
    }
}
