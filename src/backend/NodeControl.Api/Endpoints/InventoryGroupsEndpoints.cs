using NodeControl.Application.Auth;
using NodeControl.Application.InventoryGroups;

namespace NodeControl.Api.Endpoints;

public static class InventoryGroupsEndpoints
{
    public static IEndpointRouteBuilder MapInventoryGroupsEndpoints(this IEndpointRouteBuilder endpoints)
    {
        var group = endpoints.MapGroup("/api/v1/customers/{customerId:guid}/inventory-groups")
            .RequireAuthorization();

        group.MapGet("/", async (
            Guid customerId,
            CurrentUserService currentUserService,
            InventoryGroupService inventoryGroupService,
            CancellationToken cancellationToken) =>
        {
            var currentUser = await currentUserService.GetCurrentUserAsync(cancellationToken);
            if (currentUser is null)
            {
                return Results.Unauthorized();
            }

            return CustomersEndpoints.ToResult(await inventoryGroupService.ListAsync(
                currentUser,
                customerId,
                cancellationToken));
        });

        group.MapPost("/", async (
            Guid customerId,
            CreateInventoryGroupRequest request,
            CurrentUserService currentUserService,
            InventoryGroupService inventoryGroupService,
            CancellationToken cancellationToken) =>
        {
            var currentUser = await currentUserService.GetCurrentUserAsync(cancellationToken);
            if (currentUser is null)
            {
                return Results.Unauthorized();
            }

            var result = await inventoryGroupService.CreateAsync(currentUser, customerId, request, cancellationToken);
            return CustomersEndpoints.ToResult(
                result,
                inventoryGroup => $"/api/v1/customers/{customerId}/inventory-groups/{inventoryGroup.Id}");
        });

        group.MapGet("/{inventoryGroupId:guid}", async (
            Guid customerId,
            Guid inventoryGroupId,
            CurrentUserService currentUserService,
            InventoryGroupService inventoryGroupService,
            CancellationToken cancellationToken) =>
        {
            var currentUser = await currentUserService.GetCurrentUserAsync(cancellationToken);
            if (currentUser is null)
            {
                return Results.Unauthorized();
            }

            return CustomersEndpoints.ToResult(await inventoryGroupService.GetAsync(
                currentUser,
                customerId,
                inventoryGroupId,
                cancellationToken));
        });

        group.MapPut("/{inventoryGroupId:guid}", async (
            Guid customerId,
            Guid inventoryGroupId,
            UpdateInventoryGroupRequest request,
            CurrentUserService currentUserService,
            InventoryGroupService inventoryGroupService,
            CancellationToken cancellationToken) =>
        {
            var currentUser = await currentUserService.GetCurrentUserAsync(cancellationToken);
            if (currentUser is null)
            {
                return Results.Unauthorized();
            }

            return CustomersEndpoints.ToResult(await inventoryGroupService.UpdateAsync(
                currentUser,
                customerId,
                inventoryGroupId,
                request,
                cancellationToken));
        });

        group.MapDelete("/{inventoryGroupId:guid}", async (
            Guid customerId,
            Guid inventoryGroupId,
            CurrentUserService currentUserService,
            InventoryGroupService inventoryGroupService,
            CancellationToken cancellationToken) =>
        {
            var currentUser = await currentUserService.GetCurrentUserAsync(cancellationToken);
            if (currentUser is null)
            {
                return Results.Unauthorized();
            }

            return CustomersEndpoints.ToResult(await inventoryGroupService.ArchiveAsync(
                currentUser,
                customerId,
                inventoryGroupId,
                cancellationToken));
        });

        group.MapPost("/{inventoryGroupId:guid}/nodes", async (
            Guid customerId,
            Guid inventoryGroupId,
            AddManagedNodeToInventoryGroupRequest request,
            CurrentUserService currentUserService,
            InventoryGroupService inventoryGroupService,
            CancellationToken cancellationToken) =>
        {
            var currentUser = await currentUserService.GetCurrentUserAsync(cancellationToken);
            if (currentUser is null)
            {
                return Results.Unauthorized();
            }

            return CustomersEndpoints.ToResult(await inventoryGroupService.AddManagedNodeAsync(
                currentUser,
                customerId,
                inventoryGroupId,
                request,
                cancellationToken));
        });

        group.MapDelete("/{inventoryGroupId:guid}/nodes/{managedNodeId:guid}", async (
            Guid customerId,
            Guid inventoryGroupId,
            Guid managedNodeId,
            CurrentUserService currentUserService,
            InventoryGroupService inventoryGroupService,
            CancellationToken cancellationToken) =>
        {
            var currentUser = await currentUserService.GetCurrentUserAsync(cancellationToken);
            if (currentUser is null)
            {
                return Results.Unauthorized();
            }

            return CustomersEndpoints.ToResult(await inventoryGroupService.RemoveManagedNodeAsync(
                currentUser,
                customerId,
                inventoryGroupId,
                managedNodeId,
                cancellationToken));
        });

        group.MapGet("/{inventoryGroupId:guid}/preview", async (
            Guid customerId,
            Guid inventoryGroupId,
            CurrentUserService currentUserService,
            InventoryPreviewService inventoryPreviewService,
            CancellationToken cancellationToken) =>
        {
            var currentUser = await currentUserService.GetCurrentUserAsync(cancellationToken);
            if (currentUser is null)
            {
                return Results.Unauthorized();
            }

            return CustomersEndpoints.ToResult(await inventoryPreviewService.GetPreviewAsync(
                currentUser,
                customerId,
                inventoryGroupId,
                cancellationToken));
        });

        return endpoints;
    }
}
