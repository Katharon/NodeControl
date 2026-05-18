using System.Runtime.Versioning;
using System.Security.AccessControl;
using System.Security.Principal;

namespace NodeControl.Worker.JobRuns;

public sealed class SshPrivateKeyFilePermissionHardener : ISshPrivateKeyFilePermissionHardener
{
    public SshPrivateKeyFilePermissionResult Harden(string keyPath)
    {
        try
        {
            if (OperatingSystem.IsWindows())
            {
                HardenWindowsAcl(keyPath);
                return SshPrivateKeyFilePermissionResult.Ok();
            }

            if (OperatingSystem.IsLinux() || OperatingSystem.IsMacOS() || OperatingSystem.IsFreeBSD())
            {
                File.SetUnixFileMode(keyPath, UnixFileMode.UserRead | UnixFileMode.UserWrite);
            }

            return SshPrivateKeyFilePermissionResult.Ok();
        }
        catch (Exception exception) when (exception is IOException
            or PlatformNotSupportedException
            or UnauthorizedAccessException
            or InvalidOperationException
            or SystemException)
        {
            return SshPrivateKeyFilePermissionResult.Failed(exception.Message);
        }
    }

    [SupportedOSPlatform("windows")]
    private static void HardenWindowsAcl(string keyPath)
    {
        var currentUser = WindowsIdentity.GetCurrent().User
            ?? throw new InvalidOperationException("Current Windows user SID could not be resolved.");

        var fileInfo = new FileInfo(keyPath);
        var security = fileInfo.GetAccessControl(AccessControlSections.Access);
        security.SetAccessRuleProtection(isProtected: true, preserveInheritance: false);
        RemoveExplicitAccessRules(security);

        AddAllowRule(security, currentUser, FileSystemRights.Read | FileSystemRights.Write | FileSystemRights.Delete);
        AddAllowRule(
            security,
            new SecurityIdentifier(WellKnownSidType.LocalSystemSid, null),
            FileSystemRights.FullControl);
        AddAllowRule(
            security,
            new SecurityIdentifier(WellKnownSidType.BuiltinAdministratorsSid, null),
            FileSystemRights.FullControl);

        fileInfo.SetAccessControl(security);
    }

    [SupportedOSPlatform("windows")]
    private static void RemoveExplicitAccessRules(FileSecurity security)
    {
        var rules = security.GetAccessRules(includeExplicit: true, includeInherited: false, typeof(SecurityIdentifier));
        foreach (FileSystemAccessRule rule in rules)
        {
            security.RemoveAccessRuleSpecific(rule);
        }
    }

    [SupportedOSPlatform("windows")]
    private static void AddAllowRule(FileSecurity security, IdentityReference identity, FileSystemRights rights)
    {
        security.AddAccessRule(new FileSystemAccessRule(
            identity,
            rights,
            AccessControlType.Allow));
    }
}
