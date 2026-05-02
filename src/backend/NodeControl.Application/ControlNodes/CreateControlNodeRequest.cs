namespace NodeControl.Application.ControlNodes;

public sealed record CreateControlNodeRequest(
    string Name,
    string Hostname,
    int SshPort = 22,
    string? SshUsername = null,
    Guid? SshPrivateKeySecretId = null,
    string? RemoteWorkspaceRoot = null,
    string? Description = null);
