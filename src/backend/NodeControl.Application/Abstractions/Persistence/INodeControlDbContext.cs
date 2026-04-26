using NodeControl.Domain.Users;

namespace NodeControl.Application.Abstractions.Persistence;

public interface INodeControlDbContext
{
    Task<ExternalIdentity?> FindExternalIdentityAsync(
        string provider,
        string subject,
        CancellationToken cancellationToken);

    Task<User?> FindUserAsync(Guid id, CancellationToken cancellationToken);

    void AddUser(User user);

    void AddExternalIdentity(ExternalIdentity externalIdentity);

    Task<int> SaveChangesAsync(CancellationToken cancellationToken);
}
