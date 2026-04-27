namespace NodeControl.Application.JobRuns;

public sealed record JobRunLogsResponse(IReadOnlyList<JobRunLogEntryDto> Items);
