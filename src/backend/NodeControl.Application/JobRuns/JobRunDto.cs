using NodeControl.Domain.Jobs;

namespace NodeControl.Application.JobRuns;

public sealed record JobRunDto(
    Guid Id,
    Guid CustomerId,
    Guid JobId,
    Guid ControlNodeId,
    JobRunTriggerType TriggerType,
    Guid? TriggeredByUserId,
    Guid? ScheduleId,
    Guid? RetriedFromJobRunId,
    int RetryAttempt,
    JobRunStatus Status,
    DateTimeOffset QueuedAt,
    DateTimeOffset? StartedAt,
    DateTimeOffset? FinishedAt,
    int? ExitCode,
    string? ErrorMessage,
    JobRunFailureDiagnosticDto? FailureDiagnostic,
    string? WorkspacePath,
    string? StdoutLogPath,
    string? StderrLogPath,
    DateTimeOffset? CancellationRequestedAtUtc,
    Guid? CancellationRequestedByUserId,
    string? CancellationReason,
    DateTimeOffset CreatedAt);
