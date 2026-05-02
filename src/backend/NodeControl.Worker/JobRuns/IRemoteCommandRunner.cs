namespace NodeControl.Worker.JobRuns;

public interface IRemoteCommandRunner
{
    Task<RemoteCommandResult> RunAsync(
        string fileName,
        IReadOnlyList<string> arguments,
        TimeSpan timeout,
        Func<CancellationToken, Task<bool>>? isCancellationRequested,
        Func<string, CancellationToken, Task>? onStdoutLine,
        Func<string, CancellationToken, Task>? onStderrLine,
        CancellationToken cancellationToken = default);
}

public sealed record RemoteCommandResult(
    int? ExitCode,
    bool TimedOut,
    bool Cancelled,
    string? ErrorMessage);
