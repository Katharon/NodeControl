namespace NodeControl.Application.ControlNodes;

public sealed record UpdateControlNodeRequest(
    string Name,
    string Hostname,
    int SshPort = 22,
    string? Description = null);
