using NodeControl.Application.Auth;
using NodeControl.Application.Memberships;

namespace NodeControl.Api.Endpoints;

public static class CustomerMembershipsEndpoints
{
    public static IEndpointRouteBuilder MapCustomerMembershipsEndpoints(this IEndpointRouteBuilder endpoints)
    {
        var group = endpoints.MapGroup("/api/v1/customers/{customerId:guid}/memberships")
            .RequireAuthorization();

        group.MapGet("/", async (
            Guid customerId,
            CurrentUserService currentUserService,
            CustomerMembershipService membershipService,
            CancellationToken cancellationToken) =>
        {
            var currentUser = await currentUserService.GetCurrentUserAsync(cancellationToken);
            if (currentUser is null)
            {
                return Results.Unauthorized();
            }

            var result = await membershipService.ListMembershipsAsync(currentUser, customerId, cancellationToken);
            return CustomersEndpoints.ToResult(result);
        });

        group.MapPost("/", async (
            Guid customerId,
            CreateCustomerMembershipRequest request,
            CurrentUserService currentUserService,
            CustomerMembershipService membershipService,
            CancellationToken cancellationToken) =>
        {
            var currentUser = await currentUserService.GetCurrentUserAsync(cancellationToken);
            if (currentUser is null)
            {
                return Results.Unauthorized();
            }

            var result = await membershipService.CreateMembershipAsync(
                currentUser,
                customerId,
                request,
                cancellationToken);
            return CustomersEndpoints.ToResult(
                result,
                membership => $"/api/v1/customers/{customerId}/memberships/{membership.Id}");
        });

        group.MapPut("/{membershipId:guid}", async (
            Guid customerId,
            Guid membershipId,
            UpdateCustomerMembershipRequest request,
            CurrentUserService currentUserService,
            CustomerMembershipService membershipService,
            CancellationToken cancellationToken) =>
        {
            var currentUser = await currentUserService.GetCurrentUserAsync(cancellationToken);
            if (currentUser is null)
            {
                return Results.Unauthorized();
            }

            var result = await membershipService.UpdateMembershipAsync(
                currentUser,
                customerId,
                membershipId,
                request,
                cancellationToken);
            return CustomersEndpoints.ToResult(result);
        });

        group.MapDelete("/{membershipId:guid}", async (
            Guid customerId,
            Guid membershipId,
            CurrentUserService currentUserService,
            CustomerMembershipService membershipService,
            CancellationToken cancellationToken) =>
        {
            var currentUser = await currentUserService.GetCurrentUserAsync(cancellationToken);
            if (currentUser is null)
            {
                return Results.Unauthorized();
            }

            var result = await membershipService.DeactivateMembershipAsync(
                currentUser,
                customerId,
                membershipId,
                cancellationToken);
            return CustomersEndpoints.ToResult(result);
        });

        return endpoints;
    }
}
