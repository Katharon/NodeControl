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

    public JobRunStatus Status { get; private set; }

    public DateTimeOffset QueuedAt { get; private set; }

    public DateTimeOffset? StartedAt { get; private set; }

    public DateTimeOffset? FinishedAt { get; private set; }

    public int? ExitCode { get; private set; }

    public string? ErrorMessage { get; private set; }

    public string? WorkspacePath { get; private set; }

    public string? StdoutLogPath { get; private set; }

    public string? StderrLogPath { get; private set; }

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
}
