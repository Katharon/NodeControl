using NodeControl.Application.Auth;
using NodeControl.Application.Users;

namespace NodeControl.Api.Endpoints;

public static class UsersEndpoints
{
    public static IEndpointRouteBuilder MapUsersEndpoints(this IEndpointRouteBuilder endpoints)
    {
        var group = endpoints.MapGroup("/api/v1/users")
            .RequireAuthorization();

        group.MapGet("/", async (
            string? q,
            bool? includeInactive,
            int? limit,
            CurrentUserService currentUserService,
            UserService userService,
            CancellationToken cancellationToken) =>
        {
            var currentUser = await currentUserService.GetCurrentUserAsync(cancellationToken);
            if (currentUser is null)
            {
                return Results.Unauthorized();
            }

            var result = await userService.ListUsersAsync(
                currentUser,
                new UserListQuery(q, includeInactive ?? false, limit),
                cancellationToken);
            return CustomersEndpoints.ToResult(result);
        });

        group.MapGet("/{userId:guid}", async (
            Guid userId,
            CurrentUserService currentUserService,
            UserService userService,
            CancellationToken cancellationToken) =>
        {
            var currentUser = await currentUserService.GetCurrentUserAsync(cancellationToken);
            if (currentUser is null)
            {
                return Results.Unauthorized();
            }

            var result = await userService.GetUserAsync(currentUser, userId, cancellationToken);
            return CustomersEndpoints.ToResult(result);
        });

        return endpoints;
    }
}
