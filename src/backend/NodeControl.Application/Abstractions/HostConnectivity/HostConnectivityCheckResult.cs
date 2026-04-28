namespace NodeControl.Application.Abstractions.HostConnectivity;

public sealed record HostConnectivityCheckResult(
    bool Succeeded,
    bool TimedOut,
    string Message);
