using NodeControl.Application.Auth;
using NodeControl.Application.ManagedNodes;

namespace NodeControl.Api.Endpoints;

public static class ManagedNodesEndpoints
{
    public static IEndpointRouteBuilder MapManagedNodesEndpoints(this IEndpointRouteBuilder endpoints)
    {
        var group = endpoints.MapGroup("/api/v1/customers/{customerId:guid}/managed-nodes")
            .RequireAuthorization();

        group.MapGet("/", async (
            Guid customerId,
            CurrentUserService currentUserService,
            ManagedNodeService managedNodeService,
            CancellationToken cancellationToken) =>
        {
            var currentUser = await currentUserService.GetCurrentUserAsync(cancellationToken);
            if (currentUser is null)
            {
                return Results.Unauthorized();
            }

            return CustomersEndpoints.ToResult(await managedNodeService.ListAsync(
                currentUser,
                customerId,
                cancellationToken));
        });

        group.MapPost("/", async (
            Guid customerId,
            CreateManagedNodeRequest request,
            CurrentUserService currentUserService,
            ManagedNodeService managedNodeService,
            CancellationToken cancellationToken) =>
        {
            var currentUser = await currentUserService.GetCurrentUserAsync(cancellationToken);
            if (currentUser is null)
            {
                return Results.Unauthorized();
            }

            var result = await managedNodeService.CreateAsync(currentUser, customerId, request, cancellationToken);
            return CustomersEndpoints.ToResult(
                result,
                managedNode => $"/api/v1/customers/{customerId}/managed-nodes/{managedNode.Id}");
        });

        group.MapGet("/{managedNodeId:guid}", async (
            Guid customerId,
            Guid managedNodeId,
            CurrentUserService currentUserService,
            ManagedNodeService managedNodeService,
            CancellationToken cancellationToken) =>
        {
            var currentUser = await currentUserService.GetCurrentUserAsync(cancellationToken);
            if (currentUser is null)
            {
                return Results.Unauthorized();
            }

            return CustomersEndpoints.ToResult(await managedNodeService.GetAsync(
                currentUser,
                customerId,
                managedNodeId,
                cancellationToken));
        });

        group.MapPut("/{managedNodeId:guid}", async (
            Guid customerId,
            Guid managedNodeId,
            UpdateManagedNodeRequest request,
            CurrentUserService currentUserService,
            ManagedNodeService managedNodeService,
            CancellationToken cancellationToken) =>
        {
            var currentUser = await currentUserService.GetCurrentUserAsync(cancellationToken);
            if (currentUser is null)
            {
                return Results.Unauthorized();
            }

            return CustomersEndpoints.ToResult(await managedNodeService.UpdateAsync(
                currentUser,
                customerId,
                managedNodeId,
                request,
                cancellationToken));
        });

        group.MapDelete("/{managedNodeId:guid}", async (
            Guid customerId,
            Guid managedNodeId,
            CurrentUserService currentUserService,
            ManagedNodeService managedNodeService,
            CancellationToken cancellationToken) =>
        {
            var currentUser = await currentUserService.GetCurrentUserAsync(cancellationToken);
            if (currentUser is null)
            {
                return Results.Unauthorized();
            }

            return CustomersEndpoints.ToResult(await managedNodeService.ArchiveAsync(
                currentUser,
                customerId,
                managedNodeId,
                cancellationToken));
        });

        return endpoints;
    }
}
