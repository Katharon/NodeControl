namespace NodeControl.Worker.JobRuns;

public interface ISshPrivateKeyFilePermissionHardener
{
    SshPrivateKeyFilePermissionResult Harden(string keyPath);
}

public sealed record SshPrivateKeyFilePermissionResult(
    bool Succeeded,
    string? ErrorMessage)
{
    public static SshPrivateKeyFilePermissionResult Ok()
    {
        return new SshPrivateKeyFilePermissionResult(true, null);
    }

    public static SshPrivateKeyFilePermissionResult Failed(string errorMessage)
    {
        return new SshPrivateKeyFilePermissionResult(false, errorMessage);
    }
}
