using NodeControl.Domain.Jobs;

namespace NodeControl.Application.JobRuns;

public sealed record JobRunDto(
    Guid Id,
    Guid CustomerId,
    Guid JobId,
    JobRunTriggerType TriggerType,
    Guid? TriggeredByUserId,
    Guid? ScheduleId,
    JobRunStatus Status,
    DateTimeOffset QueuedAt,
    DateTimeOffset? StartedAt,
    DateTimeOffset? FinishedAt,
    int? ExitCode,
    string? ErrorMessage,
    DateTimeOffset CreatedAt);
