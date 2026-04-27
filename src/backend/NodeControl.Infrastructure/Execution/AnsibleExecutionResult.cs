namespace NodeControl.Infrastructure.Execution;

public sealed record AnsibleExecutionResult(
    int? ExitCode,
    bool TimedOut,
    string? ErrorMessage);
