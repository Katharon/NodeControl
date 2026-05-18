using System.Net;
using Microsoft.Extensions.Options;
using NodeControl.Application.Abstractions.Execution;
using NodeControl.Infrastructure.Execution;

namespace NodeControl.Worker.JobRuns;

public sealed class ControlNodeDispatcher : IControlNodeDispatcher
{
    private readonly IAnsiblePlaybookRunner ansibleRunner;
    private readonly IOptions<ExecutionOptions> options;
    private readonly IRemoteCommandRunner remoteCommandRunner;
    private readonly ISshPrivateKeyFilePermissionHardener sshKeyPermissionHardener;

    public ControlNodeDispatcher(
        IAnsiblePlaybookRunner ansibleRunner,
        IOptions<ExecutionOptions> options,
        IRemoteCommandRunner remoteCommandRunner,
        ISshPrivateKeyFilePermissionHardener sshKeyPermissionHardener)
    {
        this.ansibleRunner = ansibleRunner;
        this.options = options;
        this.remoteCommandRunner = remoteCommandRunner;
        this.sshKeyPermissionHardener = sshKeyPermissionHardener;
    }

    public ControlNodeDispatcher(
        IAnsiblePlaybookRunner ansibleRunner,
        IOptions<ExecutionOptions> options,
        IRemoteCommandRunner remoteCommandRunner)
        : this(ansibleRunner, options, remoteCommandRunner, new SshPrivateKeyFilePermissionHardener())
    {
    }

    public ControlNodeDispatcher(
        IAnsiblePlaybookRunner ansibleRunner,
        IOptions<ExecutionOptions> options)
        : this(ansibleRunner, options, new RemoteCommandRunner(), new SshPrivateKeyFilePermissionHardener())
    {
    }

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

        try
        {
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
        finally
        {
            TryDeleteDirectory(BuildLocalManagedHostKeyDirectory(request.Workspace));
        }
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

        var tempDirectory = BuildTemporaryDispatchDirectory(request.JobRun.Id);
        var keyPath = Path.Combine(tempDirectory, "id_control_node");
        var remoteRunPath = request.Workspace.ControlHostWorkspacePath;
        var remoteRunParentPath = GetRemoteDirectoryName(remoteRunPath);
        var remoteStagingPath = BuildRemoteStagingPath(remoteRunPath);
        var stagingPrepared = false;
        var promoted = false;

        try
        {
            Directory.CreateDirectory(tempDirectory);
            await File.WriteAllTextAsync(keyPath, NormalizePrivateKey(request.CredentialMaterial.SshPrivateKey), cancellationToken);
            var keyPermissionResult = sshKeyPermissionHardener.Harden(keyPath);
            if (!keyPermissionResult.Succeeded)
            {
                var errorMessage = string.IsNullOrWhiteSpace(keyPermissionResult.ErrorMessage)
                    ? "Temporary SSH private key permission hardening failed."
                    : $"Temporary SSH private key permission hardening failed: {keyPermissionResult.ErrorMessage}";
                await EmitSystemAsync(request, errorMessage, cancellationToken);
                return new ControlNodeDispatchResult(null, false, false, errorMessage);
            }

            await EmitSystemAsync(
                request,
                $"Staging execution workspace to remote control node path {remoteRunPath}.",
                cancellationToken);

            var parentPrepareResult = await RunSshCommandAsync(
                request,
                keyPath,
                BuildRemotePrepareParentCommand(remoteRunParentPath),
                request.Timeout,
                request.OnSystemLine,
                request.OnSystemLine,
                cancellationToken);
            if (parentPrepareResult.ExitCode != 0 || parentPrepareResult.TimedOut || parentPrepareResult.Cancelled)
            {
                return ToDispatchResult(parentPrepareResult, "Remote parent mkdir failed for the control-node run workspace.");
            }

            var stagingPrepareResult = await RunSshCommandAsync(
                request,
                keyPath,
                BuildRemotePrepareStagingCommand(remoteStagingPath),
                request.Timeout,
                request.OnSystemLine,
                request.OnSystemLine,
                cancellationToken);
            if (stagingPrepareResult.ExitCode != 0 || stagingPrepareResult.TimedOut || stagingPrepareResult.Cancelled)
            {
                return ToDispatchResult(stagingPrepareResult, "Remote staging directory create failed for the control-node run workspace.");
            }

            stagingPrepared = true;
            var stageResult = await RunScpAsync(
                request,
                keyPath,
                remoteStagingPath,
                request.Timeout,
                request.OnSystemLine,
                request.OnSystemLine,
                cancellationToken);
            if (stageResult.ExitCode != 0 || stageResult.TimedOut || stageResult.Cancelled)
            {
                return ToDispatchResult(stageResult, "Remote upload failed after path preparation; check JobRun logs for scp target canonicalization or permission errors.");
            }

            var promoteResult = await RunSshCommandAsync(
                request,
                keyPath,
                BuildRemotePromoteStagingCommand(remoteStagingPath, remoteRunPath),
                request.Timeout,
                request.OnSystemLine,
                request.OnSystemLine,
                cancellationToken);
            if (promoteResult.ExitCode != 0 || promoteResult.TimedOut || promoteResult.Cancelled)
            {
                return ToDispatchResult(promoteResult, "Remote staging workspace could not be promoted to the run workspace.");
            }

            promoted = true;
            await EmitSystemAsync(request, "Hardening remote SSH key permissions.", cancellationToken);
            var hardenResult = await RunSshCommandAsync(
                request,
                keyPath,
                BuildRemoteHardenManagedHostKeyPermissionsCommand(remoteRunPath),
                request.Timeout,
                request.OnSystemLine,
                request.OnSystemLine,
                cancellationToken);
            if (hardenResult.ExitCode != 0 || hardenResult.TimedOut || hardenResult.Cancelled)
            {
                await EmitSystemAsync(request, "Remote SSH key permission hardening failed.", cancellationToken);
                return ToDispatchResult(hardenResult, "Remote SSH key permission hardening failed for the control-node run workspace.");
            }

            await EmitSystemAsync(request, "Remote SSH key permissions hardened.", cancellationToken);
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
            if (promoted)
            {
                await TryCleanupRemoteManagedHostKeysAsync(request, keyPath, remoteRunPath, cancellationToken);
            }

            if (stagingPrepared && !promoted)
            {
                await TryCleanupRemoteStagingAsync(request, keyPath, remoteStagingPath, cancellationToken);
            }

            TryDeleteDirectory(BuildLocalManagedHostKeyDirectory(request.Workspace));
            TryDeleteSensitiveFile(keyPath);
            TryDeleteDirectory(tempDirectory);
        }
    }

