using Microsoft.EntityFrameworkCore;
using NodeControl.Application.Abstractions.Persistence;
using NodeControl.Domain.Users;

namespace NodeControl.Infrastructure.Persistence;

public sealed class NodeControlDbContext(DbContextOptions<NodeControlDbContext> options)
    : DbContext(options), INodeControlDbContext
{
    public DbSet<User> Users => Set<User>();

    public DbSet<ExternalIdentity> ExternalIdentities => Set<ExternalIdentity>();

    public async Task<ExternalIdentity?> FindExternalIdentityAsync(
        string provider,
        string subject,
        CancellationToken cancellationToken)
    {
        return await ExternalIdentities
            .Include(externalIdentity => externalIdentity.User)
            .FirstOrDefaultAsync(
                externalIdentity => externalIdentity.Provider == provider
                    && externalIdentity.Subject == subject,
                cancellationToken);
    }

    public async Task<User?> FindUserAsync(Guid id, CancellationToken cancellationToken)
    {
        return await Users.FirstOrDefaultAsync(user => user.Id == id, cancellationToken);
    }

    public void AddUser(User user)
    {
        Users.Add(user);
    }

    public void AddExternalIdentity(ExternalIdentity externalIdentity)
    {
        ExternalIdentities.Add(externalIdentity);
    }

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.ApplyConfigurationsFromAssembly(typeof(NodeControlDbContext).Assembly);
    }
}
