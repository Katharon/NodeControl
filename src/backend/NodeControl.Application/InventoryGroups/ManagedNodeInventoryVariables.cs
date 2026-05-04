using NodeControl.Domain.Nodes;

namespace NodeControl.Application.InventoryGroups;

internal static class ManagedNodeInventoryVariables
{
    public static IReadOnlyDictionary<string, object> Build(ManagedNode managedNode)
    {
        var variables = new Dictionary<string, object>
        {
            ["ansible_host"] = managedNode.Hostname,
            ["ansible_port"] = managedNode.SshPort
        };

        if (ShouldUseLocalConnection(managedNode))
        {
            variables["ansible_connection"] = "local";
        }

        if (!string.IsNullOrWhiteSpace(managedNode.SshUsername))
        {
            variables["ansible_user"] = managedNode.SshUsername;
        }

        if (managedNode.SshPrivateKeySecretId is not null)
        {
            variables["ansible_ssh_private_key_file"] = GetPrivateKeyRelativePath(managedNode);
            variables["ansible_ssh_common_args"] = "-o IdentitiesOnly=yes";
        }

        return variables;
    }

    public static string GetPrivateKeyRelativePath(ManagedNode managedNode)
    {
        return string.Join(
            '/',
            ".nodecontrol",
            "managed-host-keys",
            $"{managedNode.Id:D}.key");
    }

    private static bool ShouldUseLocalConnection(ManagedNode managedNode)
    {
        if (!string.IsNullOrWhiteSpace(managedNode.SshUsername)
            || managedNode.SshPrivateKeySecretId is not null)
        {
            return false;
        }

        var normalized = managedNode.Hostname.Trim().ToLowerInvariant();
        return normalized is "localhost" or "127.0.0.1" or "::1";
    }
}
