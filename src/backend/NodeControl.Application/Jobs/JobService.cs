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
using NodeControl.Domain.VariableSets;

namespace NodeControl.Application.Jobs;

public sealed class JobService(
    INodeControlDbContext dbContext,
    ICustomerAuthorizationService authorizationService,
    IClock clock,
    IAuditLogWriter auditLogWriter)
{
    public async Task<CustomerServiceResult<IReadOnlyList<JobDto>>> ListAsync(
        CurrentUserDto currentUser,
        Guid customerId,
        CancellationToken cancellationToken = default)
    {
        var authorization = await AuthorizeAsync(currentUser, customerId, Permission.ViewPlaybooks, cancellationToken);
        if (authorization != CustomerAuthorizationResult.Allowed)
        {
            return CustomerServiceResult<IReadOnlyList<JobDto>>.FromAuthorization(authorization);
        }

        var jobs = await dbContext.ListActiveJobsAsync(customerId, cancellationToken);
        return CustomerServiceResult<IReadOnlyList<JobDto>>.Ok(jobs.Select(Map).ToArray());
    }

    public async Task<CustomerServiceResult<JobDto>> GetAsync(
        CurrentUserDto currentUser,
        Guid customerId,
        Guid jobId,
        CancellationToken cancellationToken = default)
    {
        var authorization = await AuthorizeAsync(currentUser, customerId, Permission.ViewPlaybooks, cancellationToken);
        if (authorization != CustomerAuthorizationResult.Allowed)
        {
            return CustomerServiceResult<JobDto>.FromAuthorization(authorization);
        }

        var job = await dbContext.FindJobAsync(customerId, jobId, cancellationToken);
        return job is null
            ? CustomerServiceResult<JobDto>.NotFound()
            : CustomerServiceResult<JobDto>.Ok(Map(job));
    }

    public async Task<CustomerServiceResult<JobDto>> CreateAsync(
        CurrentUserDto currentUser,
        Guid customerId,
        CreateJobRequest request,
        CancellationToken cancellationToken = default)
    {
        var authorization = await AuthorizeAsync(currentUser, customerId, Permission.ManagePlaybooks, cancellationToken);
        if (authorization != CustomerAuthorizationResult.Allowed)
        {
            return CustomerServiceResult<JobDto>.FromAuthorization(authorization);
        }

        if (await dbContext.FindJobBySlugAsync(customerId, request.Slug.Trim(), cancellationToken) is not null)
        {
            return CustomerServiceResult<JobDto>.Conflict();
        }

        if (!await ReferencesAreActiveAsync(customerId, request.ControlNodeId, request.InventoryGroupId, request.PlaybookId, request.VariableSetId, cancellationToken))
        {
            return CustomerServiceResult<JobDto>.BadRequest();
        }

        try
        {
            var job = Job.Create(
                customerId,
                request.Name,
                request.Slug,
                request.Description,
                request.ControlNodeId,
                request.InventoryGroupId,
                request.PlaybookId,
                request.VariableSetId,
                request.DefaultTimeoutSeconds,
                clock.UtcNow);

            dbContext.AddJob(job);
            await dbContext.SaveChangesAsync(cancellationToken);
            await WriteJobAuditAsync(currentUser, job, "job.created", $"Job '{job.Name}' was created.", cancellationToken);

            return CustomerServiceResult<JobDto>.Ok(Map(job));
        }
        catch (ArgumentException)
        {
            return CustomerServiceResult<JobDto>.BadRequest();
        }
    }

    public async Task<CustomerServiceResult<JobDto>> UpdateAsync(
        CurrentUserDto currentUser,
        Guid customerId,
        Guid jobId,
        UpdateJobRequest request,
        CancellationToken cancellationToken = default)
    {
        var authorization = await AuthorizeAsync(currentUser, customerId, Permission.ManagePlaybooks, cancellationToken);
        if (authorization != CustomerAuthorizationResult.Allowed)
        {
            return CustomerServiceResult<JobDto>.FromAuthorization(authorization);
        }

        var job = await dbContext.FindJobAsync(customerId, jobId, cancellationToken);
        if (job is null)
        {
            return CustomerServiceResult<JobDto>.NotFound();
        }

        var existing = await dbContext.FindJobBySlugAsync(customerId, request.Slug.Trim(), cancellationToken);
        if (existing is not null && existing.Id != jobId)
        {
            return CustomerServiceResult<JobDto>.Conflict();
        }

        if (!await ReferencesAreActiveAsync(customerId, request.ControlNodeId, request.InventoryGroupId, request.PlaybookId, request.VariableSetId, cancellationToken))
        {
            return CustomerServiceResult<JobDto>.BadRequest();
        }

        try
        {
            job.Update(
                request.Name,
                request.Slug,
                request.Description,
                request.ControlNodeId,
                request.InventoryGroupId,
                request.PlaybookId,
                request.VariableSetId,
                request.DefaultTimeoutSeconds,
                clock.UtcNow);
            await dbContext.SaveChangesAsync(cancellationToken);
            await WriteJobAuditAsync(currentUser, job, "job.updated", $"Job '{job.Name}' was updated.", cancellationToken);

            return CustomerServiceResult<JobDto>.Ok(Map(job));
        }
        catch (ArgumentException)
        {
            return CustomerServiceResult<JobDto>.BadRequest();
        }
    }

    public async Task<CustomerServiceResult<JobDto>> ArchiveAsync(
        CurrentUserDto currentUser,
        Guid customerId,
        Guid jobId,
        CancellationToken cancellationToken = default)
    {
        var authorization = await AuthorizeAsync(currentUser, customerId, Permission.ManagePlaybooks, cancellationToken);
        if (authorization != CustomerAuthorizationResult.Allowed)
        {
            return CustomerServiceResult<JobDto>.FromAuthorization(authorization);
        }

        var job = await dbContext.FindJobAsync(customerId, jobId, cancellationToken);
        if (job is null)
        {
            return CustomerServiceResult<JobDto>.NotFound();
        }

        job.Archive(clock.UtcNow);
        await dbContext.SaveChangesAsync(cancellationToken);
        await WriteJobAuditAsync(currentUser, job, "job.archived", $"Job '{job.Name}' was archived.", cancellationToken);

        return CustomerServiceResult<JobDto>.Ok(Map(job));
    }

    private async Task WriteJobAuditAsync(
        CurrentUserDto currentUser,
        Job job,
        string action,
        string message,
        CancellationToken cancellationToken)
    {
        await auditLogWriter.WriteAsync(new AuditLogWriteRequest(
            job.CustomerId,
            currentUser.Id,
            currentUser.DisplayName,
            AuditActorType.User,
            action,
            "Job",
            job.Id,
            job.Name,
            AuditOutcome.Succeeded,
            message,
            JsonSerializer.Serialize(new
            {
                jobId = job.Id,
                jobSlug = job.Slug,
                jobStatus = job.Status.ToString()
            })),
            cancellationToken);
    }

    private async Task<bool> ReferencesAreActiveAsync(
        Guid customerId,
        Guid controlNodeId,
        Guid inventoryGroupId,
        Guid playbookId,
        Guid? variableSetId,
        CancellationToken cancellationToken)
    {
        var controlNode = await dbContext.FindControlNodeAsync(customerId, controlNodeId, cancellationToken);
        if (controlNode?.Status != ControlNodeStatus.Active)
        {
            return false;
        }

        var inventoryGroup = await dbContext.FindInventoryGroupAsync(customerId, inventoryGroupId, cancellationToken);
        if (inventoryGroup is null || inventoryGroup.IsArchived)
        {
            return false;
        }

        var playbook = await dbContext.FindPlaybookAsync(customerId, playbookId, cancellationToken);
        if (playbook?.Status != PlaybookStatus.Active)
        {
            return false;
        }

        if (variableSetId is not null)
        {
            var variableSet = await dbContext.FindVariableSetAsync(customerId, variableSetId.Value, cancellationToken);
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

    private static JobDto Map(Job job)
    {
        return new JobDto(
            job.Id,
            job.CustomerId,
            job.Name,
            job.Slug,
            job.Description,
            job.ControlNodeId,
            job.InventoryGroupId,
            job.PlaybookId,
            job.VariableSetId,
            job.Status,
            job.DefaultTimeoutSeconds,
            job.CreatedAt,
            job.UpdatedAt,
            job.ArchivedAt);
    }
}
