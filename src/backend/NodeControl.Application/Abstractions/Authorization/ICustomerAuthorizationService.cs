using NodeControl.Application.Auth;
using NodeControl.Domain.Authorization;

namespace NodeControl.Application.Abstractions.Authorization;

public interface ICustomerAuthorizationService
{
    Task<CustomerAuthorizationResult> AuthorizeAsync(
        CurrentUserDto currentUser,
        Guid customerId,
        Permission permission,
        CancellationToken cancellationToken);
}

public enum CustomerAuthorizationResult
{
    Allowed,
    Forbidden,
    NotFound
}
