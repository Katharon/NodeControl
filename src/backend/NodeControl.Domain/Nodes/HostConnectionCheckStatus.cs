namespace NodeControl.Domain.Nodes;

public enum HostConnectionCheckStatus
{
    Queued = 1,
    Running = 2,
    Succeeded = 3,
    Failed = 4,
    TimedOut = 5
}
