using NodeControl.Application.Auth;
using NodeControl.Application.Secrets;

namespace NodeControl.Api.Endpoints;

public static class SecretsEndpoints
{
    public static IEndpointRouteBuilder MapSecretsEndpoints(this IEndpointRouteBuilder endpoints)
    {
        var group = endpoints.MapGroup("/api/v1/customers/{customerId:guid}/secrets")
            .RequireAuthorization();

        group.MapGet("/", async (
            Guid customerId,
            CurrentUserService currentUserService,
            SecretService secretService,
            CancellationToken cancellationToken) =>
        {
            var currentUser = await currentUserService.GetCurrentUserAsync(cancellationToken);
            return currentUser is null
                ? Results.Unauthorized()
                : CustomersEndpoints.ToResult(await secretService.ListAsync(currentUser, customerId, cancellationToken));
        });

        group.MapPost("/", async (
            Guid customerId,
            CreateSecretRequest request,
            CurrentUserService currentUserService,
            SecretService secretService,
            CancellationToken cancellationToken) =>
        {
            var currentUser = await currentUserService.GetCurrentUserAsync(cancellationToken);
            if (currentUser is null)
            {
                return Results.Unauthorized();
            }

            var result = await secretService.CreateAsync(currentUser, customerId, request, cancellationToken);
            return CustomersEndpoints.ToResult(
                result,
                secret => $"/api/v1/customers/{customerId}/secrets/{secret.Id}");
        });

        group.MapGet("/{secretId:guid}", async (
            Guid customerId,
            Guid secretId,
            CurrentUserService currentUserService,
            SecretService secretService,
            CancellationToken cancellationToken) =>
        {
            var currentUser = await currentUserService.GetCurrentUserAsync(cancellationToken);
            return currentUser is null
                ? Results.Unauthorized()
                : CustomersEndpoints.ToResult(await secretService.GetAsync(currentUser, customerId, secretId, cancellationToken));
        });

        group.MapPut("/{secretId:guid}", async (
            Guid customerId,
            Guid secretId,
            UpdateSecretRequest request,
            CurrentUserService currentUserService,
            SecretService secretService,
            CancellationToken cancellationToken) =>
        {
            var currentUser = await currentUserService.GetCurrentUserAsync(cancellationToken);
            return currentUser is null
                ? Results.Unauthorized()
                : CustomersEndpoints.ToResult(await secretService.UpdateAsync(currentUser, customerId, secretId, request, cancellationToken));
        });

        group.MapPost("/{secretId:guid}/rotate", async (
            Guid customerId,
            Guid secretId,
            RotateSecretRequest request,
            CurrentUserService currentUserService,
            SecretService secretService,
            CancellationToken cancellationToken) =>
        {
            var currentUser = await currentUserService.GetCurrentUserAsync(cancellationToken);
            return currentUser is null
                ? Results.Unauthorized()
                : CustomersEndpoints.ToResult(await secretService.RotateAsync(currentUser, customerId, secretId, request, cancellationToken));
        });

        group.MapDelete("/{secretId:guid}", async (
            Guid customerId,
            Guid secretId,
            CurrentUserService currentUserService,
            SecretService secretService,
            CancellationToken cancellationToken) =>
        {
            var currentUser = await currentUserService.GetCurrentUserAsync(cancellationToken);
            return currentUser is null
                ? Results.Unauthorized()
                : CustomersEndpoints.ToResult(await secretService.ArchiveAsync(currentUser, customerId, secretId, cancellationToken));
        });

        return endpoints;
    }
}
