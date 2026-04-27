using System.Diagnostics;
using Microsoft.Extensions.Options;
using NodeControl.Application.Abstractions.Execution;

namespace NodeControl.Infrastructure.Execution;

public sealed class AnsiblePlaybookRunner(IOptions<ExecutionOptions> options) : IAnsiblePlaybookRunner
{
    private const string InventoryFileName = "inventory.yml";
    private const string PlaybookFileName = "playbook/site.yml";

    public async Task<AnsiblePlaybookRunResult> RunAsync(
        AnsiblePlaybookRunRequest request,
        CancellationToken cancellationToken = default)
    {
        var result = await RunProcessAsync(request, cancellationToken);
        return new AnsiblePlaybookRunResult(result.ExitCode, result.TimedOut, result.ErrorMessage);
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
        startInfo.ArgumentList.Add(PlaybookFileName);
        startInfo.ArgumentList.Add("-e");
        startInfo.ArgumentList.Add($"@{request.VariableFileName}");

        using var process = new Process { StartInfo = startInfo };

        try
        {
            if (!process.Start())
            {
                return new AnsibleExecutionResult(null, false, "ansible-playbook could not be started.");
            }
        }
        catch (Exception exception) when (exception is InvalidOperationException or System.ComponentModel.Win32Exception)
        {
            return new AnsibleExecutionResult(null, false, $"ansible-playbook could not be started: {exception.Message}");
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

        using var timeoutCts = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken);
        timeoutCts.CancelAfter(request.Timeout);

        try
        {
            await process.WaitForExitAsync(timeoutCts.Token);
            await Task.WhenAll(stdoutCopy, stderrCopy);
            return new AnsibleExecutionResult(process.ExitCode, false, null);
        }
        catch (OperationCanceledException) when (!cancellationToken.IsCancellationRequested)
        {
            TryKillProcessTree(process);
            await process.WaitForExitAsync(CancellationToken.None);
            await Task.WhenAll(IgnoreCancellation(stdoutCopy), IgnoreCancellation(stderrCopy));
            return new AnsibleExecutionResult(null, true, $"ansible-playbook exceeded the timeout of {request.Timeout.TotalSeconds:N0} seconds.");
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
