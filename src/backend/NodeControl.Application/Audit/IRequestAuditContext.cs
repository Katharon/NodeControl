namespace NodeControl.Application.Audit;

public interface IRequestAuditContext
{
    string? IpAddress { get; }

    string? UserAgent { get; }
}
