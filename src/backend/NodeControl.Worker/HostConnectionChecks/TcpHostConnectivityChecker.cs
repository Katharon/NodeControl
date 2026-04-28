using System.Diagnostics;
using System.Net.Sockets;
using NodeControl.Application.Abstractions.HostConnectivity;

namespace NodeControl.Worker.HostConnectionChecks;

public sealed class TcpHostConnectivityChecker : IHostConnectivityChecker
{
    public async Task<HostConnectivityCheckResult> CheckTcpAsync(
        string hostname,
        int port,
        TimeSpan timeout,
        CancellationToken cancellationToken)
    {
        var stopwatch = Stopwatch.StartNew();
        using var client = new TcpClient();
        using var timeoutSource = new CancellationTokenSource(timeout);
        using var linkedSource = CancellationTokenSource.CreateLinkedTokenSource(
            cancellationToken,
            timeoutSource.Token);

        try
        {
            await client.ConnectAsync(hostname, port, linkedSource.Token);
            stopwatch.Stop();
            return new HostConnectivityCheckResult(
                true,
                false,
                $"TCP connection to {hostname}:{port} succeeded in {stopwatch.ElapsedMilliseconds} ms.");
        }
        catch (OperationCanceledException) when (timeoutSource.IsCancellationRequested && !cancellationToken.IsCancellationRequested)
        {
            stopwatch.Stop();
            return new HostConnectivityCheckResult(
                false,
                true,
                $"TCP connection to {hostname}:{port} timed out after {timeout.TotalSeconds:0.#} seconds.");
        }
        catch (SocketException exception)
        {
            stopwatch.Stop();
            return new HostConnectivityCheckResult(
                false,
                false,
                $"TCP connection to {hostname}:{port} failed: {exception.SocketErrorCode}.");
        }
        catch (Exception exception) when (exception is not OperationCanceledException)
        {
            stopwatch.Stop();
            return new HostConnectivityCheckResult(
                false,
                false,
                $"TCP connection to {hostname}:{port} failed: {exception.Message}");
        }
    }
}
