using NodeControl.Application.Auth;
using NodeControl.Application.VariableSets;

namespace NodeControl.Api.Endpoints;

public static class VariableSetsEndpoints
{
    public static IEndpointRouteBuilder MapVariableSetsEndpoints(this IEndpointRouteBuilder endpoints)
    {
        var group = endpoints.MapGroup("/api/v1/customers/{customerId:guid}/variable-sets")
            .RequireAuthorization();

        group.MapGet("/", async (
            Guid customerId,
            CurrentUserService currentUserService,
            VariableSetService variableSetService,
            CancellationToken cancellationToken) =>
        {
            var currentUser = await currentUserService.GetCurrentUserAsync(cancellationToken);
            return currentUser is null
                ? Results.Unauthorized()
                : CustomersEndpoints.ToResult(await variableSetService.ListAsync(currentUser, customerId, cancellationToken));
        });

        group.MapPost("/", async (
            Guid customerId,
            CreateVariableSetRequest request,
            CurrentUserService currentUserService,
            VariableSetService variableSetService,
            CancellationToken cancellationToken) =>
        {
            var currentUser = await currentUserService.GetCurrentUserAsync(cancellationToken);
            if (currentUser is null)
            {
                return Results.Unauthorized();
            }

            var result = await variableSetService.CreateAsync(currentUser, customerId, request, cancellationToken);
            return CustomersEndpoints.ToResult(
                result,
                variableSet => $"/api/v1/customers/{customerId}/variable-sets/{variableSet.Id}");
        });

        group.MapGet("/{variableSetId:guid}", async (
            Guid customerId,
            Guid variableSetId,
            CurrentUserService currentUserService,
            VariableSetService variableSetService,
            CancellationToken cancellationToken) =>
        {
            var currentUser = await currentUserService.GetCurrentUserAsync(cancellationToken);
            return currentUser is null
                ? Results.Unauthorized()
                : CustomersEndpoints.ToResult(await variableSetService.GetAsync(currentUser, customerId, variableSetId, cancellationToken));
        });

        group.MapPut("/{variableSetId:guid}", async (
            Guid customerId,
            Guid variableSetId,
            UpdateVariableSetRequest request,
            CurrentUserService currentUserService,
            VariableSetService variableSetService,
            CancellationToken cancellationToken) =>
        {
            var currentUser = await currentUserService.GetCurrentUserAsync(cancellationToken);
            return currentUser is null
                ? Results.Unauthorized()
                : CustomersEndpoints.ToResult(await variableSetService.UpdateAsync(currentUser, customerId, variableSetId, request, cancellationToken));
        });

        group.MapDelete("/{variableSetId:guid}", async (
            Guid customerId,
            Guid variableSetId,
            CurrentUserService currentUserService,
            VariableSetService variableSetService,
            CancellationToken cancellationToken) =>
        {
            var currentUser = await currentUserService.GetCurrentUserAsync(cancellationToken);
            return currentUser is null
                ? Results.Unauthorized()
                : CustomersEndpoints.ToResult(await variableSetService.ArchiveAsync(currentUser, customerId, variableSetId, cancellationToken));
        });

        group.MapPost("/{variableSetId:guid}/validate", async (
            Guid customerId,
            Guid variableSetId,
            CurrentUserService currentUserService,
            VariableSetService variableSetService,
            CancellationToken cancellationToken) =>
        {
            var currentUser = await currentUserService.GetCurrentUserAsync(cancellationToken);
            return currentUser is null
                ? Results.Unauthorized()
                : CustomersEndpoints.ToResult(await variableSetService.ValidateAsync(currentUser, customerId, variableSetId, cancellationToken));
        });

        return endpoints;
    }
}
