using NodeControl.Application.JobRuns;

namespace NodeControl.Worker.JobRuns;

public sealed class QueuedJobRunWorker(
    IServiceScopeFactory serviceScopeFactory,
    ILogger<QueuedJobRunWorker> logger) : BackgroundService
{
    private static readonly TimeSpan PollInterval = TimeSpan.FromSeconds(5);

    public async Task<bool> ExecuteOnceAsync(CancellationToken cancellationToken = default)
    {
        using var scope = serviceScopeFactory.CreateScope();
        var executionService = scope.ServiceProvider.GetRequiredService<JobRunExecutionService>();
        return await executionService.ProcessOldestQueuedAsync(cancellationToken);
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        while (!stoppingToken.IsCancellationRequested)
        {
            try
            {
                await ExecuteOnceAsync(stoppingToken);
            }
            catch (OperationCanceledException) when (stoppingToken.IsCancellationRequested)
            {
                throw;
            }
            catch (Exception exception)
            {
                logger.LogError(exception, "Queued JobRun processing failed.");
            }

            await Task.Delay(PollInterval, stoppingToken);
        }
    }
}
