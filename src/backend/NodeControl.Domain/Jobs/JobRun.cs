namespace NodeControl.Domain.Jobs;

public sealed class JobRun
{
    private JobRun()
    {
    }

    private JobRun(
        Guid id,
        Guid customerId,
        Guid jobId,
        JobRunTriggerType triggerType,
        Guid? triggeredByUserId,
        Guid? scheduleId,
        Guid? retriedFromJobRunId,
        int retryAttempt,
        JobRunStatus status,
        DateTimeOffset queuedAt,
        DateTimeOffset createdAt)
    {
        Id = id;
        CustomerId = customerId;
        JobId = jobId;
        TriggerType = triggerType;
        TriggeredByUserId = triggeredByUserId;
        ScheduleId = scheduleId;
        RetriedFromJobRunId = retriedFromJobRunId;
        RetryAttempt = retryAttempt;
        Status = status;
        QueuedAt = queuedAt;
        CreatedAt = createdAt;
    }

    public Guid Id { get; private set; }

    public Guid CustomerId { get; private set; }

    public Guid JobId { get; private set; }

    public JobRunTriggerType TriggerType { get; private set; }

    public Guid? TriggeredByUserId { get; private set; }

    public Guid? ScheduleId { get; private set; }

    public Guid? RetriedFromJobRunId { get; private set; }

    public int RetryAttempt { get; private set; }

    public JobRunStatus Status { get; private set; }

    public DateTimeOffset QueuedAt { get; private set; }

    public DateTimeOffset? StartedAt { get; private set; }

    public DateTimeOffset? FinishedAt { get; private set; }

    public int? ExitCode { get; private set; }

    public string? ErrorMessage { get; private set; }

    public string? WorkspacePath { get; private set; }

    public string? StdoutLogPath { get; private set; }

    public string? StderrLogPath { get; private set; }

    public DateTimeOffset? CancellationRequestedAtUtc { get; private set; }

    public Guid? CancellationRequestedByUserId { get; private set; }

    public string? CancellationReason { get; private set; }

    public DateTimeOffset CreatedAt { get; private set; }

    public static JobRun CreateManual(Job job, Guid triggeredByUserId, DateTimeOffset queuedAt)
    {
        if (job.Status != JobStatus.Active)
        {
            throw new InvalidOperationException("Archived jobs cannot be queued.");
        }

        if (triggeredByUserId == Guid.Empty)
        {
            throw new ArgumentException("A user id is required.", nameof(triggeredByUserId));
        }

        return new JobRun(
            Guid.NewGuid(),
            job.CustomerId,
            job.Id,
            JobRunTriggerType.Manual,
            triggeredByUserId,
            null,
            null,
            0,
            JobRunStatus.Queued,
            queuedAt,
            queuedAt);
    }

    public static JobRun CreateScheduled(Job job, JobSchedule schedule, DateTimeOffset queuedAt)
    {
        ArgumentNullException.ThrowIfNull(job);
        ArgumentNullException.ThrowIfNull(schedule);
        if (job.Status != JobStatus.Active)
        {
            throw new InvalidOperationException("Archived jobs cannot be queued.");
        }

        if (schedule.CustomerId != job.CustomerId || schedule.JobId != job.Id)
        {
            throw new InvalidOperationException("Schedule and job must belong to the same customer.");
        }

        return new JobRun(
            Guid.NewGuid(),
            job.CustomerId,
            job.Id,
            JobRunTriggerType.Scheduled,
            null,
            schedule.Id,
            null,
            0,
            JobRunStatus.Queued,
            queuedAt,
            queuedAt);
    }

    public static JobRun CreateRetry(JobRun original, Job job, Guid triggeredByUserId, DateTimeOffset queuedAt)
    {
        ArgumentNullException.ThrowIfNull(original);
        ArgumentNullException.ThrowIfNull(job);
        if (job.Status != JobStatus.Active)
        {
            throw new InvalidOperationException("Archived jobs cannot be queued.");
        }

        if (original.CustomerId != job.CustomerId || original.JobId != job.Id)
        {
            throw new InvalidOperationException("Retry JobRun and Job must belong to the same customer.");
        }

        if (!original.CanRetry)
        {
            throw new InvalidOperationException("Only failed, timed out, or cancelled JobRuns can be retried.");
        }

        if (triggeredByUserId == Guid.Empty)
        {
            throw new ArgumentException("A user id is required.", nameof(triggeredByUserId));
        }

        return new JobRun(
            Guid.NewGuid(),
            job.CustomerId,
            job.Id,
            JobRunTriggerType.Retry,
            triggeredByUserId,
            null,
            original.Id,
            original.RetryAttempt + 1,
            JobRunStatus.Queued,
            queuedAt,
            queuedAt);
    }