    private async Task<RemoteCommandResult> RunSshCommandAsync(
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
        return await remoteCommandRunner.RunAsync("ssh", arguments, timeout, request.IsCancellationRequested, onStdoutLine, onStderrLine, cancellationToken);
    }

    private async Task<RemoteCommandResult> RunScpAsync(
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
        return await remoteCommandRunner.RunAsync("scp", arguments, timeout, request.IsCancellationRequested, onStdoutLine, onStderrLine, cancellationToken);
    }

    private async Task TryCleanupRemoteStagingAsync(
        ControlNodeDispatchRequest request,
        string keyPath,
        string remoteStagingPath,
        CancellationToken cancellationToken)
    {
        try
        {
            await RunSshCommandAsync(
                request,
                keyPath,
                BuildRemoteCleanupCommand(remoteStagingPath),
                request.Timeout,
                request.OnSystemLine,
                request.OnSystemLine,
                cancellationToken);
        }
        catch (OperationCanceledException)
        {
        }
    }

    private async Task TryCleanupRemoteManagedHostKeysAsync(
        ControlNodeDispatchRequest request,
        string keyPath,
        string remoteRunPath,
        CancellationToken cancellationToken)
    {
        try
        {
            await RunSshCommandAsync(
                request,
                keyPath,
                BuildRemoteCleanupCommand($"{remoteRunPath}/.nodecontrol/managed-host-keys"),
                request.Timeout,
                request.OnSystemLine,
                request.OnSystemLine,
                cancellationToken);
        }
        catch (OperationCanceledException)
        {
        }
    }

    private static ControlNodeDispatchResult ToDispatchResult(RemoteCommandResult result, string fallbackErrorMessage)
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

