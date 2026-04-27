using NodeControl.Domain.Audit;

namespace NodeControl.Application.Audit;

public sealed record AuditLogWriteRequest(
    Guid? CustomerId,
    Guid? ActorUserId,
    string? ActorDisplayName,
    AuditActorType ActorType,
    string Action,
    string EntityType,
    Guid? EntityId,
    string? EntityDisplayName,
    AuditOutcome Outcome,
    string Message,
    string? MetadataJson = null,
    string? IpAddress = null,
    string? UserAgent = null);
