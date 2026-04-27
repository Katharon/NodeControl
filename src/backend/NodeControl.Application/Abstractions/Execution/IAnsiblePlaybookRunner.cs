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
    TimeSpan Timeout,
    Func<string, CancellationToken, Task>? OnStdoutLine = null,
    Func<string, CancellationToken, Task>? OnStderrLine = null,
    Func<CancellationToken, Task<bool>>? IsCancellationRequested = null);

public sealed record AnsiblePlaybookRunResult(
    int? ExitCode,
    bool TimedOut,
    bool Cancelled,
    string? ErrorMessage);
