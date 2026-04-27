using NodeControl.Application.Abstractions.Authorization;
using NodeControl.Application.Abstractions.Persistence;
using NodeControl.Application.Abstractions.Time;
using NodeControl.Application.Auth;
using NodeControl.Application.Customers;
using NodeControl.Domain.Authorization;
using NodeControl.Domain.Inventories;
using NodeControl.Domain.Jobs;
using NodeControl.Domain.Nodes;
using NodeControl.Domain.Playbooks;
using NodeControl.Domain.VariableSets;

namespace NodeControl.Application.JobRuns;

public sealed class JobRunService(
    INodeControlDbContext dbContext,
    ICustomerAuthorizationService authorizationService,
    IClock clock)
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

    private async Task<bool> ReferencesAreActiveAsync(Job job, CancellationToken cancellationToken)
    {
        var controlNode = await dbContext.FindControlNodeAsync(job.CustomerId, job.ControlNodeId, cancellationToken);
        if (controlNode?.Status != ControlNodeStatus.Active)
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

        return true;
    }

    private async Task<CustomerAuthorizationResult> AuthorizeAsync(
        CurrentUserDto currentUser,
        Guid customerId,
        Permission permission,
        CancellationToken cancellationToken)
    {
        return await authorizationService.AuthorizeAsync(currentUser, customerId, permission, cancellationToken);
    }

    private static JobRunDto Map(JobRun jobRun)
    {
        return new JobRunDto(
            jobRun.Id,
            jobRun.CustomerId,
            jobRun.JobId,
            jobRun.TriggerType,
            jobRun.TriggeredByUserId,
            jobRun.ScheduleId,
            jobRun.Status,
            jobRun.QueuedAt,
            jobRun.StartedAt,
            jobRun.FinishedAt,
            jobRun.ExitCode,
            jobRun.ErrorMessage,
            jobRun.WorkspacePath,
            jobRun.StdoutLogPath,
            jobRun.StderrLogPath,
            jobRun.CreatedAt);
    }
}
