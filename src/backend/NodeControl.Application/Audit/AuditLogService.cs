using NodeControl.Application.Abstractions.Authorization;
using NodeControl.Application.Abstractions.Persistence;
using NodeControl.Application.Auth;
using NodeControl.Application.Customers;
using NodeControl.Domain.Audit;
using NodeControl.Domain.Authorization;

namespace NodeControl.Application.Audit;

public sealed class AuditLogService(
    INodeControlDbContext dbContext,
    ICustomerAuthorizationService authorizationService,
    IAuditLogWriter auditLogWriter)
{
    public const int DefaultLimit = 100;
    public const int MaxLimit = 500;

    public async Task<CustomerServiceResult<AuditLogListResponse>> ListAsync(
        CurrentUserDto currentUser,
        Guid customerId,
        AuditLogQuery query,
        CancellationToken cancellationToken = default)
    {
        var authorization = await AuthorizeAsync(currentUser, customerId, Permission.ViewAuditLogs, cancellationToken);
        if (authorization != CustomerAuthorizationResult.Allowed)
        {
            return CustomerServiceResult<AuditLogListResponse>.FromAuthorization(authorization);
        }

        if (!TryNormalizeQuery(query, out var normalized, out var limit))
        {
            return CustomerServiceResult<AuditLogListResponse>.BadRequest();
        }

        var entries = await dbContext.ListAuditLogEntriesAsync(customerId, normalized, limit, cancellationToken);
        return CustomerServiceResult<AuditLogListResponse>.Ok(new AuditLogListResponse(entries.Select(Map).ToArray()));
    }

    public async Task<CustomerServiceResult<AuditLogEntryDto>> GetAsync(
        CurrentUserDto currentUser,
        Guid customerId,
        Guid auditLogEntryId,
        CancellationToken cancellationToken = default)
    {
        var authorization = await AuthorizeAsync(currentUser, customerId, Permission.ViewAuditLogs, cancellationToken);
        if (authorization != CustomerAuthorizationResult.Allowed)
        {
            return CustomerServiceResult<AuditLogEntryDto>.FromAuthorization(authorization);
        }

        var entry = await dbContext.FindAuditLogEntryAsync(customerId, auditLogEntryId, cancellationToken);
        return entry is null
            ? CustomerServiceResult<AuditLogEntryDto>.NotFound()
            : CustomerServiceResult<AuditLogEntryDto>.Ok(Map(entry));
    }

    public async Task WriteAsync(AuditLogWriteRequest request, CancellationToken cancellationToken = default)
    {
        await auditLogWriter.WriteAsync(request, cancellationToken);
    }

    private static bool TryNormalizeQuery(AuditLogQuery query, out AuditLogQuery normalized, out int limit)
    {
        limit = query.Limit ?? DefaultLimit;
        if (limit < 1)
        {
            normalized = query;
            return false;
        }

        limit = Math.Min(limit, MaxLimit);
        if (query.FromUtc is not null && query.ToUtc is not null && query.FromUtc > query.ToUtc)
        {
            normalized = query;
            return false;
        }

        if (query.Outcome is not null && !Enum.TryParse<AuditOutcome>(query.Outcome, ignoreCase: true, out _))
        {
            normalized = query;
            return false;
        }

        normalized = query with
        {
            Action = NormalizeOptional(query.Action),
            EntityType = NormalizeOptional(query.EntityType),
            Outcome = NormalizeOptional(query.Outcome)
        };
        return true;
    }

    private async Task<CustomerAuthorizationResult> AuthorizeAsync(
        CurrentUserDto currentUser,
        Guid customerId,
        Permission permission,
        CancellationToken cancellationToken)
    {
        return await authorizationService.AuthorizeAsync(currentUser, customerId, permission, cancellationToken);
    }

    private static string? NormalizeOptional(string? value)
    {
        return string.IsNullOrWhiteSpace(value) ? null : value.Trim();
    }

    private static AuditLogEntryDto Map(AuditLogEntry entry)
    {
        return new AuditLogEntryDto(
            entry.Id,
            entry.CustomerId,
            entry.ActorUserId,
            entry.ActorDisplayName,
            entry.ActorType.ToString(),
            entry.Action,
            entry.EntityType,
            entry.EntityId,
            entry.EntityDisplayName,
            entry.Outcome.ToString(),
            entry.Message,
            entry.MetadataJson,
            entry.IpAddress,
            entry.UserAgent,
            entry.CreatedAtUtc);
    }
}
