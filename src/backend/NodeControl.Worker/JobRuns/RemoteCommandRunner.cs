using System.Diagnostics;

namespace NodeControl.Worker.JobRuns;

public sealed class RemoteCommandRunner : IRemoteCommandRunner
{
    public async Task<RemoteCommandResult> RunAsync(
        string fileName,
        IReadOnlyList<string> arguments,
        TimeSpan timeout,
        Func<CancellationToken, Task<bool>>? isCancellationRequested,
        Func<string, CancellationToken, Task>? onStdoutLine,
        Func<string, CancellationToken, Task>? onStderrLine,
        CancellationToken cancellationToken = default)
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
                return new RemoteCommandResult(null, false, false, $"{fileName} could not be started.");
            }
        }
        catch (Exception exception) when (exception is InvalidOperationException or System.ComponentModel.Win32Exception)
        {
            return new RemoteCommandResult(null, false, false, $"{fileName} could not be started: {exception.Message}");
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
                return new RemoteCommandResult(TryGetExitCode(process), false, true, $"{fileName} was cancelled.");
            }

            if (DateTimeOffset.UtcNow >= deadlineUtc)
            {
                TryKillProcessTree(process);
                await process.WaitForExitAsync(CancellationToken.None);
                await Task.WhenAll(IgnoreCancellation(stdoutCopy), IgnoreCancellation(stderrCopy));
                return new RemoteCommandResult(TryGetExitCode(process), true, false, $"{fileName} exceeded the timeout of {timeout.TotalSeconds:N0} seconds.");
            }

            await Task.Delay(TimeSpan.FromMilliseconds(500), cancellationToken);
        }

        await Task.WhenAll(stdoutCopy, stderrCopy);
        return new RemoteCommandResult(process.ExitCode, false, false, null);
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
}
