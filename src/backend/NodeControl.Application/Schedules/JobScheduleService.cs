using System.Text.Json;
using NodeControl.Application.Audit;
using NodeControl.Application.Abstractions.Authorization;
using NodeControl.Application.Abstractions.Persistence;
using NodeControl.Application.Abstractions.Time;
using NodeControl.Application.Auth;
using NodeControl.Application.Customers;
using NodeControl.Domain.Authorization;
using NodeControl.Domain.Audit;
using NodeControl.Domain.Jobs;

namespace NodeControl.Application.Schedules;

public sealed class JobScheduleService(
    INodeControlDbContext dbContext,
    ICustomerAuthorizationService authorizationService,
    ICronScheduleCalculator cronScheduleCalculator,
    IClock clock,
    IAuditLogWriter auditLogWriter)
{
    private const string DefaultTimeZoneId = "UTC";

    public async Task<CustomerServiceResult<IReadOnlyList<JobScheduleDto>>> ListAsync(
        CurrentUserDto currentUser,
        Guid customerId,
        CancellationToken cancellationToken = default)
    {
        var authorization = await AuthorizeAsync(currentUser, customerId, Permission.ViewSchedules, cancellationToken);
        if (authorization != CustomerAuthorizationResult.Allowed)
        {
            return CustomerServiceResult<IReadOnlyList<JobScheduleDto>>.FromAuthorization(authorization);
        }

        var schedules = await dbContext.ListJobSchedulesAsync(customerId, cancellationToken);
        return CustomerServiceResult<IReadOnlyList<JobScheduleDto>>.Ok(schedules.Select(Map).ToArray());
    }

    public async Task<CustomerServiceResult<JobScheduleDto>> GetAsync(
        CurrentUserDto currentUser,
        Guid customerId,
        Guid scheduleId,
        CancellationToken cancellationToken = default)
    {
        var authorization = await AuthorizeAsync(currentUser, customerId, Permission.ViewSchedules, cancellationToken);
        if (authorization != CustomerAuthorizationResult.Allowed)
        {
            return CustomerServiceResult<JobScheduleDto>.FromAuthorization(authorization);
        }

        var schedule = await dbContext.FindJobScheduleAsync(customerId, scheduleId, cancellationToken);
        return schedule is null
            ? CustomerServiceResult<JobScheduleDto>.NotFound()
            : CustomerServiceResult<JobScheduleDto>.Ok(Map(schedule));
    }

    public async Task<CustomerServiceResult<JobScheduleDto>> CreateAsync(
        CurrentUserDto currentUser,
        Guid customerId,
        CreateJobScheduleRequest request,
        CancellationToken cancellationToken = default)
    {
        var authorization = await AuthorizeAsync(currentUser, customerId, Permission.ManageSchedules, cancellationToken);
        if (authorization != CustomerAuthorizationResult.Allowed)
        {
            return CustomerServiceResult<JobScheduleDto>.FromAuthorization(authorization);
        }

        if (await dbContext.FindJobScheduleBySlugAsync(customerId, request.Slug.Trim(), cancellationToken) is not null)
        {
            return CustomerServiceResult<JobScheduleDto>.Conflict();
        }

        var job = await dbContext.FindJobAsync(customerId, request.JobId, cancellationToken);
        if (job?.Status != JobStatus.Active)
        {
            return CustomerServiceResult<JobScheduleDto>.BadRequest();
        }

        var timeZoneId = NormalizeTimeZone(request.TimeZoneId);
        var nextRunAtUtc = ValidateAndGetNextRun(request.CronExpression, timeZoneId);
        if (nextRunAtUtc is null)
        {
            return CustomerServiceResult<JobScheduleDto>.BadRequest();
        }

        try
        {
            var schedule = JobSchedule.Create(
                job,
                request.Name,
                request.Slug,
                request.Description,
                request.CronExpression,
                timeZoneId,
                nextRunAtUtc,
                clock.UtcNow);

            dbContext.AddJobSchedule(schedule);
            await dbContext.SaveChangesAsync(cancellationToken);
            await WriteScheduleAuditAsync(
                currentUser,
                schedule,
                "schedule.created",
                $"Schedule '{schedule.Name}' was created.",
                cancellationToken);

            return CustomerServiceResult<JobScheduleDto>.Ok(Map(schedule));
        }
        catch (ArgumentException)
        {
            return CustomerServiceResult<JobScheduleDto>.BadRequest();
        }
        catch (InvalidOperationException)
        {
            return CustomerServiceResult<JobScheduleDto>.BadRequest();
        }
    }

    public async Task<CustomerServiceResult<JobScheduleDto>> UpdateAsync(
        CurrentUserDto currentUser,
        Guid customerId,
        Guid scheduleId,
        UpdateJobScheduleRequest request,
        CancellationToken cancellationToken = default)
    {
        var authorization = await AuthorizeAsync(currentUser, customerId, Permission.ManageSchedules, cancellationToken);
        if (authorization != CustomerAuthorizationResult.Allowed)
        {
            return CustomerServiceResult<JobScheduleDto>.FromAuthorization(authorization);
        }

        var schedule = await dbContext.FindJobScheduleAsync(customerId, scheduleId, cancellationToken);
        if (schedule is null)
        {
            return CustomerServiceResult<JobScheduleDto>.NotFound();
        }

        var existing = await dbContext.FindJobScheduleBySlugAsync(customerId, request.Slug.Trim(), cancellationToken);
        if (existing is not null && existing.Id != scheduleId)
        {
            return CustomerServiceResult<JobScheduleDto>.Conflict();
        }

        var job = await dbContext.FindJobAsync(customerId, request.JobId, cancellationToken);
        if (job?.Status != JobStatus.Active)
        {
            return CustomerServiceResult<JobScheduleDto>.BadRequest();
        }

        var timeZoneId = NormalizeTimeZone(request.TimeZoneId);
        DateTimeOffset? nextRunAtUtc = null;
        if (schedule.Status == JobScheduleStatus.Active)
        {
            nextRunAtUtc = ValidateAndGetNextRun(request.CronExpression, timeZoneId);
            if (nextRunAtUtc is null)
            {
                return CustomerServiceResult<JobScheduleDto>.BadRequest();
            }
        }
        else if (!IsValidScheduleDefinition(request.CronExpression, timeZoneId))
        {
            return CustomerServiceResult<JobScheduleDto>.BadRequest();
        }

        try
        {
            schedule.Update(
                job,
                request.Name,
                request.Slug,
                request.Description,
                request.CronExpression,
                timeZoneId,
                nextRunAtUtc,
                clock.UtcNow);

            await dbContext.SaveChangesAsync(cancellationToken);
            await WriteScheduleAuditAsync(
                currentUser,
                schedule,
                "schedule.updated",
                $"Schedule '{schedule.Name}' was updated.",
                cancellationToken);
            return CustomerServiceResult<JobScheduleDto>.Ok(Map(schedule));
        }
        catch (ArgumentException)
        {
            return CustomerServiceResult<JobScheduleDto>.BadRequest();
        }
        catch (InvalidOperationException)
        {
            return CustomerServiceResult<JobScheduleDto>.BadRequest();
        }
    }

    public async Task<CustomerServiceResult<JobScheduleDto>> PauseAsync(
        CurrentUserDto currentUser,
        Guid customerId,
        Guid scheduleId,
        CancellationToken cancellationToken = default)
    {
        return await ChangeStateAsync(
            currentUser,
            customerId,
            scheduleId,
            (schedule, _) => schedule.Pause(clock.UtcNow),
            "schedule.paused",
            schedule => $"Schedule '{schedule.Name}' was paused.",
            cancellationToken);
    }

    public async Task<CustomerServiceResult<JobScheduleDto>> ResumeAsync(
        CurrentUserDto currentUser,
        Guid customerId,
        Guid scheduleId,
        CancellationToken cancellationToken = default)
    {
        return await ChangeStateAsync(
            currentUser,
            customerId,
            scheduleId,
            (schedule, job) =>
            {
                var nextRunAtUtc = ValidateAndGetNextRun(schedule.CronExpression, schedule.TimeZoneId);
                if (nextRunAtUtc is null)
                {
                    throw new InvalidOperationException("Schedule definition is invalid.");
                }

                schedule.Resume(job, nextRunAtUtc, clock.UtcNow);
            },
            "schedule.resumed",
            schedule => $"Schedule '{schedule.Name}' was resumed.",
            cancellationToken);
    }

    public async Task<CustomerServiceResult<JobScheduleDto>> ArchiveAsync(
        CurrentUserDto currentUser,
        Guid customerId,
        Guid scheduleId,
        CancellationToken cancellationToken = default)
    {
        return await ChangeStateAsync(
            currentUser,
            customerId,
            scheduleId,
            (schedule, _) => schedule.Archive(clock.UtcNow),
            "schedule.archived",
            schedule => $"Schedule '{schedule.Name}' was archived.",
            cancellationToken);
    }

    private async Task<CustomerServiceResult<JobScheduleDto>> ChangeStateAsync(
        CurrentUserDto currentUser,
        Guid customerId,
        Guid scheduleId,
        Action<JobSchedule, Job> change,
        string auditAction,
        Func<JobSchedule, string> auditMessage,
        CancellationToken cancellationToken)
    {
        var authorization = await AuthorizeAsync(currentUser, customerId, Permission.ManageSchedules, cancellationToken);
        if (authorization != CustomerAuthorizationResult.Allowed)
        {
            return CustomerServiceResult<JobScheduleDto>.FromAuthorization(authorization);
        }

        var schedule = await dbContext.FindJobScheduleAsync(customerId, scheduleId, cancellationToken);
        if (schedule is null)
        {
            return CustomerServiceResult<JobScheduleDto>.NotFound();
        }

        var job = await dbContext.FindJobAsync(customerId, schedule.JobId, cancellationToken);
        if (job is null)
        {
            return CustomerServiceResult<JobScheduleDto>.BadRequest();
        }

        try
        {
            change(schedule, job);
            await dbContext.SaveChangesAsync(cancellationToken);
            await WriteScheduleAuditAsync(
                currentUser,
                schedule,
                auditAction,
                auditMessage(schedule),
                cancellationToken);
            return CustomerServiceResult<JobScheduleDto>.Ok(Map(schedule));
        }
        catch (InvalidOperationException)
        {
            return CustomerServiceResult<JobScheduleDto>.BadRequest();
        }
    }

    private DateTimeOffset? ValidateAndGetNextRun(string cronExpression, string timeZoneId)
    {
        if (!IsValidScheduleDefinition(cronExpression, timeZoneId))
        {
            return null;
        }

        return cronScheduleCalculator.GetNextRunUtc(cronExpression, timeZoneId, clock.UtcNow);
    }

    private bool IsValidScheduleDefinition(string cronExpression, string timeZoneId)
    {
        return cronScheduleCalculator.IsValidTimeZone(timeZoneId)
            && cronScheduleCalculator.IsValidExpression(cronExpression);
    }

    private static string NormalizeTimeZone(string? timeZoneId)
    {
        return string.IsNullOrWhiteSpace(timeZoneId) ? DefaultTimeZoneId : timeZoneId.Trim();
    }

    private async Task<CustomerAuthorizationResult> AuthorizeAsync(
        CurrentUserDto currentUser,
        Guid customerId,
        Permission permission,
        CancellationToken cancellationToken)
    {
        return await authorizationService.AuthorizeAsync(currentUser, customerId, permission, cancellationToken);
    }

    private async Task WriteScheduleAuditAsync(
        CurrentUserDto currentUser,
        JobSchedule schedule,
        string action,
        string message,
        CancellationToken cancellationToken)
    {
        await auditLogWriter.WriteAsync(new AuditLogWriteRequest(
            schedule.CustomerId,
            currentUser.Id,
            currentUser.DisplayName,
            AuditActorType.User,
            action,
            "Schedule",
            schedule.Id,
            schedule.Name,
            AuditOutcome.Succeeded,
            message,
            JsonSerializer.Serialize(new
            {
                scheduleId = schedule.Id,
                scheduleSlug = schedule.Slug,
                jobId = schedule.JobId,
                status = schedule.Status.ToString()
            })),
            cancellationToken);
    }

    private static JobScheduleDto Map(JobSchedule schedule)
    {
        return new JobScheduleDto(
            schedule.Id,
            schedule.CustomerId,
            schedule.JobId,
            schedule.Name,
            schedule.Slug,
            schedule.Description,
            schedule.CronExpression,
            schedule.TimeZoneId,
            schedule.Status.ToString(),
            schedule.NextRunAtUtc,
            schedule.LastRunAtUtc,
            schedule.LastJobRunId,
            schedule.CreatedAt,
            schedule.UpdatedAt,
            schedule.ArchivedAt);
    }
}
