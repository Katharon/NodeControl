using NodeControl.Application.Auth;
using NodeControl.Application.Customers;

namespace NodeControl.Api.Endpoints;

public static class MeEndpoints
{
    public static IEndpointRouteBuilder MapMeEndpoints(this IEndpointRouteBuilder endpoints)
    {
        var group = endpoints.MapGroup("/api/v1")
            .RequireAuthorization();

        group.MapGet("/me", async (
            CurrentUserService currentUserService,
            CancellationToken cancellationToken) =>
        {
            var currentUser = await currentUserService.GetCurrentUserAsync(cancellationToken);
            return currentUser is null
                ? Results.Unauthorized()
                : Results.Ok(currentUser);
        });

        group.MapGet("/me/customers", async (
            CurrentUserService currentUserService,
            CustomerService customerService,
            CancellationToken cancellationToken) =>
        {
            var currentUser = await currentUserService.GetCurrentUserAsync(cancellationToken);
            if (currentUser is null)
            {
                return Results.Unauthorized();
            }

            var customers = await customerService.ListCustomersAsync(currentUser, cancellationToken);
            return Results.Ok(customers);
        });

        return endpoints;
    }
}