    public void MarkRunning(DateTimeOffset startedAt)
    {
        EnsureStatus(JobRunStatus.Queued);

        Status = JobRunStatus.Running;
        StartedAt = startedAt;
        ErrorMessage = null;
    }

    public void RequestCancellation(Guid requestedByUserId, string? reason, DateTimeOffset requestedAtUtc)
    {
        if (requestedByUserId == Guid.Empty)
        {
            throw new ArgumentException("A user id is required.", nameof(requestedByUserId));
        }

        if (Status == JobRunStatus.Cancelling)
        {
            EnsureCancellationRequestFields(requestedByUserId, reason, requestedAtUtc);
            return;
        }

        if (IsTerminal)
        {
            throw new InvalidOperationException("Terminal JobRuns cannot be cancelled.");
        }

        if (Status == JobRunStatus.Queued)
        {
            Status = JobRunStatus.Cancelled;
            FinishedAt = requestedAtUtc;
            ErrorMessage = "JobRun was cancelled before execution.";
            EnsureCancellationRequestFields(requestedByUserId, reason, requestedAtUtc);
            return;
        }

        EnsureStatus(JobRunStatus.Running);
        Status = JobRunStatus.Cancelling;
        EnsureCancellationRequestFields(requestedByUserId, reason, requestedAtUtc);
    }

    public void MarkCancelled(int? exitCode, string errorMessage, DateTimeOffset finishedAt)
    {
        if (Status is not (JobRunStatus.Running or JobRunStatus.Cancelling))
        {
            throw new InvalidOperationException("Only running or cancelling JobRuns can be marked cancelled.");
        }

        Status = JobRunStatus.Cancelled;
        ExitCode = exitCode;
        ErrorMessage = NormalizeErrorMessage(errorMessage);
        FinishedAt = finishedAt;
    }

    public void SetExecutionPaths(string workspacePath, string stdoutLogPath, string stderrLogPath)
    {
        WorkspacePath = NormalizePath(workspacePath, nameof(workspacePath));
        StdoutLogPath = NormalizePath(stdoutLogPath, nameof(stdoutLogPath));
        StderrLogPath = NormalizePath(stderrLogPath, nameof(stderrLogPath));
    }

    public void MarkSucceeded(int exitCode, DateTimeOffset finishedAt)
    {
        EnsureStatus(JobRunStatus.Running);

        Status = JobRunStatus.Succeeded;
        ExitCode = exitCode;
        ErrorMessage = null;
        FinishedAt = finishedAt;
    }

    public void MarkFailed(int? exitCode, string errorMessage, DateTimeOffset finishedAt)
    {
        EnsureStatus(JobRunStatus.Running);

        Status = JobRunStatus.Failed;
        ExitCode = exitCode;
        ErrorMessage = NormalizeErrorMessage(errorMessage);
        FinishedAt = finishedAt;
    }

    public void MarkTimedOut(int? exitCode, string errorMessage, DateTimeOffset finishedAt)
    {
        EnsureStatus(JobRunStatus.Running);

        Status = JobRunStatus.TimedOut;
        ExitCode = exitCode;
        ErrorMessage = NormalizeErrorMessage(errorMessage);
        FinishedAt = finishedAt;
    }

    private void EnsureStatus(JobRunStatus expectedStatus)
    {
        if (Status != expectedStatus)
        {
            throw new InvalidOperationException($"JobRun must be {expectedStatus}.");
        }
    }

    private bool IsTerminal =>
        Status is JobRunStatus.Succeeded or JobRunStatus.Failed or JobRunStatus.Cancelled or JobRunStatus.TimedOut;

    private bool CanRetry =>
        Status is JobRunStatus.Failed or JobRunStatus.Cancelled or JobRunStatus.TimedOut;

    private void EnsureCancellationRequestFields(Guid requestedByUserId, string? reason, DateTimeOffset requestedAtUtc)
    {
        CancellationRequestedAtUtc ??= requestedAtUtc;
        CancellationRequestedByUserId ??= requestedByUserId;
        CancellationReason ??= NormalizeOptional(reason, 1000, nameof(reason));
    }

    private static string NormalizeErrorMessage(string errorMessage)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(errorMessage);
        var trimmed = errorMessage.Trim();
        return trimmed.Length <= 4000 ? trimmed : trimmed[..4000];
    }

    private static string NormalizePath(string path, string parameterName)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(path, parameterName);
        var trimmed = path.Trim();
        if (trimmed.Length > 1000)
        {
            throw new ArgumentException("Path must be at most 1000 characters.", parameterName);
        }

        return trimmed;
    }

    private static string? NormalizeOptional(string? value, int maxLength, string name)
    {
        if (string.IsNullOrWhiteSpace(value))
        {
            return null;
        }

        var trimmed = value.Trim();
        if (trimmed.Length > maxLength)
        {
            throw new ArgumentException($"{name} must be at most {maxLength} characters.", name);
        }

        return trimmed;
    }
}
