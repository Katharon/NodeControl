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
}
