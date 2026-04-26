using NodeControl.Application.Abstractions.Authorization;
using NodeControl.Application.Abstractions.Persistence;
using NodeControl.Application.Auth;
using NodeControl.Domain.Authorization;
using NodeControl.Domain.Customers;

namespace NodeControl.Application.Authorization;

public sealed class CustomerAuthorizationService(INodeControlDbContext dbContext)
    : ICustomerAuthorizationService
{
    public async Task<CustomerAuthorizationResult> AuthorizeAsync(
        CurrentUserDto currentUser,
        Guid customerId,
        Permission permission,
        CancellationToken cancellationToken)
    {
        if (!currentUser.IsActive)
        {
            return CustomerAuthorizationResult.Forbidden;
        }

        var customer = await dbContext.FindCustomerAsync(customerId, cancellationToken);
        if (customer is null)
        {
            return CustomerAuthorizationResult.NotFound;
        }

        if (currentUser.IsPlatformAdmin)
        {
            return CustomerAuthorizationResult.Allowed;
        }

        if (customer.Status != CustomerStatus.Active)
        {
            return CustomerAuthorizationResult.NotFound;
        }

        var membership = await dbContext.FindCustomerMembershipForUserAsync(
            customerId,
            currentUser.Id,
            cancellationToken);

        if (membership is not { IsActive: true })
        {
            return CustomerAuthorizationResult.Forbidden;
        }

        return RolePermissionMap.HasPermission(membership.Role, permission)
            ? CustomerAuthorizationResult.Allowed
            : CustomerAuthorizationResult.Forbidden;
    }
}
