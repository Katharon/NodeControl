using System.Text.Json;
using NodeControl.Application.Audit;
using NodeControl.Application.Abstractions.Persistence;
using NodeControl.Application.Abstractions.Time;
using NodeControl.Domain.Audit;
using NodeControl.Domain.Jobs;
using NodeControl.Domain.Nodes;
using NodeControl.Domain.Playbooks;
using NodeControl.Domain.Secrets;
using NodeControl.Domain.VariableSets;

namespace NodeControl.Application.Schedules;

public sealed class ScheduledJobRunService(
    INodeControlDbContext dbContext,
    ICronScheduleCalculator cronScheduleCalculator,
    IClock clock,
    IAuditLogWriter auditLogWriter)
{
    private const int DueScheduleBatchSize = 50;

    public async Task<int> EnqueueDueSchedulesAsync(CancellationToken cancellationToken = default)
    {
        var now = clock.UtcNow;
        var dueSchedules = await dbContext.ListDueActiveJobSchedulesAsync(
            now,
            DueScheduleBatchSize,
            cancellationToken);

        var enqueuedCount = 0;
        foreach (var schedule in dueSchedules)
        {
            if (schedule.Status != JobScheduleStatus.Active || schedule.NextRunAtUtc is null || schedule.NextRunAtUtc > now)
            {
                continue;
            }

            var job = await dbContext.FindJobAsync(schedule.CustomerId, schedule.JobId, cancellationToken);
            if (job?.Status != JobStatus.Active || !await ReferencesAreActiveAsync(job, cancellationToken))
            {
                continue;
            }

            var jobRun = JobRun.CreateScheduled(job, schedule, now);
            dbContext.AddJobRun(jobRun);

            var nextRunAtUtc = cronScheduleCalculator.GetNextRunUtc(
                schedule.CronExpression,
                schedule.TimeZoneId,
                now);

            schedule.MarkTriggered(jobRun, now, nextRunAtUtc);
            await auditLogWriter.WriteAsync(new AuditLogWriteRequest(
                schedule.CustomerId,
                null,
                null,
                AuditActorType.Scheduler,
                "job_run.created_scheduled",
                "JobRun",
                jobRun.Id,
                $"Run {jobRun.Id}",
                AuditOutcome.Succeeded,
                $"Scheduled run queued from schedule '{schedule.Name}'.",
                JsonSerializer.Serialize(new
                {
                    jobRunId = jobRun.Id,
                    jobId = job.Id,
                    controlNodeId = jobRun.ControlNodeId,
                    jobName = job.Name,
                    scheduleId = schedule.Id,
                    scheduleSlug = schedule.Slug,
                    triggerType = jobRun.TriggerType.ToString()
                })),
                cancellationToken);
            enqueuedCount++;
        }

        if (enqueuedCount > 0)
        {
            await dbContext.SaveChangesAsync(cancellationToken);
        }

        return enqueuedCount;
    }

    private async Task<bool> ReferencesAreActiveAsync(Job job, CancellationToken cancellationToken)
    {
        var controlNode = await dbContext.FindControlNodeAsync(job.CustomerId, job.ControlNodeId, cancellationToken);
        if (controlNode?.Status != ControlNodeStatus.Active)
        {
            return false;
        }

        if (!await SshPrivateKeySecretReferenceIsValidAsync(job.CustomerId, controlNode.SshPrivateKeySecretId, cancellationToken))
        {
            return false;
        }

        var inventoryGroup = await dbContext.FindInventoryGroupAsync(job.CustomerId, job.InventoryGroupId, cancellationToken);
        if (inventoryGroup is null || inventoryGroup.IsArchived)
        {
            return false;
        }

        var playbook = await dbContext.FindPlaybookAsync(job.CustomerId, job.PlaybookId, cancellationToken);
        if (playbook?.Status != PlaybookStatus.Active)
        {
            return false;
        }

        if (job.VariableSetId is not null)
        {
            var variableSet = await dbContext.FindVariableSetAsync(job.CustomerId, job.VariableSetId.Value, cancellationToken);
            if (variableSet?.Status != VariableSetStatus.Active)
            {
                return false;
            }
        }

        return await ManagedNodeSecretReferencesAreValidAsync(job.CustomerId, job.InventoryGroupId, cancellationToken);
    }

    private async Task<bool> ManagedNodeSecretReferencesAreValidAsync(
        Guid customerId,
        Guid inventoryGroupId,
        CancellationToken cancellationToken)
    {
        var managedNodes = await dbContext.ListActiveManagedNodesForInventoryGroupAsync(inventoryGroupId, cancellationToken);
        foreach (var managedNode in managedNodes)
        {
            if (!await SshPrivateKeySecretReferenceIsValidAsync(customerId, managedNode.SshPrivateKeySecretId, cancellationToken))
            {
                return false;
            }
        }

        var activeCustomerManagedNodes = await dbContext.ListActiveManagedNodesAsync(customerId, cancellationToken);
        foreach (var jumpHost in managedNodes
            .Where(managedNode => managedNode.JumpHostManagedNodeId is not null)
            .Select(managedNode => activeCustomerManagedNodes.FirstOrDefault(candidate => candidate.Id == managedNode.JumpHostManagedNodeId!.Value)))
        {
            if (jumpHost is null || !await SshPrivateKeySecretReferenceIsValidAsync(customerId, jumpHost.SshPrivateKeySecretId, cancellationToken))
            {
                return false;
            }
        }

        return true;
    }

    private async Task<bool> SshPrivateKeySecretReferenceIsValidAsync(
        Guid customerId,
        Guid? secretId,
        CancellationToken cancellationToken)
    {
        if (secretId is null)
        {
            return true;
        }

        var secret = await dbContext.FindSecretAsync(customerId, secretId.Value, cancellationToken);
        return secret is not null
            && secret.CustomerId == customerId
            && secret.Status == SecretStatus.Active
            && secret.Kind == SecretKind.SshPrivateKey;
    }
}
