namespace NodeControl.Application.Audit;

public interface IAuditLogWriter
{
    Task WriteAsync(AuditLogWriteRequest request, CancellationToken cancellationToken);
}
