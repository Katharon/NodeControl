namespace NodeControl.Application.Audit;

public sealed record AuditLogListResponse(IReadOnlyList<AuditLogEntryDto> Items);
