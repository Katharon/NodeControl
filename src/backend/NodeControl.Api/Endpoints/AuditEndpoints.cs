using NodeControl.Application.Audit;
using NodeControl.Application.Auth;

namespace NodeControl.Api.Endpoints;

public static class AuditEndpoints
{
    public static IEndpointRouteBuilder MapAuditEndpoints(this IEndpointRouteBuilder endpoints)
    {
        var group = endpoints.MapGroup("/api/v1/customers/{customerId:guid}/audit-logs")
            .RequireAuthorization();

        group.MapGet("/", async (
            Guid customerId,
            string? action,
            string? entityType,
            Guid? entityId,
            Guid? actorUserId,
            string? outcome,
            DateTimeOffset? fromUtc,
            DateTimeOffset? toUtc,
            int? limit,
            CurrentUserService currentUserService,
            AuditLogService auditLogService,
            CancellationToken cancellationToken) =>
        {
            var currentUser = await currentUserService.GetCurrentUserAsync(cancellationToken);
            if (currentUser is null)
            {
                return Results.Unauthorized();
            }

            var query = new AuditLogQuery(action, entityType, entityId, actorUserId, outcome, fromUtc, toUtc, limit);
            return CustomersEndpoints.ToResult(await auditLogService.ListAsync(
                currentUser,
                customerId,
                query,
                cancellationToken));
        });

        group.MapGet("/{auditLogEntryId:guid}", async (
            Guid customerId,
            Guid auditLogEntryId,
            CurrentUserService currentUserService,
            AuditLogService auditLogService,
            CancellationToken cancellationToken) =>
        {
            var currentUser = await currentUserService.GetCurrentUserAsync(cancellationToken);
            return currentUser is null
                ? Results.Unauthorized()
                : CustomersEndpoints.ToResult(await auditLogService.GetAsync(
                    currentUser,
                    customerId,
                    auditLogEntryId,
                    cancellationToken));
        });

        return endpoints;
    }
}
