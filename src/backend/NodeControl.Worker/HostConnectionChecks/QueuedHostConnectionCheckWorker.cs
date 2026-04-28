using NodeControl.Application.HostConnectionChecks;

namespace NodeControl.Worker.HostConnectionChecks;

public sealed class QueuedHostConnectionCheckWorker(
    IServiceScopeFactory serviceScopeFactory,
    ILogger<QueuedHostConnectionCheckWorker> logger) : BackgroundService
{
    private static readonly TimeSpan PollInterval = TimeSpan.FromSeconds(5);

    public async Task<bool> ExecuteOnceAsync(CancellationToken cancellationToken = default)
    {
        using var scope = serviceScopeFactory.CreateScope();
        var processor = scope.ServiceProvider.GetRequiredService<HostConnectionCheckProcessor>();
        return await processor.ProcessOldestQueuedAsync(cancellationToken);
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
                logger.LogError(exception, "Queued host connection check processing failed.");
            }

            await Task.Delay(PollInterval, stoppingToken);
        }
    }
}
