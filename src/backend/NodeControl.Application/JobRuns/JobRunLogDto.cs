namespace NodeControl.Application.JobRuns;

public sealed record JobRunLogDto(
    string? WorkspacePath,
    string? StdoutLogPath,
    string? StderrLogPath);
