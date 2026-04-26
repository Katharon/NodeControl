using NodeControl.Application.Abstractions.Auth;

namespace NodeControl.Application.Auth;

public sealed class CurrentUserService(
    ICurrentUser currentUser,
    UserProvisioningService userProvisioningService)
{
    public async Task<CurrentUserDto?> GetCurrentUserAsync(CancellationToken cancellationToken = default)
    {
        if (!currentUser.IsAuthenticated)
        {
            return null;
        }

        if (string.IsNullOrWhiteSpace(currentUser.AuthProvider)
            || string.IsNullOrWhiteSpace(currentUser.ExternalSubject)
            || string.IsNullOrWhiteSpace(currentUser.Email)
            || string.IsNullOrWhiteSpace(currentUser.DisplayName))
        {
            return null;
        }

        var externalUser = new ExternalUserInfo(
            currentUser.AuthProvider,
            currentUser.ExternalSubject,
            currentUser.Email,
            currentUser.DisplayName,
            currentUser.IsPlatformAdmin);

        var user = await userProvisioningService.ProvisionUserAsync(externalUser, cancellationToken);
        if (!user.IsActive)
        {
            return null;
        }

        return new CurrentUserDto(
            user.Id,
            user.DisplayName,
            user.Email,
            user.IsActive,
            user.IsPlatformAdmin,
            externalUser.Provider,
            externalUser.Subject);
    }
}
