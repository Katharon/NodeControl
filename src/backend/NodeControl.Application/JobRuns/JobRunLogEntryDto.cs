namespace NodeControl.Application.JobRuns;

public sealed record JobRunLogEntryDto(
    Guid Id,
    Guid JobRunId,
    long Sequence,
    DateTimeOffset TimestampUtc,
    string Stream,
    string Level,
    string Message);
