using NodeControl.Domain.Nodes;

namespace NodeControl.Application.HostConnectionChecks;

public sealed record HostConnectionCheckDto(
    Guid Id,
    Guid CustomerId,
    HostConnectionTargetType TargetType,
    Guid? ControlNodeId,
    Guid? ManagedNodeId,
    string Hostname,
    int Port,
    HostConnectionCheckStatus Status,
    Guid? RequestedByUserId,
    DateTimeOffset QueuedAtUtc,
    DateTimeOffset? StartedAtUtc,
    DateTimeOffset? FinishedAtUtc,
    long? DurationMs,
    string? ResultMessage,
    string? ErrorMessage);
