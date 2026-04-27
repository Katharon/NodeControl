namespace NodeControl.Application.Audit;

public sealed record AuditLogQuery(
    string? Action,
    string? EntityType,
    Guid? EntityId,
    Guid? ActorUserId,
    string? Outcome,
    DateTimeOffset? FromUtc,
    DateTimeOffset? ToUtc,
    int? Limit);
