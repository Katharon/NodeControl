using NodeControl.Application.Auth;
using NodeControl.Application.Customers;
using NodeControl.Application.Secrets;
using NodeControl.Domain.Authorization;

namespace NodeControl.Api.Endpoints;

public static class SecretReferencesEndpoints
{
    public static IEndpointRouteBuilder MapSecretReferencesEndpoints(this IEndpointRouteBuilder endpoints)
    {
        var group = endpoints.MapGroup("/api/v1/customers/{customerId:guid}/secret-references")
            .RequireAuthorization();

        group.MapPost("/validate", async (
            Guid customerId,
            ValidateSecretReferencesRequest request,
            CurrentUserService currentUserService,
            CustomerService customerService,
            SecretReferenceValidationService validationService,
            CancellationToken cancellationToken) =>
        {
            var currentUser = await currentUserService.GetCurrentUserAsync(cancellationToken);
            if (currentUser is null)
            {
                return Results.Unauthorized();
            }

            var customer = await customerService.GetCustomerAsync(currentUser, customerId, cancellationToken);
            if (customer.Error is not null)
            {
                return CustomersEndpoints.ToResult(customer);
            }

            if (!customer.Value!.Permissions.Contains(Permission.ViewSecrets))
            {
                return Results.Forbid();
            }

            return Results.Ok(await validationService.ValidateAsync(customerId, request.Content, cancellationToken));
        });

        return endpoints;
    }
}
