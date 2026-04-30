using NodeControl.Application.Auth;
using NodeControl.Application.GitRepositories;

namespace NodeControl.Api.Endpoints;

public static class GitRepositoriesEndpoints
{
    public static IEndpointRouteBuilder MapGitRepositoriesEndpoints(this IEndpointRouteBuilder endpoints)
    {
        var group = endpoints.MapGroup("/api/v1/customers/{customerId:guid}/git-repositories")
            .RequireAuthorization();

        group.MapGet("/", async (
            Guid customerId,
            CurrentUserService currentUserService,
            GitRepositoryService gitRepositoryService,
            CancellationToken cancellationToken) =>
        {
            var currentUser = await currentUserService.GetCurrentUserAsync(cancellationToken);
            return currentUser is null
                ? Results.Unauthorized()
                : CustomersEndpoints.ToResult(await gitRepositoryService.ListAsync(currentUser, customerId, cancellationToken));
        });

        group.MapPost("/", async (
            Guid customerId,
            CreateGitRepositoryRequest request,
            CurrentUserService currentUserService,
            GitRepositoryService gitRepositoryService,
            CancellationToken cancellationToken) =>
        {
            var currentUser = await currentUserService.GetCurrentUserAsync(cancellationToken);
            if (currentUser is null)
            {
                return Results.Unauthorized();
            }

            var result = await gitRepositoryService.CreateAsync(currentUser, customerId, request, cancellationToken);
            return CustomersEndpoints.ToResult(
                result,
                repository => $"/api/v1/customers/{customerId}/git-repositories/{repository.Id}");
        });

        group.MapGet("/{gitRepositoryId:guid}", async (
            Guid customerId,
            Guid gitRepositoryId,
            CurrentUserService currentUserService,
            GitRepositoryService gitRepositoryService,
            CancellationToken cancellationToken) =>
        {
            var currentUser = await currentUserService.GetCurrentUserAsync(cancellationToken);
            return currentUser is null
                ? Results.Unauthorized()
                : CustomersEndpoints.ToResult(await gitRepositoryService.GetAsync(currentUser, customerId, gitRepositoryId, cancellationToken));
        });

        group.MapPut("/{gitRepositoryId:guid}", async (
            Guid customerId,
            Guid gitRepositoryId,
            UpdateGitRepositoryRequest request,
            CurrentUserService currentUserService,
            GitRepositoryService gitRepositoryService,
            CancellationToken cancellationToken) =>
        {
            var currentUser = await currentUserService.GetCurrentUserAsync(cancellationToken);
            return currentUser is null
                ? Results.Unauthorized()
                : CustomersEndpoints.ToResult(await gitRepositoryService.UpdateAsync(currentUser, customerId, gitRepositoryId, request, cancellationToken));
        });

        group.MapDelete("/{gitRepositoryId:guid}", async (
            Guid customerId,
            Guid gitRepositoryId,
            CurrentUserService currentUserService,
            GitRepositoryService gitRepositoryService,
            CancellationToken cancellationToken) =>
        {
            var currentUser = await currentUserService.GetCurrentUserAsync(cancellationToken);
            return currentUser is null
                ? Results.Unauthorized()
                : CustomersEndpoints.ToResult(await gitRepositoryService.ArchiveAsync(currentUser, customerId, gitRepositoryId, cancellationToken));
        });

        return endpoints;
    }
}
