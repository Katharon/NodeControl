namespace NodeControl.Application.ManagedNodes;

public sealed record UpdateManagedNodeRequest(
    string Name,
    string Hostname,
    int SshPort = 22,
    string? SshUsername = null,
    Guid? SshPrivateKeySecretId = null,
    string? OperatingSystem = null,
    string? Environment = null,
    string? Description = null,
    Guid? JumpHostManagedNodeId = null);
