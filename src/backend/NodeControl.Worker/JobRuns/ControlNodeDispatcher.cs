using System.Diagnostics;
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
            return await DispatchRemoteAsync(request, cancellationToken);
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

    private async Task<ControlNodeDispatchResult> DispatchRemoteAsync(
        ControlNodeDispatchRequest request,
        CancellationToken cancellationToken)
    {
        if (string.IsNullOrWhiteSpace(request.ControlNode.SshUsername)
            || string.IsNullOrWhiteSpace(request.ControlNode.RemoteWorkspaceRoot)
            || request.ControlNode.SshPrivateKeySecretId is null)
        {
            return new ControlNodeDispatchResult(
                null,
                false,
                false,
                $"Control node '{request.ControlNode.Name}' at {request.ControlNode.Hostname}:{request.ControlNode.SshPort} requires SSH remote dispatch settings.");
        }

        if (string.IsNullOrWhiteSpace(request.CredentialMaterial?.SshPrivateKey))
        {
            return new ControlNodeDispatchResult(
                null,
                false,
                false,
                "Control node SSH private key material is unavailable for remote dispatch.");
        }

        await EmitSystemAsync(request, "Preparing SSH remote dispatch.", cancellationToken);

        var tempDirectory = Path.Combine(Path.GetTempPath(), "nodecontrol-ssh", request.JobRun.Id.ToString("D"));
        var keyPath = Path.Combine(tempDirectory, "id_control_node");
        try
        {
            Directory.CreateDirectory(tempDirectory);
            await File.WriteAllTextAsync(keyPath, NormalizePrivateKey(request.CredentialMaterial.SshPrivateKey), cancellationToken);
            TrySetPrivateKeyPermissions(keyPath);

            var remoteRunPath = BuildRemoteRunPath(request.ControlNode.RemoteWorkspaceRoot, request);
            await EmitSystemAsync(request, $"Staging execution workspace to remote control node path {remoteRunPath}.", cancellationToken);

            var mkdirResult = await RunSshCommandAsync(
                request,
                keyPath,
                $"mkdir -p -- {QuoteForRemoteShell(remoteRunPath)}",
                request.Timeout,
                request.OnSystemLine,
                request.OnSystemLine,
                cancellationToken);
            if (mkdirResult.ExitCode != 0 || mkdirResult.TimedOut || mkdirResult.Cancelled)
            {
                return ToDispatchResult(mkdirResult, "Remote workspace directory could not be prepared.");
            }

            var stageResult = await RunScpAsync(
                request,
                keyPath,
                remoteRunPath,
                request.Timeout,
                request.OnSystemLine,
                request.OnSystemLine,
                cancellationToken);
            if (stageResult.ExitCode != 0 || stageResult.TimedOut || stageResult.Cancelled)
            {
                return ToDispatchResult(stageResult, "Execution workspace could not be staged to the remote control node.");
            }

            await EmitSystemAsync(request, "Starting ansible-playbook on remote control node.", cancellationToken);
            var ansibleResult = await RunSshCommandAsync(
                request,
                keyPath,
                BuildRemoteAnsibleCommand(remoteRunPath, request.Workspace.PlaybookFileName, request.Workspace.VariableFileName),
                request.Timeout,
                request.OnStdoutLine,
                request.OnStderrLine,
                cancellationToken);

            return new ControlNodeDispatchResult(
                ansibleResult.ExitCode,
                ansibleResult.TimedOut,
                ansibleResult.Cancelled,
                ansibleResult.ErrorMessage);
        }
        finally
        {
            TryDeleteDirectory(tempDirectory);
        }
    }

    private async Task<CommandResult> RunSshCommandAsync(
        ControlNodeDispatchRequest request,
        string keyPath,
        string remoteCommand,
        TimeSpan timeout,
        Func<string, CancellationToken, Task>? onStdoutLine,
        Func<string, CancellationToken, Task>? onStderrLine,
        CancellationToken cancellationToken)
    {
        var arguments = BuildSshArguments(request, keyPath);
        arguments.Add(BuildRemoteLogin(request));
        arguments.Add(remoteCommand);
        return await RunProcessAsync("ssh", arguments, timeout, request.IsCancellationRequested, onStdoutLine, onStderrLine, cancellationToken);
    }

    private async Task<CommandResult> RunScpAsync(
        ControlNodeDispatchRequest request,
        string keyPath,
        string remoteRunPath,
        TimeSpan timeout,
        Func<string, CancellationToken, Task>? onStdoutLine,
        Func<string, CancellationToken, Task>? onStderrLine,
        CancellationToken cancellationToken)
    {
        var arguments = BuildScpArguments(request, keyPath);
        arguments.Add(Path.Combine(request.Workspace.WorkspacePath, "."));
        arguments.Add(BuildRemoteScpTarget(request, remoteRunPath));
        return await RunProcessAsync("scp", arguments, timeout, request.IsCancellationRequested, onStdoutLine, onStderrLine, cancellationToken);
    }

    private static async Task<CommandResult> RunProcessAsync(
        string fileName,
        IReadOnlyList<string> arguments,
        TimeSpan timeout,
        Func<CancellationToken, Task<bool>>? isCancellationRequested,
        Func<string, CancellationToken, Task>? onStdoutLine,
        Func<string, CancellationToken, Task>? onStderrLine,
        CancellationToken cancellationToken)
    {
        var startInfo = new ProcessStartInfo
        {
            FileName = fileName,
            UseShellExecute = false,
            RedirectStandardOutput = true,
            RedirectStandardError = true
        };

        foreach (var argument in arguments)
        {
            startInfo.ArgumentList.Add(argument);
        }

        using var process = new Process { StartInfo = startInfo };

        try
        {
            if (!process.Start())
            {
                return new CommandResult(null, false, false, $"{fileName} could not be started.");
            }
        }
        catch (Exception exception) when (exception is InvalidOperationException or System.ComponentModel.Win32Exception)
        {
            return new CommandResult(null, false, false, $"{fileName} could not be started: {exception.Message}");
        }

        var stdoutCopy = CaptureLinesAsync(process.StandardOutput, onStdoutLine, cancellationToken);
        var stderrCopy = CaptureLinesAsync(process.StandardError, onStderrLine, cancellationToken);
        var deadlineUtc = DateTimeOffset.UtcNow.Add(timeout);

        while (!process.HasExited)
        {
            if (isCancellationRequested is not null && await isCancellationRequested(cancellationToken))
            {
                TryKillProcessTree(process);
                await process.WaitForExitAsync(CancellationToken.None);
                await Task.WhenAll(IgnoreCancellation(stdoutCopy), IgnoreCancellation(stderrCopy));
                return new CommandResult(TryGetExitCode(process), false, true, $"{fileName} was cancelled.");
            }

            if (DateTimeOffset.UtcNow >= deadlineUtc)
            {
                TryKillProcessTree(process);
                await process.WaitForExitAsync(CancellationToken.None);
                await Task.WhenAll(IgnoreCancellation(stdoutCopy), IgnoreCancellation(stderrCopy));
                return new CommandResult(TryGetExitCode(process), true, false, $"{fileName} exceeded the timeout of {timeout.TotalSeconds:N0} seconds.");
            }

            await Task.Delay(TimeSpan.FromMilliseconds(500), cancellationToken);
        }

        await Task.WhenAll(stdoutCopy, stderrCopy);
        return new CommandResult(process.ExitCode, false, false, null);
    }

    private static async Task CaptureLinesAsync(
        StreamReader reader,
        Func<string, CancellationToken, Task>? onLine,
        CancellationToken cancellationToken)
    {
        while (await reader.ReadLineAsync(cancellationToken) is { } line)
        {
            if (!string.IsNullOrWhiteSpace(line) && onLine is not null)
            {
                await onLine(line, cancellationToken);
            }
        }
    }

    private static async Task IgnoreCancellation(Task task)
    {
        try
        {
            await task;
        }
        catch (OperationCanceledException)
        {
        }
    }

    private static void TryKillProcessTree(Process process)
    {
        try
        {
            if (!process.HasExited)
            {
                process.Kill(entireProcessTree: true);
            }
        }
        catch (InvalidOperationException)
        {
        }
    }

    private static int? TryGetExitCode(Process process)
    {
        try
        {
            return process.HasExited ? process.ExitCode : null;
        }
        catch (InvalidOperationException)
        {
            return null;
        }
    }

    private static ControlNodeDispatchResult ToDispatchResult(CommandResult result, string fallbackErrorMessage)
    {
        return new ControlNodeDispatchResult(
            result.ExitCode,
            result.TimedOut,
            result.Cancelled,
            result.ErrorMessage ?? fallbackErrorMessage);
    }

    private static async Task EmitSystemAsync(
        ControlNodeDispatchRequest request,
        string message,
        CancellationToken cancellationToken)
    {
        if (request.OnSystemLine is not null)
        {
            await request.OnSystemLine(message, cancellationToken);
        }
    }

    private static List<string> BuildSshArguments(ControlNodeDispatchRequest request, string keyPath)
    {
        return
        [
            "-p",
            request.ControlNode.SshPort.ToString(),
            "-i",
            keyPath,
            "-o",
            "BatchMode=yes",
            "-o",
            "IdentitiesOnly=yes",
            "-o",
            "StrictHostKeyChecking=accept-new"
        ];
    }

    private static List<string> BuildScpArguments(ControlNodeDispatchRequest request, string keyPath)
    {
        return
        [
            "-r",
            "-P",
            request.ControlNode.SshPort.ToString(),
            "-i",
            keyPath,
            "-o",
            "BatchMode=yes",
            "-o",
            "IdentitiesOnly=yes",
            "-o",
            "StrictHostKeyChecking=accept-new"
        ];
    }

    private static string BuildRemoteLogin(ControlNodeDispatchRequest request)
    {
        return $"{request.ControlNode.SshUsername}@{request.ControlNode.Hostname}";
    }

    private static string BuildRemoteScpTarget(ControlNodeDispatchRequest request, string remoteRunPath)
    {
        var host = request.ControlNode.Hostname.Contains(':', StringComparison.Ordinal)
            ? $"[{request.ControlNode.Hostname}]"
            : request.ControlNode.Hostname;
        return $"{request.ControlNode.SshUsername}@{host}:{QuoteForRemoteShell(remoteRunPath)}";
    }

    private string BuildRemoteAnsibleCommand(string remoteRunPath, string playbookFileName, string variableFileName)
    {
        var executable = string.IsNullOrWhiteSpace(options.Value.RemoteAnsiblePlaybookPath)
            ? "ansible-playbook"
            : options.Value.RemoteAnsiblePlaybookPath.Trim();

        return string.Join(
            " ",
            "cd",
            QuoteForRemoteShell(remoteRunPath),
            "&&",
            "exec",
            QuoteForRemoteShell(executable),
            "-i",
            QuoteForRemoteShell("inventory.yml"),
            QuoteForRemoteShell(playbookFileName),
            "-e",
            QuoteForRemoteShell($"@{variableFileName}"));
    }

    private static string BuildRemoteRunPath(string remoteWorkspaceRoot, ControlNodeDispatchRequest request)
    {
        return string.Join(
            '/',
            remoteWorkspaceRoot.TrimEnd('/'),
            request.JobRun.CustomerId.ToString("D"),
            "control-nodes",
            request.ControlNode.Id.ToString("D"),
            "runs",
            request.JobRun.Id.ToString("D"));
    }

    private static string QuoteForRemoteShell(string value)
    {
        return $"'{value.Replace("'", "'\"'\"'", StringComparison.Ordinal)}'";
    }

    private static string NormalizePrivateKey(string privateKey)
    {
        return privateKey.EndsWith('\n') ? privateKey : privateKey + "\n";
    }

    private static void TrySetPrivateKeyPermissions(string keyPath)
    {
        try
        {
            if (OperatingSystem.IsLinux() || OperatingSystem.IsMacOS() || OperatingSystem.IsFreeBSD())
            {
                File.SetUnixFileMode(keyPath, UnixFileMode.UserRead | UnixFileMode.UserWrite);
            }
        }
        catch (PlatformNotSupportedException)
        {
        }
        catch (UnauthorizedAccessException)
        {
        }
    }

    private static void TryDeleteDirectory(string directory)
    {
        try
        {
            if (Directory.Exists(directory))
            {
                Directory.Delete(directory, recursive: true);
            }
        }
        catch (IOException)
        {
        }
        catch (UnauthorizedAccessException)
        {
        }
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

    private sealed record CommandResult(
        int? ExitCode,
        bool TimedOut,
        bool Cancelled,
        string? ErrorMessage);
}
