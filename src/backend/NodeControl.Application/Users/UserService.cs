using NodeControl.Application.Abstractions.Persistence;
using NodeControl.Application.Auth;
using NodeControl.Application.Customers;
using NodeControl.Domain.Users;

namespace NodeControl.Application.Users;

public sealed class UserService(INodeControlDbContext dbContext)
{
    private const int DefaultLimit = 50;
    private const int MaxLimit = 200;

    public async Task<CustomerServiceResult<IReadOnlyList<UserDto>>> ListUsersAsync(
        CurrentUserDto currentUser,
        UserListQuery query,
        CancellationToken cancellationToken = default)
    {
        if (!currentUser.IsActive || !currentUser.IsPlatformAdmin)
        {
            return CustomerServiceResult<IReadOnlyList<UserDto>>.Forbidden();
        }

        var users = await dbContext.ListUsersAsync(
            query.Query,
            query.IncludeInactive,
            NormalizeLimit(query.Limit),
            cancellationToken);

        var externalIdentities = await dbContext.ListExternalIdentitiesForUsersAsync(
            users.Select(user => user.Id).ToArray(),
            cancellationToken);

        return CustomerServiceResult<IReadOnlyList<UserDto>>.Ok(
            users.Select(user => MapUser(user, externalIdentities)).ToArray());
    }

    public async Task<CustomerServiceResult<UserDto>> GetUserAsync(
        CurrentUserDto currentUser,
        Guid userId,
        CancellationToken cancellationToken = default)
    {
        if (!currentUser.IsActive || !currentUser.IsPlatformAdmin)
        {
            return CustomerServiceResult<UserDto>.Forbidden();
        }

        var user = await dbContext.FindUserAsync(userId, cancellationToken);
        if (user is null)
        {
            return CustomerServiceResult<UserDto>.NotFound();
        }

        var externalIdentities = await dbContext.ListExternalIdentitiesForUsersAsync(
            [user.Id],
            cancellationToken);

        return CustomerServiceResult<UserDto>.Ok(MapUser(user, externalIdentities));
    }

    private static int NormalizeLimit(int? limit)
    {
        return Math.Clamp(limit ?? DefaultLimit, 1, MaxLimit);
    }

    private static UserDto MapUser(
        User user,
        IReadOnlyList<ExternalIdentity> externalIdentities)
    {
        return new UserDto(
            user.Id,
            user.DisplayName,
            user.Email,
            user.IsActive,
            user.IsPlatformAdmin,
            user.CreatedAt,
            user.LastLoginAt,
            externalIdentities
                .Where(externalIdentity => externalIdentity.UserId == user.Id)
                .Select(externalIdentity => new ExternalIdentitySummaryDto(
                    externalIdentity.Provider,
                    externalIdentity.Subject))
                .ToArray());
    }
}
