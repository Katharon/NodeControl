using NodeControl.Application.Auth;
using NodeControl.Application.Users;

namespace NodeControl.Api.Endpoints;

public static class CustomerUserLookupEndpoints
{
    public static IEndpointRouteBuilder MapCustomerUserLookupEndpoints(this IEndpointRouteBuilder endpoints)
    {
        var group = endpoints.MapGroup("/api/v1/customers/{customerId:guid}/users")
            .RequireAuthorization();

        group.MapGet("/lookup", async (
            Guid customerId,
            string? query,
            CurrentUserService currentUserService,
            UserLookupService userLookupService,
            CancellationToken cancellationToken) =>
        {
            var currentUser = await currentUserService.GetCurrentUserAsync(cancellationToken);
            if (currentUser is null)
            {
                return Results.Unauthorized();
            }

            var result = await userLookupService.SearchUsersAsync(
                currentUser,
                customerId,
                query,
                cancellationToken);
            return CustomersEndpoints.ToResult(result);
        });

        return endpoints;
    }
}
