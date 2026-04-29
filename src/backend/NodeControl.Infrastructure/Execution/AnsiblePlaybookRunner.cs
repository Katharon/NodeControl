using System.Diagnostics;
using Microsoft.Extensions.Options;
using NodeControl.Application.Abstractions.Execution;

namespace NodeControl.Infrastructure.Execution;

public sealed class AnsiblePlaybookRunner(IOptions<ExecutionOptions> options) : IAnsiblePlaybookRunner
{
    private const string InventoryFileName = "inventory.yml";

    public async Task<AnsiblePlaybookRunResult> RunAsync(
        AnsiblePlaybookRunRequest request,
        CancellationToken cancellationToken = default)
    {
        var result = await RunProcessAsync(request, cancellationToken);
        return new AnsiblePlaybookRunResult(result.ExitCode, result.TimedOut, result.Cancelled, result.ErrorMessage);
    }

    private async Task<AnsibleExecutionResult> RunProcessAsync(
        AnsiblePlaybookRunRequest request,
        CancellationToken cancellationToken)
    {
        var startInfo = new ProcessStartInfo
        {
            FileName = string.IsNullOrWhiteSpace(options.Value.AnsiblePlaybookPath)
                ? "ansible-playbook"
                : options.Value.AnsiblePlaybookPath,
            WorkingDirectory = request.WorkspacePath,
            UseShellExecute = false,
            RedirectStandardOutput = true,
            RedirectStandardError = true
        };

        startInfo.ArgumentList.Add("-i");
        startInfo.ArgumentList.Add(InventoryFileName);
        startInfo.ArgumentList.Add(request.PlaybookFileName);
        startInfo.ArgumentList.Add("-e");
        startInfo.ArgumentList.Add($"@{request.VariableFileName}");

        using var process = new Process { StartInfo = startInfo };

        try
        {
            if (!process.Start())
            {
                return new AnsibleExecutionResult(null, false, false, "ansible-playbook could not be started.");
            }
        }
        catch (Exception exception) when (exception is InvalidOperationException or System.ComponentModel.Win32Exception)
        {
            return new AnsibleExecutionResult(null, false, false, $"ansible-playbook could not be started: {exception.Message}");
        }

        var stdoutCopy = CaptureLinesAsync(
            process.StandardOutput,
            request.StdoutLogPath,
            request.OnStdoutLine,
            cancellationToken);
        var stderrCopy = CaptureLinesAsync(
            process.StandardError,
            request.StderrLogPath,
            request.OnStderrLine,
            cancellationToken);

        var deadlineUtc = DateTimeOffset.UtcNow.Add(request.Timeout);
        while (!process.HasExited)
        {
            if (request.IsCancellationRequested is not null
                && await request.IsCancellationRequested(cancellationToken))
            {
                TryKillProcessTree(process);
                await process.WaitForExitAsync(CancellationToken.None);
                await Task.WhenAll(IgnoreCancellation(stdoutCopy), IgnoreCancellation(stderrCopy));
                return new AnsibleExecutionResult(
                    TryGetExitCode(process),
                    false,
                    true,
                    "ansible-playbook was cancelled.");
            }

            if (DateTimeOffset.UtcNow >= deadlineUtc)
            {
                TryKillProcessTree(process);
                await process.WaitForExitAsync(CancellationToken.None);
                await Task.WhenAll(IgnoreCancellation(stdoutCopy), IgnoreCancellation(stderrCopy));
                return new AnsibleExecutionResult(
                    TryGetExitCode(process),
                    true,
                    false,
                    $"ansible-playbook exceeded the timeout of {request.Timeout.TotalSeconds:N0} seconds.");
            }

            await Task.Delay(TimeSpan.FromMilliseconds(500), cancellationToken);
        }

        await Task.WhenAll(stdoutCopy, stderrCopy);
        return new AnsibleExecutionResult(process.ExitCode, false, false, null);
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

    private static async Task CaptureLinesAsync(
        StreamReader reader,
        string logPath,
        Func<string, CancellationToken, Task>? onLine,
        CancellationToken cancellationToken)
    {
        await using var fileStream = File.Create(logPath);
        await using var writer = new StreamWriter(fileStream);

        while (await reader.ReadLineAsync(cancellationToken) is { } line)
        {
            await writer.WriteLineAsync(line.AsMemory(), cancellationToken);
            await writer.FlushAsync(cancellationToken);
            if (!string.IsNullOrWhiteSpace(line) && onLine is not null)
            {
                await onLine(line, cancellationToken);
            }
        }
    }
}
