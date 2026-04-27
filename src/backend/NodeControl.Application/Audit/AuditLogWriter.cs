using NodeControl.Application.Abstractions.Persistence;
using NodeControl.Application.Abstractions.Time;
using NodeControl.Domain.Audit;

namespace NodeControl.Application.Audit;

public sealed class AuditLogWriter(
    INodeControlDbContext dbContext,
    IClock clock,
    IRequestAuditContext requestAuditContext) : IAuditLogWriter
{
    public async Task WriteAsync(AuditLogWriteRequest request, CancellationToken cancellationToken)
    {
        var entry = AuditLogEntry.Create(
            request.CustomerId,
            request.ActorUserId,
            request.ActorDisplayName,
            request.ActorType,
            request.Action,
            request.EntityType,
            request.EntityId,
            request.EntityDisplayName,
            request.Outcome,
            request.Message,
            request.MetadataJson,
            request.IpAddress ?? requestAuditContext.IpAddress,
            request.UserAgent ?? requestAuditContext.UserAgent,
            clock.UtcNow);

        dbContext.AddAuditLogEntry(entry);
        await dbContext.SaveChangesAsync(cancellationToken);
    }
}
