using NodeControl.Application.Abstractions.Authorization;
using NodeControl.Application.Abstractions.Persistence;
using NodeControl.Application.Auth;
using NodeControl.Application.Customers;
using NodeControl.Domain.Authorization;
using NodeControl.Domain.Users;

namespace NodeControl.Application.Users;

public sealed class UserLookupService(
    INodeControlDbContext dbContext,
    ICustomerAuthorizationService authorizationService)
{
    private const int DefaultLimit = 20;
    private const int MaxLimit = 50;

    public async Task<CustomerServiceResult<IReadOnlyList<UserLookupDto>>> SearchUsersAsync(
        CurrentUserDto currentUser,
        Guid customerId,
        string? query,
        int? limit = null,
        CancellationToken cancellationToken = default)
    {
        return await SearchMembershipCandidatesAsync(
            currentUser,
            customerId,
            query,
            limit,
            cancellationToken);
    }

    public async Task<CustomerServiceResult<IReadOnlyList<UserLookupDto>>> SearchMembershipCandidatesAsync(
        CurrentUserDto currentUser,
        Guid customerId,
        string? query,
        int? limit = null,
        CancellationToken cancellationToken = default)
    {
        var authorization = await authorizationService.AuthorizeAsync(
            currentUser,
            customerId,
            Permission.ManageMemberships,
            cancellationToken);

        if (authorization != CustomerAuthorizationResult.Allowed)
        {
            return CustomerServiceResult<IReadOnlyList<UserLookupDto>>.FromAuthorization(authorization);
        }

        var normalizedQuery = query?.Trim();
        var users = await dbContext.SearchMembershipCandidateUsersAsync(
            customerId,
            normalizedQuery?.Length >= 2 ? normalizedQuery : null,
            Math.Clamp(limit ?? DefaultLimit, 1, MaxLimit),
            cancellationToken);

        return CustomerServiceResult<IReadOnlyList<UserLookupDto>>.Ok(users.Select(MapUser).ToArray());
    }

    private static UserLookupDto MapUser(User user)
    {
        return new UserLookupDto(
            user.Id,
            user.DisplayName,
            user.Email,
            user.IsActive,
            user.IsPlatformAdmin);
    }
}
