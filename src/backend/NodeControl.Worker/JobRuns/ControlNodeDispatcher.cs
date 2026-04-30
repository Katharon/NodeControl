using System.Net;
using Microsoft.Extensions.Options;
using NodeControl.Application.Abstractions.Execution;
using NodeControl.Infrastructure.Execution;

namespace NodeControl.Worker.JobRuns;

public sealed class ControlNodeDispatcher(
    IAnsiblePlaybookRunner ansibleRunner,
    IOptions<ExecutionOptions> options) : IControlNodeDispatcher
{
    public async Task<ControlNodeDispatchResult> DispatchAsync(
        ControlNodeDispatchRequest request,
        CancellationToken cancellationToken = default)
    {
        if (request.JobRun.ControlNodeId != request.ControlNode.Id)
        {
            return new ControlNodeDispatchResult(
                null,
                false,
                false,
                "JobRun control node binding does not match the dispatch target.");
        }

        if (!File.Exists(request.Workspace.DispatchManifestPath))
        {
            return new ControlNodeDispatchResult(
                null,
                false,
                false,
                "Control-node dispatch manifest is missing from the execution workspace.");
        }

        if (!CanUseLocalExecution(request.ControlNode.Hostname))
        {
            return new ControlNodeDispatchResult(
                null,
                false,
                false,
                $"Control node '{request.ControlNode.Name}' at {request.ControlNode.Hostname}:{request.ControlNode.SshPort} requires remote dispatch, but no remote transport is configured for this MVP.");
        }

        var runResult = await ansibleRunner.RunAsync(
            new AnsiblePlaybookRunRequest(
                request.Workspace.WorkspacePath,
                request.Workspace.PlaybookFileName,
                request.Workspace.VariableFileName,
                request.Workspace.StdoutLogPath,
                request.Workspace.StderrLogPath,
                request.Timeout,
                request.OnStdoutLine,
                request.OnStderrLine,
                request.IsCancellationRequested),
            cancellationToken);

        return new ControlNodeDispatchResult(
            runResult.ExitCode,
            runResult.TimedOut,
            runResult.Cancelled,
            runResult.ErrorMessage);
    }

    private bool CanUseLocalExecution(string hostname)
    {
        if (!options.Value.AllowLocalControlNodeExecution)
        {
            return false;
        }

        if (string.IsNullOrWhiteSpace(hostname))
        {
            return false;
        }

        var normalized = hostname.Trim();
        if (options.Value.LocalControlNodeHostnames.Any(
            localHostname => string.Equals(localHostname, normalized, StringComparison.OrdinalIgnoreCase)))
        {
            return true;
        }

        return IPAddress.TryParse(normalized, out var address) && IPAddress.IsLoopback(address);
    }
}
