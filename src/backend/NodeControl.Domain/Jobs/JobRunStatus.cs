namespace NodeControl.Domain.Jobs;

public enum JobRunStatus
{
    Queued = 1,
    Running = 2,
    Cancelling = 3,
    Succeeded = 4,
    Failed = 5,
    Cancelled = 6,
    TimedOut = 7
}
