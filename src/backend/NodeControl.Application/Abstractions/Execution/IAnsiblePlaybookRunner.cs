namespace NodeControl.Application.Abstractions.Execution;

public interface IAnsiblePlaybookRunner
{
    Task<AnsiblePlaybookRunResult> RunAsync(
        AnsiblePlaybookRunRequest request,
        CancellationToken cancellationToken = default);
}

public sealed record AnsiblePlaybookRunRequest(
    string WorkspacePath,
    string VariableFileName,
    string StdoutLogPath,
    string StderrLogPath,
    TimeSpan Timeout);

public sealed record AnsiblePlaybookRunResult(
    int? ExitCode,
    bool TimedOut,
    string? ErrorMessage);
