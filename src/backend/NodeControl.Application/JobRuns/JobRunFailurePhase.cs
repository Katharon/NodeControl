namespace NodeControl.Application.JobRuns;

public enum JobRunFailurePhase
{
    Unknown = 0,
    ExecutionInput,
    Workspace,
    CredentialMaterialization,
    Dispatch,
    ProcessStart,
    PlaybookExecution,
    Timeout,
    Cancellation,
    Unhandled
}
