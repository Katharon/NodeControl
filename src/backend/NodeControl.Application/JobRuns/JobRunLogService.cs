using NodeControl.Application.Abstractions.Authorization;
using NodeControl.Application.Abstractions.Persistence;
using NodeControl.Application.Auth;
using NodeControl.Application.Customers;
using NodeControl.Domain.Authorization;
using NodeControl.Domain.Jobs;

namespace NodeControl.Application.JobRuns;

public sealed class JobRunLogService(
    INodeControlDbContext dbContext,
    ICustomerAuthorizationService authorizationService)
{
    private const int DefaultLimit = 500;
    private const int MaxLimit = 2000;

    public async Task<CustomerServiceResult<JobRunLogsResponse>> ListAsync(
        CurrentUserDto currentUser,
        Guid customerId,
        Guid jobRunId,
        long? afterSequence,
        int? limit,
        CancellationToken cancellationToken = default)
    {
        var authorization = await authorizationService.AuthorizeAsync(
            currentUser,
            customerId,
            Permission.ViewJobRuns,
            cancellationToken);
        if (authorization != CustomerAuthorizationResult.Allowed)
        {
            return CustomerServiceResult<JobRunLogsResponse>.FromAuthorization(authorization);
        }

        var jobRun = await dbContext.FindJobRunAsync(customerId, jobRunId, cancellationToken);
        if (jobRun is null)
        {
            return CustomerServiceResult<JobRunLogsResponse>.NotFound();
        }

        var effectiveLimit = Math.Clamp(limit ?? DefaultLimit, 1, MaxLimit);
        var entries = await dbContext.ListJobRunLogEntriesAsync(
            jobRun.Id,
            afterSequence,
            effectiveLimit,
            cancellationToken);

        return CustomerServiceResult<JobRunLogsResponse>.Ok(new JobRunLogsResponse(entries.Select(Map).ToArray()));
    }

    private static JobRunLogEntryDto Map(JobRunLogEntry entry)
    {
        return new JobRunLogEntryDto(
            entry.Id,
            entry.JobRunId,
            entry.Sequence,
            entry.TimestampUtc,
            entry.Stream.ToString(),
            entry.Level.ToString(),
            JobRunLogRedactor.Redact(entry.Message) ?? string.Empty);
    }
}
