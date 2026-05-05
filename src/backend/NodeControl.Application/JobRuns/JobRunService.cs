using System.Text.Json;
using NodeControl.Application.Audit;
using NodeControl.Application.Abstractions.Authorization;
using NodeControl.Application.Abstractions.Persistence;
using NodeControl.Application.Abstractions.Time;
using NodeControl.Application.Auth;
using NodeControl.Application.Customers;
using NodeControl.Domain.Authorization;
using NodeControl.Domain.Audit;
using NodeControl.Domain.Inventories;
using NodeControl.Domain.Jobs;
using NodeControl.Domain.Nodes;
using NodeControl.Domain.Playbooks;
using NodeControl.Domain.Secrets;
using NodeControl.Domain.VariableSets;

namespace NodeControl.Application.JobRuns;

public sealed class JobRunService(
    INodeControlDbContext dbContext,
    ICustomerAuthorizationService authorizationService,
    IClock clock,
    IAuditLogWriter auditLogWriter)
{
    public async Task<CustomerServiceResult<IReadOnlyList<JobRunDto>>> ListAsync(
        CurrentUserDto currentUser,
        Guid customerId,
        CancellationToken cancellationToken = default)
    {
        var authorization = await AuthorizeAsync(currentUser, customerId, Permission.ViewJobRuns, cancellationToken);
        if (authorization != CustomerAuthorizationResult.Allowed)
        {
            return CustomerServiceResult<IReadOnlyList<JobRunDto>>.FromAuthorization(authorization);
        }

        var jobRuns = await dbContext.ListJobRunsAsync(customerId, cancellationToken);
        return CustomerServiceResult<IReadOnlyList<JobRunDto>>.Ok(jobRuns.Select(Map).ToArray());
    }

    public async Task<CustomerServiceResult<JobRunDto>> GetAsync(
        CurrentUserDto currentUser,
        Guid customerId,
        Guid jobRunId,
        CancellationToken cancellationToken = default)
    {
        var authorization = await AuthorizeAsync(currentUser, customerId, Permission.ViewJobRuns, cancellationToken);
        if (authorization != CustomerAuthorizationResult.Allowed)
        {
            return CustomerServiceResult<JobRunDto>.FromAuthorization(authorization);
        }

        var jobRun = await dbContext.FindJobRunAsync(customerId, jobRunId, cancellationToken);
        return jobRun is null
            ? CustomerServiceResult<JobRunDto>.NotFound()
            : CustomerServiceResult<JobRunDto>.Ok(Map(jobRun));
    }

    public async Task<CustomerServiceResult<JobRunDto>> CreateManualAsync(
        CurrentUserDto currentUser,
        Guid customerId,
        Guid jobId,
        CancellationToken cancellationToken = default)
    {
        var authorization = await AuthorizeAsync(currentUser, customerId, Permission.RunJobs, cancellationToken);
        if (authorization != CustomerAuthorizationResult.Allowed)
        {
            return CustomerServiceResult<JobRunDto>.FromAuthorization(authorization);
        }

        var job = await dbContext.FindJobAsync(customerId, jobId, cancellationToken);
        if (job is null)
        {
            return CustomerServiceResult<JobRunDto>.NotFound();
        }

        if (job.Status != JobStatus.Active)
        {
            return CustomerServiceResult<JobRunDto>.BadRequest();
        }

        if (!await ReferencesAreActiveAsync(job, cancellationToken))
        {
            return CustomerServiceResult<JobRunDto>.BadRequest();
        }

        try
        {
            var jobRun = JobRun.CreateManual(job, currentUser.Id, clock.UtcNow);
            dbContext.AddJobRun(jobRun);
            await dbContext.SaveChangesAsync(cancellationToken);
            await WriteJobRunAuditAsync(
                currentUser,
                jobRun,
                "job_run.created_manual",
                $"Manual run queued for job '{job.Name}'.",
                job.Name,
                cancellationToken);

            return CustomerServiceResult<JobRunDto>.Ok(Map(jobRun));
        }
        catch (ArgumentException)
        {
            return CustomerServiceResult<JobRunDto>.BadRequest();
        }
        catch (InvalidOperationException)
        {
            return CustomerServiceResult<JobRunDto>.BadRequest();
        }
    }

    public async Task<CustomerServiceResult<JobRunDto>> CancelAsync(
        CurrentUserDto currentUser,
        Guid customerId,
        Guid jobRunId,
        CancelJobRunRequest request,
        CancellationToken cancellationToken = default)
    {
        var authorization = await AuthorizeAsync(currentUser, customerId, Permission.CancelJobRuns, cancellationToken);
        if (authorization != CustomerAuthorizationResult.Allowed)
        {
            return CustomerServiceResult<JobRunDto>.FromAuthorization(authorization);
        }

        var jobRun = await dbContext.FindJobRunAsync(customerId, jobRunId, cancellationToken);
        if (jobRun is null)
        {
            return CustomerServiceResult<JobRunDto>.NotFound();
        }

        try
        {
            var previousStatus = jobRun.Status;
            var wasCancelling = jobRun.Status == JobRunStatus.Cancelling;
            jobRun.RequestCancellation(currentUser.Id, request.Reason, clock.UtcNow);
            if (!wasCancelling)
            {
                await AppendSystemLogAsync(jobRun, JobRunLogLevel.Warning, "Cancellation requested.", cancellationToken);
            }

            await dbContext.SaveChangesAsync(cancellationToken);
            if (!wasCancelling)
            {
                var action = previousStatus == JobRunStatus.Queued
                    ? "job_run.cancelled_queued"
                    : "job_run.cancel_requested";
                var message = previousStatus == JobRunStatus.Queued
                    ? "Queued JobRun was cancelled before execution."
                    : "Cancellation was requested for a running JobRun.";
                await WriteJobRunAuditAsync(currentUser, jobRun, action, message, null, cancellationToken);
            }

            return CustomerServiceResult<JobRunDto>.Ok(Map(jobRun));
        }
        catch (ArgumentException)
        {
            return CustomerServiceResult<JobRunDto>.BadRequest();
        }
        catch (InvalidOperationException)
        {
            return CustomerServiceResult<JobRunDto>.Conflict();
        }
    }

    public async Task<CustomerServiceResult<JobRunDto>> RetryAsync(
        CurrentUserDto currentUser,
        Guid customerId,
        Guid jobRunId,
        CancellationToken cancellationToken = default)
    {
        var authorization = await AuthorizeAsync(currentUser, customerId, Permission.RetryJobRuns, cancellationToken);
        if (authorization != CustomerAuthorizationResult.Allowed)
        {
            return CustomerServiceResult<JobRunDto>.FromAuthorization(authorization);
        }

        var original = await dbContext.FindJobRunAsync(customerId, jobRunId, cancellationToken);
        if (original is null)
        {
            return CustomerServiceResult<JobRunDto>.NotFound();
        }

        var job = await dbContext.FindJobAsync(customerId, original.JobId, cancellationToken);
        if (job is null)
        {
            return CustomerServiceResult<JobRunDto>.BadRequest();
        }

        if (job.Status != JobStatus.Active || !await ReferencesAreActiveAsync(job, cancellationToken))
        {
            return CustomerServiceResult<JobRunDto>.BadRequest();
        }

        try
        {
            var retry = JobRun.CreateRetry(original, job, currentUser.Id, clock.UtcNow);
            dbContext.AddJobRun(retry);
            await dbContext.SaveChangesAsync(cancellationToken);
            await WriteJobRunAuditAsync(
                currentUser,
                retry,
                "job_run.retried",
                $"Retry run queued for job '{job.Name}'.",
                job.Name,
                cancellationToken,
                original.Id);
            return CustomerServiceResult<JobRunDto>.Ok(Map(retry));
        }
        catch (ArgumentException)
        {
            return CustomerServiceResult<JobRunDto>.BadRequest();
        }
        catch (InvalidOperationException)
        {
            return CustomerServiceResult<JobRunDto>.Conflict();
        }
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

        if (!await ManagedNodeSecretReferencesAreValidAsync(job.CustomerId, job.InventoryGroupId, cancellationToken))
        {
            return false;
        }

        return true;
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

    private async Task<CustomerAuthorizationResult> AuthorizeAsync(
        CurrentUserDto currentUser,
        Guid customerId,
        Permission permission,
        CancellationToken cancellationToken)
    {
        return await authorizationService.AuthorizeAsync(currentUser, customerId, permission, cancellationToken);
    }

    private async Task AppendSystemLogAsync(
        JobRun jobRun,
        JobRunLogLevel level,
        string message,
        CancellationToken cancellationToken)
    {
        var sequence = await dbContext.GetNextJobRunLogSequenceAsync(jobRun.Id, cancellationToken);
        dbContext.AddJobRunLogEntry(JobRunLogEntry.Create(
            jobRun,
            sequence,
            clock.UtcNow,
            JobRunLogStream.System,
            level,
            message));
    }

    private async Task WriteJobRunAuditAsync(
        CurrentUserDto currentUser,
        JobRun jobRun,
        string action,
        string message,
        string? jobName,
        CancellationToken cancellationToken,
        Guid? originalJobRunId = null)
    {
        await auditLogWriter.WriteAsync(new AuditLogWriteRequest(
            jobRun.CustomerId,
            currentUser.Id,
            currentUser.DisplayName,
            AuditActorType.User,
            action,
            "JobRun",
            jobRun.Id,
            $"Run {jobRun.Id}",
            AuditOutcome.Succeeded,
            message,
            JsonSerializer.Serialize(new
            {
                jobRunId = jobRun.Id,
                jobId = jobRun.JobId,
                controlNodeId = jobRun.ControlNodeId,
                jobName,
                status = jobRun.Status.ToString(),
                triggerType = jobRun.TriggerType.ToString(),
                originalJobRunId
            })),
            cancellationToken);
    }

    private static JobRunDto Map(JobRun jobRun)
    {
        return new JobRunDto(
            jobRun.Id,
            jobRun.CustomerId,
            jobRun.JobId,
            jobRun.ControlNodeId,
            jobRun.TriggerType,
            jobRun.TriggeredByUserId,
            jobRun.ScheduleId,
            jobRun.RetriedFromJobRunId,
            jobRun.RetryAttempt,
            jobRun.Status,
            jobRun.QueuedAt,
            jobRun.StartedAt,
            jobRun.FinishedAt,
            jobRun.ExitCode,
            JobRunLogRedactor.Redact(jobRun.ErrorMessage),
            jobRun.WorkspacePath,
            jobRun.StdoutLogPath,
            jobRun.StderrLogPath,
            jobRun.CancellationRequestedAtUtc,
            jobRun.CancellationRequestedByUserId,
            JobRunLogRedactor.Redact(jobRun.CancellationReason),
            jobRun.CreatedAt);
    }
}
