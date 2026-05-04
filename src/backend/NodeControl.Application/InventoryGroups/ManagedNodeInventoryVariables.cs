using NodeControl.Domain.Nodes;

namespace NodeControl.Application.InventoryGroups;

internal static class ManagedNodeInventoryVariables
{
    public static IReadOnlyDictionary<string, object> Build(ManagedNode managedNode, ManagedNode? jumpHost = null)
    {
        var variables = new Dictionary<string, object>
        {
            ["ansible_host"] = managedNode.Hostname,
            ["ansible_port"] = managedNode.SshPort
        };

        if (jumpHost is null && ShouldUseLocalConnection(managedNode))
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
        }

        var sshCommonArgs = BuildSshCommonArgs(managedNode, jumpHost);
        if (!string.IsNullOrWhiteSpace(sshCommonArgs))
        {
            variables["ansible_ssh_common_args"] = sshCommonArgs;
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

    private static string? BuildSshCommonArgs(ManagedNode managedNode, ManagedNode? jumpHost)
    {
        var options = new List<string>();
        if (managedNode.SshPrivateKeySecretId is not null)
        {
            options.Add("-o IdentitiesOnly=yes");
        }

        if (jumpHost is not null)
        {
            options.Add($"-o ProxyCommand=\"{BuildProxyCommand(jumpHost)}\"");
        }

        return options.Count == 0 ? null : string.Join(' ', options);
    }

    private static string BuildProxyCommand(ManagedNode jumpHost)
    {
        var commandParts = new List<string>
        {
            "ssh",
            "-W",
            "%h:%p",
            "-q",
            "-p",
            jumpHost.SshPort.ToString()
        };

        if (jumpHost.SshPrivateKeySecretId is not null)
        {
            commandParts.Add("-i");
            commandParts.Add(ShellQuote(GetPrivateKeyRelativePath(jumpHost)));
            commandParts.Add("-o");
            commandParts.Add("IdentitiesOnly=yes");
        }

        commandParts.Add(ShellQuote(BuildJumpLogin(jumpHost)));
        return string.Join(' ', commandParts);
    }

    private static string BuildJumpLogin(ManagedNode jumpHost)
    {
        return string.IsNullOrWhiteSpace(jumpHost.SshUsername)
            ? jumpHost.Hostname
            : $"{jumpHost.SshUsername}@{jumpHost.Hostname}";
    }

    private static string ShellQuote(string value)
    {
        return $"'{value.Replace("'", "'\"'\"'", StringComparison.Ordinal)}'";
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
