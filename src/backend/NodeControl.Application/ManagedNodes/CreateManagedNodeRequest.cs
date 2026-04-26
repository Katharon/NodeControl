namespace NodeControl.Application.ManagedNodes;

public sealed record CreateManagedNodeRequest(
    string Name,
    string Hostname,
    int SshPort = 22,
    string? OperatingSystem = null,
    string? Environment = null,
    string? Description = null);
