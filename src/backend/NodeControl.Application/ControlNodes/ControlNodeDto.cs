using NodeControl.Domain.Nodes;

namespace NodeControl.Application.ControlNodes;

public sealed record ControlNodeDto(
    Guid Id,
    Guid CustomerId,
    string Name,
    string Hostname,
    int SshPort,
    string? Description,
    ControlNodeStatus Status,
    DateTimeOffset CreatedAt,
    DateTimeOffset? UpdatedAt,
    DateTimeOffset? ArchivedAt);
