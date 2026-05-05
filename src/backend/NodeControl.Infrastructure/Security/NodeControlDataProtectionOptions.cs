namespace NodeControl.Infrastructure.Security;

public sealed class NodeControlDataProtectionOptions
{
    public const string SectionName = "NodeControl:DataProtection";

    public string ApplicationName { get; set; } = "NodeControl";

    public string KeyRingPath { get; set; } = "/var/lib/nodecontrol/data-protection-keys";
}
