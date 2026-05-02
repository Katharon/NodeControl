using NodeControl.Domain.Nodes;

namespace NodeControl.Application.ManagedNodes;

public sealed record ManagedNodeDto(
    Guid Id,
    Guid CustomerId,
    string Name,
    string Hostname,
    int SshPort,
    string? SshUsername,
    Guid? SshPrivateKeySecretId,
    string? OperatingSystem,
    string? Environment,
    string? Description,
    ManagedNodeStatus Status,
    DateTimeOffset CreatedAt,
    DateTimeOffset? UpdatedAt,
    DateTimeOffset? ArchivedAt);
