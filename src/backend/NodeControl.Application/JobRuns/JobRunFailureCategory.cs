namespace NodeControl.Application.JobRuns;

public enum JobRunFailureCategory
{
    Unknown = 0,
    HostUnreachable,
    SshAuthenticationFailed,
    SshPrivateKeyFilePermissionsTooOpen,
    HostKeyVerificationFailed,
    MissingSecretOrSshKey,
    JumpHostConnectionFailed,
    InventoryGenerationFailed,
    WorkspaceGenerationFailed,
    ControlHostDispatchFailed,
    AnsibleProcessStartFailed,
    PlaybookExecutionFailed,
    TimedOut,
    Cancelled
}
