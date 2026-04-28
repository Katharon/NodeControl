namespace NodeControl.Application.Abstractions.HostConnectivity;

public interface IHostConnectivityChecker
{
    Task<HostConnectivityCheckResult> CheckTcpAsync(
        string hostname,
        int port,
        TimeSpan timeout,
        CancellationToken cancellationToken);
}
