using NodeControl.Application.Abstractions.Persistence;
using NodeControl.Application.Abstractions.Time;
using NodeControl.Domain.Users;

namespace NodeControl.Application.Auth;

public sealed class UserProvisioningService(
    INodeControlDbContext dbContext,
    IClock clock)
{
    public async Task<User> ProvisionUserAsync(
        ExternalUserInfo externalUser,
        CancellationToken cancellationToken = default)
    {
        Validate(externalUser);

        var now = clock.UtcNow;
        var provider = externalUser.Provider.Trim();
        var subject = externalUser.Subject.Trim();
        var email = externalUser.Email.Trim();
        var displayName = externalUser.DisplayName.Trim();

        var externalIdentity = await dbContext.FindExternalIdentityAsync(provider, subject, cancellationToken);
        if (externalIdentity is not null)
        {
            externalIdentity.RecordSeen(email, displayName, now);
            externalIdentity.User.RecordLogin(displayName, email, now);
            await dbContext.SaveChangesAsync(cancellationToken);

            return externalIdentity.User;
        }

        var user = User.Create(displayName, email, externalUser.IsPlatformAdmin, now);
        var newExternalIdentity = ExternalIdentity.Create(user, provider, subject, email, displayName, now);

        dbContext.AddUser(user);
        dbContext.AddExternalIdentity(newExternalIdentity);
        await dbContext.SaveChangesAsync(cancellationToken);

        return user;
    }

    private static void Validate(ExternalUserInfo externalUser)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(externalUser.Provider);
        ArgumentException.ThrowIfNullOrWhiteSpace(externalUser.Subject);
        ArgumentException.ThrowIfNullOrWhiteSpace(externalUser.Email);
        ArgumentException.ThrowIfNullOrWhiteSpace(externalUser.DisplayName);
    }
}
