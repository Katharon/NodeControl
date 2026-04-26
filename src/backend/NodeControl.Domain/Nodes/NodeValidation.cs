using System.Text.RegularExpressions;

namespace NodeControl.Domain.Nodes;

internal static partial class NodeValidation
{
    public static void ValidateName(string name, int maxLength)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(name);
        if (name.Trim().Length > maxLength)
        {
            throw new ArgumentException($"Name must be at most {maxLength} characters.", nameof(name));
        }
    }

    public static void ValidateInventoryName(string name)
    {
        ValidateName(name, 100);
        if (!InventorySafeNameRegex().IsMatch(name.Trim()))
        {
            throw new ArgumentException("Name must be inventory-safe.", nameof(name));
        }
    }

    public static void ValidateHostname(string hostname)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(hostname);
        var trimmed = hostname.Trim();
        if (trimmed.Length > 253 || trimmed.Any(char.IsWhiteSpace))
        {
            throw new ArgumentException("Hostname must be at most 253 characters and contain no whitespace.", nameof(hostname));
        }
    }

    public static void ValidateSshPort(int sshPort)
    {
        if (sshPort is < 1 or > 65535)
        {
            throw new ArgumentOutOfRangeException(nameof(sshPort), "SSH port must be between 1 and 65535.");
        }
    }

    public static string? NormalizeOptional(string? value, int maxLength, string name)
    {
        if (string.IsNullOrWhiteSpace(value))
        {
            return null;
        }

        var trimmed = value.Trim();
        if (trimmed.Length > maxLength)
        {
            throw new ArgumentException($"{name} must be at most {maxLength} characters.", name);
        }

        return trimmed;
    }

    [GeneratedRegex("^[a-zA-Z][a-zA-Z0-9_-]{1,99}$")]
    private static partial Regex InventorySafeNameRegex();
}
