namespace NodeControl.Application.Audit;

public sealed class EmptyRequestAuditContext : IRequestAuditContext
{
    public string? IpAddress => null;

    public string? UserAgent => null;
}
