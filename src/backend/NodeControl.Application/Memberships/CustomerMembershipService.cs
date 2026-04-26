using NodeControl.Application.Abstractions.Authorization;
using NodeControl.Application.Abstractions.Persistence;
using NodeControl.Application.Abstractions.Time;
using NodeControl.Application.Auth;
using NodeControl.Application.Customers;
using NodeControl.Domain.Authorization;
using NodeControl.Domain.Customers;

namespace NodeControl.Application.Memberships;

public sealed class CustomerMembershipService(
    INodeControlDbContext dbContext,
    ICustomerAuthorizationService authorizationService,
    IClock clock)
{
    public async Task<CustomerServiceResult<IReadOnlyList<CustomerMembershipDto>>> ListMembershipsAsync(
        CurrentUserDto currentUser,
        Guid customerId,
        CancellationToken cancellationToken = default)
    {
        var authorization = await authorizationService.AuthorizeAsync(
            currentUser,
            customerId,
            Permission.ManageMemberships,
            cancellationToken);

        if (authorization != CustomerAuthorizationResult.Allowed)
        {
            return CustomerServiceResult<IReadOnlyList<CustomerMembershipDto>>.FromAuthorization(authorization);
        }

        var memberships = await dbContext.ListCustomerMembershipsAsync(customerId, cancellationToken);
        return CustomerServiceResult<IReadOnlyList<CustomerMembershipDto>>.Ok(
            memberships.Select(MapMembership).ToArray());
    }

    public async Task<CustomerServiceResult<CustomerMembershipDto>> CreateMembershipAsync(
        CurrentUserDto currentUser,
        Guid customerId,
        CreateCustomerMembershipRequest request,
        CancellationToken cancellationToken = default)
    {
        var authorization = await authorizationService.AuthorizeAsync(
            currentUser,
            customerId,
            Permission.ManageMemberships,
            cancellationToken);

        if (authorization != CustomerAuthorizationResult.Allowed)
        {
            return CustomerServiceResult<CustomerMembershipDto>.FromAuthorization(authorization);
        }

        var existingMembership = await dbContext.FindCustomerMembershipForUserAsync(
            customerId,
            request.UserId,
            cancellationToken);

        if (existingMembership is not null)
        {
            return CustomerServiceResult<CustomerMembershipDto>.Conflict();
        }

        var customer = await dbContext.FindCustomerAsync(customerId, cancellationToken);
        var user = await dbContext.FindUserAsync(request.UserId, cancellationToken);
        if (customer is null || user is null || !user.IsActive)
        {
            return CustomerServiceResult<CustomerMembershipDto>.NotFound();
        }

        var membership = CustomerMembership.Create(customer, user, request.Role, clock.UtcNow);
        dbContext.AddCustomerMembership(membership);
        await dbContext.SaveChangesAsync(cancellationToken);

        return CustomerServiceResult<CustomerMembershipDto>.Ok(MapMembership(membership));
    }

    public async Task<CustomerServiceResult<CustomerMembershipDto>> UpdateMembershipAsync(
        CurrentUserDto currentUser,
        Guid customerId,
        Guid membershipId,
        UpdateCustomerMembershipRequest request,
        CancellationToken cancellationToken = default)
    {
        var authorization = await authorizationService.AuthorizeAsync(
            currentUser,
            customerId,
            Permission.ManageMemberships,
            cancellationToken);

        if (authorization != CustomerAuthorizationResult.Allowed)
        {
            return CustomerServiceResult<CustomerMembershipDto>.FromAuthorization(authorization);
        }

        var membership = await dbContext.FindCustomerMembershipAsync(membershipId, cancellationToken);
        if (membership is null || membership.CustomerId != customerId)
        {
            return CustomerServiceResult<CustomerMembershipDto>.NotFound();
        }

        membership.Update(request.Role, request.IsActive, clock.UtcNow);
        await dbContext.SaveChangesAsync(cancellationToken);

        return CustomerServiceResult<CustomerMembershipDto>.Ok(MapMembership(membership));
    }

    public async Task<CustomerServiceResult<CustomerMembershipDto>> DeactivateMembershipAsync(
        CurrentUserDto currentUser,
        Guid customerId,
        Guid membershipId,
        CancellationToken cancellationToken = default)
    {
        var authorization = await authorizationService.AuthorizeAsync(
            currentUser,
            customerId,
            Permission.ManageMemberships,
            cancellationToken);

        if (authorization != CustomerAuthorizationResult.Allowed)
        {
            return CustomerServiceResult<CustomerMembershipDto>.FromAuthorization(authorization);
        }

        var membership = await dbContext.FindCustomerMembershipAsync(membershipId, cancellationToken);
        if (membership is null || membership.CustomerId != customerId)
        {
            return CustomerServiceResult<CustomerMembershipDto>.NotFound();
        }

        membership.Deactivate(clock.UtcNow);
        await dbContext.SaveChangesAsync(cancellationToken);

        return CustomerServiceResult<CustomerMembershipDto>.Ok(MapMembership(membership));
    }

    private static CustomerMembershipDto MapMembership(CustomerMembership membership)
    {
        return new CustomerMembershipDto(
            membership.Id,
            membership.CustomerId,
            membership.UserId,
            membership.User.DisplayName,
            membership.User.Email,
            membership.Role,
            membership.IsActive,
            membership.CreatedAt,
            membership.UpdatedAt,
            membership.DeactivatedAt);
    }
}
