using NodeControl.Application.Audit;

namespace NodeControl.Api.Audit;

public sealed class HttpRequestAuditContext(IHttpContextAccessor httpContextAccessor) : IRequestAuditContext
{
    public string? IpAddress => httpContextAccessor.HttpContext?.Connection.RemoteIpAddress?.ToString();

    public string? UserAgent => httpContextAccessor.HttpContext?.Request.Headers.UserAgent.ToString();
}
