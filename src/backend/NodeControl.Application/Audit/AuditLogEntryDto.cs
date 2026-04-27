namespace NodeControl.Application.Audit;

public sealed record AuditLogEntryDto(
    Guid Id,
    Guid? CustomerId,
    Guid? ActorUserId,
    string? ActorDisplayName,
    string ActorType,
    string Action,
    string EntityType,
    Guid? EntityId,
    string? EntityDisplayName,
    string Outcome,
    string Message,
    string? MetadataJson,
    string? IpAddress,
    string? UserAgent,
    DateTimeOffset CreatedAtUtc);
