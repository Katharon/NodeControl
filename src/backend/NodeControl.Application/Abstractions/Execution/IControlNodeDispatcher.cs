using NodeControl.Domain.Jobs;
using NodeControl.Domain.Nodes;

namespace NodeControl.Application.Abstractions.Execution;

public interface IControlNodeDispatcher
{
    Task<ControlNodeDispatchResult> DispatchAsync(
        ControlNodeDispatchRequest request,
        CancellationToken cancellationToken = default);
}

public sealed record ControlNodeDispatchRequest(
    JobRun JobRun,
    ControlNode ControlNode,
    JobRunWorkspace Workspace,
    TimeSpan Timeout,
    ControlNodeCredentialMaterial? CredentialMaterial = null,
    Func<string, CancellationToken, Task>? OnSystemLine = null,
    Func<string, CancellationToken, Task>? OnStdoutLine = null,
    Func<string, CancellationToken, Task>? OnStderrLine = null,
    Func<CancellationToken, Task<bool>>? IsCancellationRequested = null);

public sealed record ControlNodeCredentialMaterial(
    string? SshPrivateKey);

public sealed record ControlNodeDispatchResult(
    int? ExitCode,
    bool TimedOut,
    bool Cancelled,
    string? ErrorMessage);
