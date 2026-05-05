namespace NodeControl.Application.JobRuns;

public sealed record JobRunFailureDiagnosticDto(
    string Category,
    string Title,
    string Summary,
    string? NextStep);
