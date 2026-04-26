using NodeControl.Application.Auth;
using NodeControl.Application.ControlNodes;

namespace NodeControl.Api.Endpoints;

public static class ControlNodesEndpoints
{
    public static IEndpointRouteBuilder MapControlNodesEndpoints(this IEndpointRouteBuilder endpoints)
    {
        var group = endpoints.MapGroup("/api/v1/customers/{customerId:guid}/control-nodes")
            .RequireAuthorization();

        group.MapGet("/", async (
            Guid customerId,
            CurrentUserService currentUserService,
            ControlNodeService controlNodeService,
            CancellationToken cancellationToken) =>
        {
            var currentUser = await currentUserService.GetCurrentUserAsync(cancellationToken);
            if (currentUser is null)
            {
                return Results.Unauthorized();
            }

            return CustomersEndpoints.ToResult(await controlNodeService.ListAsync(
                currentUser,
                customerId,
                cancellationToken));
        });

        group.MapPost("/", async (
            Guid customerId,
            CreateControlNodeRequest request,
            CurrentUserService currentUserService,
            ControlNodeService controlNodeService,
            CancellationToken cancellationToken) =>
        {
            var currentUser = await currentUserService.GetCurrentUserAsync(cancellationToken);
            if (currentUser is null)
            {
                return Results.Unauthorized();
            }

            var result = await controlNodeService.CreateAsync(currentUser, customerId, request, cancellationToken);
            return CustomersEndpoints.ToResult(
                result,
                controlNode => $"/api/v1/customers/{customerId}/control-nodes/{controlNode.Id}");
        });

        group.MapGet("/{controlNodeId:guid}", async (
            Guid customerId,
            Guid controlNodeId,
            CurrentUserService currentUserService,
            ControlNodeService controlNodeService,
            CancellationToken cancellationToken) =>
        {
            var currentUser = await currentUserService.GetCurrentUserAsync(cancellationToken);
            if (currentUser is null)
            {
                return Results.Unauthorized();
            }

            return CustomersEndpoints.ToResult(await controlNodeService.GetAsync(
                currentUser,
                customerId,
                controlNodeId,
                cancellationToken));
        });

        group.MapPut("/{controlNodeId:guid}", async (
            Guid customerId,
            Guid controlNodeId,
            UpdateControlNodeRequest request,
            CurrentUserService currentUserService,
            ControlNodeService controlNodeService,
            CancellationToken cancellationToken) =>
        {
            var currentUser = await currentUserService.GetCurrentUserAsync(cancellationToken);
            if (currentUser is null)
            {
                return Results.Unauthorized();
            }

            return CustomersEndpoints.ToResult(await controlNodeService.UpdateAsync(
                currentUser,
                customerId,
                controlNodeId,
                request,
                cancellationToken));
        });

        group.MapDelete("/{controlNodeId:guid}", async (
            Guid customerId,
            Guid controlNodeId,
            CurrentUserService currentUserService,
            ControlNodeService controlNodeService,
            CancellationToken cancellationToken) =>
        {
            var currentUser = await currentUserService.GetCurrentUserAsync(cancellationToken);
            if (currentUser is null)
            {
                return Results.Unauthorized();
            }

            return CustomersEndpoints.ToResult(await controlNodeService.ArchiveAsync(
                currentUser,
                customerId,
                controlNodeId,
                cancellationToken));
        });

        return endpoints;
    }
}
