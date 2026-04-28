using System.Text.Json;
using NodeControl.Application.Audit;
using NodeControl.Application.Abstractions.HostConnectivity;
using NodeControl.Application.Abstractions.Persistence;
using NodeControl.Application.Abstractions.Time;
using NodeControl.Domain.Audit;
using NodeControl.Domain.Nodes;

namespace NodeControl.Application.HostConnectionChecks;

public sealed class HostConnectionCheckProcessor(
    INodeControlDbContext dbContext,
    IHostConnectivityChecker connectivityChecker,
    IClock clock,
    IAuditLogWriter auditLogWriter)
{
    private static readonly TimeSpan TcpTimeout = TimeSpan.FromSeconds(5);

    public async Task<bool> ProcessOldestQueuedAsync(CancellationToken cancellationToken = default)
    {
        var check = await dbContext.FindOldestQueuedHostConnectionCheckAsync(cancellationToken);
        if (check is null)
        {
            return false;
        }

        await ProcessAsync(check, cancellationToken);
        return true;
    }

    public async Task ProcessAsync(HostConnectionCheck check, CancellationToken cancellationToken = default)
    {
        var currentStatus = await dbContext.GetHostConnectionCheckStatusAsync(check.Id, cancellationToken);
        if (check.Status != HostConnectionCheckStatus.Queued || currentStatus != HostConnectionCheckStatus.Queued)
        {
            return;
        }

        check.MarkRunning(clock.UtcNow);
        await dbContext.SaveChangesAsync(cancellationToken);

        try
        {
            var result = await connectivityChecker.CheckTcpAsync(
                check.Hostname,
                check.Port,
                TcpTimeout,
                cancellationToken);

            if (result.Succeeded)
            {
                check.MarkSucceeded(SafeMessage(result.Message, 2000), clock.UtcNow);
            }
            else if (result.TimedOut)
            {
                check.MarkTimedOut(SafeMessage(result.Message, 4000), clock.UtcNow);
            }
            else
            {
                check.MarkFailed(SafeMessage(result.Message, 4000), clock.UtcNow);
            }

            await dbContext.SaveChangesAsync(cancellationToken);
            await WriteCompletedAuditAsync(check, cancellationToken);
        }
        catch (OperationCanceledException) when (cancellationToken.IsCancellationRequested)
        {
            throw;
        }
        catch (Exception exception)
        {
            check.MarkFailed(SafeMessage($"Connection check failed: {exception.Message}", 4000), clock.UtcNow);
            await dbContext.SaveChangesAsync(CancellationToken.None);
            await WriteCompletedAuditAsync(check, CancellationToken.None);
        }
    }

    private static string SafeMessage(string message, int maxLength)
    {
        if (string.IsNullOrWhiteSpace(message))
        {
            return "No connection check details were returned.";
        }

        var trimmed = message.Trim();
        return trimmed.Length <= maxLength
            ? trimmed
            : trimmed[..maxLength];
    }

    private async Task WriteCompletedAuditAsync(
        HostConnectionCheck check,
        CancellationToken cancellationToken)
    {
        await auditLogWriter.WriteAsync(new AuditLogWriteRequest(
            check.CustomerId,
            null,
            null,
            AuditActorType.Worker,
            "host_connection_check.completed",
            "HostConnectionCheck",
            check.Id,
            $"{check.TargetType} {check.Hostname}:{check.Port}",
            check.Status == HostConnectionCheckStatus.Succeeded ? AuditOutcome.Succeeded : AuditOutcome.Failed,
            $"Connection check finished with status {check.Status}.",
            JsonSerializer.Serialize(new
            {
                checkId = check.Id,
                check.TargetType,
                check.ControlNodeId,
                check.ManagedNodeId,
                check.Hostname,
                check.Port,
                check.Status,
                check.DurationMs
            })),
            cancellationToken);
    }
}
