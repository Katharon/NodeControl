namespace NodeControl.Application.ControlNodes;

public sealed record CreateControlNodeRequest(
    string Name,
    string Hostname,
    int SshPort = 22,
    string? Description = null);
