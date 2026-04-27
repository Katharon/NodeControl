using NodeControl.Application.Auth;
using NodeControl.Application.Playbooks;

namespace NodeControl.Api.Endpoints;

public static class PlaybooksEndpoints
{
    public static IEndpointRouteBuilder MapPlaybooksEndpoints(this IEndpointRouteBuilder endpoints)
    {
        var group = endpoints.MapGroup("/api/v1/customers/{customerId:guid}/playbooks")
            .RequireAuthorization();

        group.MapGet("/", async (
            Guid customerId,
            CurrentUserService currentUserService,
            PlaybookService playbookService,
            CancellationToken cancellationToken) =>
        {
            var currentUser = await currentUserService.GetCurrentUserAsync(cancellationToken);
            return currentUser is null
                ? Results.Unauthorized()
                : CustomersEndpoints.ToResult(await playbookService.ListAsync(currentUser, customerId, cancellationToken));
        });

        group.MapPost("/", async (
            Guid customerId,
            CreatePlaybookRequest request,
            CurrentUserService currentUserService,
            PlaybookService playbookService,
            CancellationToken cancellationToken) =>
        {
            var currentUser = await currentUserService.GetCurrentUserAsync(cancellationToken);
            if (currentUser is null)
            {
                return Results.Unauthorized();
            }

            var result = await playbookService.CreateAsync(currentUser, customerId, request, cancellationToken);
            return CustomersEndpoints.ToResult(
                result,
                playbook => $"/api/v1/customers/{customerId}/playbooks/{playbook.Id}");
        });

        group.MapGet("/{playbookId:guid}", async (
            Guid customerId,
            Guid playbookId,
            CurrentUserService currentUserService,
            PlaybookService playbookService,
            CancellationToken cancellationToken) =>
        {
            var currentUser = await currentUserService.GetCurrentUserAsync(cancellationToken);
            return currentUser is null
                ? Results.Unauthorized()
                : CustomersEndpoints.ToResult(await playbookService.GetAsync(currentUser, customerId, playbookId, cancellationToken));
        });

        group.MapPut("/{playbookId:guid}", async (
            Guid customerId,
            Guid playbookId,
            UpdatePlaybookRequest request,
            CurrentUserService currentUserService,
            PlaybookService playbookService,
            CancellationToken cancellationToken) =>
        {
            var currentUser = await currentUserService.GetCurrentUserAsync(cancellationToken);
            return currentUser is null
                ? Results.Unauthorized()
                : CustomersEndpoints.ToResult(await playbookService.UpdateAsync(currentUser, customerId, playbookId, request, cancellationToken));
        });

        group.MapDelete("/{playbookId:guid}", async (
            Guid customerId,
            Guid playbookId,
            CurrentUserService currentUserService,
            PlaybookService playbookService,
            CancellationToken cancellationToken) =>
        {
            var currentUser = await currentUserService.GetCurrentUserAsync(cancellationToken);
            return currentUser is null
                ? Results.Unauthorized()
                : CustomersEndpoints.ToResult(await playbookService.ArchiveAsync(currentUser, customerId, playbookId, cancellationToken));
        });

        group.MapPost("/{playbookId:guid}/validate", async (
            Guid customerId,
            Guid playbookId,
            CurrentUserService currentUserService,
            PlaybookService playbookService,
            CancellationToken cancellationToken) =>
        {
            var currentUser = await currentUserService.GetCurrentUserAsync(cancellationToken);
            return currentUser is null
                ? Results.Unauthorized()
                : CustomersEndpoints.ToResult(await playbookService.ValidateAsync(currentUser, customerId, playbookId, cancellationToken));
        });

        return endpoints;
    }
}
