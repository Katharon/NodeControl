using NodeControl.Application.Auth;
using NodeControl.Application.HostConnectionChecks;
using NodeControl.Domain.Nodes;

namespace NodeControl.Api.Endpoints;

public static class HostConnectionChecksEndpoints
{
    public static IEndpointRouteBuilder MapHostConnectionChecksEndpoints(this IEndpointRouteBuilder endpoints)
    {
        var customerGroup = endpoints.MapGroup("/api/v1/customers/{customerId:guid}")
            .RequireAuthorization();

        customerGroup.MapGet("/host-connection-checks", async (
            Guid customerId,
            HostConnectionTargetType? targetType,
            Guid? controlNodeId,
            Guid? managedNodeId,
            int? limit,
            CurrentUserService currentUserService,
            HostConnectionCheckService hostConnectionCheckService,
            CancellationToken cancellationToken) =>
        {
            var currentUser = await currentUserService.GetCurrentUserAsync(cancellationToken);
            if (currentUser is null)
            {
                return Results.Unauthorized();
            }

            return CustomersEndpoints.ToResult(await hostConnectionCheckService.ListAsync(
                currentUser,
                customerId,
                targetType,
                controlNodeId,
                managedNodeId,
                limit,
                cancellationToken));
        });

        customerGroup.MapGet("/host-connection-checks/{checkId:guid}", async (
            Guid customerId,
            Guid checkId,
            CurrentUserService currentUserService,
            HostConnectionCheckService hostConnectionCheckService,
            CancellationToken cancellationToken) =>
        {
            var currentUser = await currentUserService.GetCurrentUserAsync(cancellationToken);
            if (currentUser is null)
            {
                return Results.Unauthorized();
            }

            return CustomersEndpoints.ToResult(await hostConnectionCheckService.GetAsync(
                currentUser,
                customerId,
                checkId,
                cancellationToken));
        });

        customerGroup.MapGet("/host-health", async (
            Guid customerId,
            CurrentUserService currentUserService,
            HostConnectionCheckService hostConnectionCheckService,
            CancellationToken cancellationToken) =>
        {
            var currentUser = await currentUserService.GetCurrentUserAsync(cancellationToken);
            if (currentUser is null)
            {
                return Results.Unauthorized();
            }

            return CustomersEndpoints.ToResult(await hostConnectionCheckService.GetHostHealthSummaryAsync(
                currentUser,
                customerId,
                cancellationToken));
        });

        customerGroup.MapPost("/control-nodes/{controlNodeId:guid}/connection-checks", async (
            Guid customerId,
            Guid controlNodeId,
            CurrentUserService currentUserService,
            HostConnectionCheckService hostConnectionCheckService,
            CancellationToken cancellationToken) =>
        {
            var currentUser = await currentUserService.GetCurrentUserAsync(cancellationToken);
            if (currentUser is null)
            {
                return Results.Unauthorized();
            }

            var result = await hostConnectionCheckService.QueueControlNodeCheckAsync(
                currentUser,
                customerId,
                controlNodeId,
                cancellationToken);
            return CustomersEndpoints.ToResult(
                result,
                check => $"/api/v1/customers/{customerId}/host-connection-checks/{check.Id}");
        });

        customerGroup.MapPost("/managed-nodes/{managedNodeId:guid}/connection-checks", async (
            Guid customerId,
            Guid managedNodeId,
            CurrentUserService currentUserService,
            HostConnectionCheckService hostConnectionCheckService,
            CancellationToken cancellationToken) =>
        {
            var currentUser = await currentUserService.GetCurrentUserAsync(cancellationToken);
            if (currentUser is null)
            {
                return Results.Unauthorized();
            }

            var result = await hostConnectionCheckService.QueueManagedNodeCheckAsync(
                currentUser,
                customerId,
                managedNodeId,
                cancellationToken);
            return CustomersEndpoints.ToResult(
                result,
                check => $"/api/v1/customers/{customerId}/host-connection-checks/{check.Id}");
        });

        return endpoints;
    }
}