    private static string BuildRemoteScpTarget(ControlNodeDispatchRequest request, string remoteDirectoryPath)
    {
        var host = request.ControlNode.Hostname.Contains(':', StringComparison.Ordinal)
            ? $"[{request.ControlNode.Hostname}]"
            : request.ControlNode.Hostname;
        return $"{request.ControlNode.SshUsername}@{host}:{EnsureTrailingSlash(remoteDirectoryPath)}";
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
            "{",
            "command",
            "-v",
            QuoteForRemoteShell(executable),
            ">/dev/null",
            "2>&1",
            "||",
            "{",
            "echo",
            QuoteForRemoteShell("ansible-playbook is not installed or is not executable on the control host."),
            ">&2;",
            "exit",
            "127;",
            "};",
            "}",
            "&&",
            "exec",
            QuoteForRemoteShell(executable),
            "-i",
            QuoteForRemoteShell("inventory.yml"),
            QuoteForRemoteShell(playbookFileName),
            "-e",
            QuoteForRemoteShell($"@{variableFileName}"));
    }

    private static string BuildRemoteStagingPath(string remoteRunPath)
    {
        return $"{remoteRunPath}.staging-{Guid.NewGuid():N}";
    }

    private static string BuildRemotePrepareStagingCommand(string remoteStagingPath)
    {
        return string.Join(
            " ",
            "rm",
            "-rf",
            "--",
            QuoteForRemoteShell(remoteStagingPath),
            "&&",
            "mkdir",
            "--",
            QuoteForRemoteShell(remoteStagingPath));
    }

    private static string BuildRemotePrepareParentCommand(string remoteParentPath)
    {
        return string.Join(
            " ",
            "mkdir",
            "-p",
            "--",
            QuoteForRemoteShell(remoteParentPath));
    }

    private static string BuildRemotePromoteStagingCommand(string remoteStagingPath, string remoteRunPath)
    {
        return string.Join(
            " ",
            "rm",
            "-rf",
            "--",
            QuoteForRemoteShell(remoteRunPath),
            "&&",
            "mkdir",
            "-p",
            "--",
            QuoteForRemoteShell(GetRemoteDirectoryName(remoteRunPath)),
            "&&",
            "mv",
            "--",
            QuoteForRemoteShell(remoteStagingPath),
            QuoteForRemoteShell(remoteRunPath));
    }

    private static string BuildRemoteHardenManagedHostKeyPermissionsCommand(string remoteRunPath)
    {
        var nodeControlPath = $"{remoteRunPath}/.nodecontrol";
        var keyDirectoryPath = $"{nodeControlPath}/managed-host-keys";

        return string.Join(
            " ",
            "if",
            "[",
            "-d",
            QuoteForRemoteShell(keyDirectoryPath),
            "];",
            "then",
            "chmod",
            "700",
            QuoteForRemoteShell(nodeControlPath),
            "&&",
            "chmod",
            "700",
            QuoteForRemoteShell(keyDirectoryPath),
            "&&",
            "find",
            QuoteForRemoteShell(keyDirectoryPath),
            "-type",
            "f",
            "-name",
            QuoteForRemoteShell("*.key"),
            "-exec",
            "chmod",
            "600",
            "{}",
            "\\;",
            ";",
            "else",
            "chmod",
            "700",
            QuoteForRemoteShell(nodeControlPath),
            "2>/dev/null",
            "||",
            "true;",
            "fi");
    }

    private static string BuildRemoteCleanupCommand(string remoteStagingPath)
    {
        return string.Join(" ", "rm", "-rf", "--", QuoteForRemoteShell(remoteStagingPath));
    }

    private static string GetRemoteDirectoryName(string remotePath)
    {
        var trimmed = remotePath.TrimEnd('/');
        var separatorIndex = trimmed.LastIndexOf('/');
        return separatorIndex <= 0 ? "." : trimmed[..separatorIndex];
    }

    private static string EnsureTrailingSlash(string remoteDirectoryPath)
    {
        return remoteDirectoryPath.EndsWith("/", StringComparison.Ordinal)
            ? remoteDirectoryPath
            : $"{remoteDirectoryPath}/";
    }

    private static string BuildTemporaryDispatchDirectory(Guid jobRunId)
    {
        return Path.Combine(
            Path.GetTempPath(),
            "nodecontrol-ssh",
            $"{jobRunId:D}-{Guid.NewGuid():N}");
    }

    private static string BuildLocalManagedHostKeyDirectory(JobRunWorkspace workspace)
    {
        return Path.Combine(workspace.WorkspacePath, ".nodecontrol", "managed-host-keys");
    }

    private static string QuoteForRemoteShell(string value)
    {
        return $"'{value.Replace("'", "'\"'\"'", StringComparison.Ordinal)}'";
    }

    private static string NormalizePrivateKey(string privateKey)
    {
        return privateKey.EndsWith('\n') ? privateKey : privateKey + "\n";
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

    private static void TryDeleteSensitiveFile(string path)
    {
        try
        {
            if (!File.Exists(path))
            {
                return;
            }

            File.WriteAllText(path, string.Empty);
            File.Delete(path);
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
}
