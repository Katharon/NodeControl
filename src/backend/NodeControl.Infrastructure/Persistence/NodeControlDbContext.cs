using Microsoft.EntityFrameworkCore;
using NodeControl.Application.Abstractions.Persistence;
using NodeControl.Domain.Customers;
using NodeControl.Domain.Users;

namespace NodeControl.Infrastructure.Persistence;

public sealed class NodeControlDbContext(DbContextOptions<NodeControlDbContext> options)
    : DbContext(options), INodeControlDbContext
{
    public DbSet<User> Users => Set<User>();

    public DbSet<ExternalIdentity> ExternalIdentities => Set<ExternalIdentity>();

    public DbSet<Customer> Customers => Set<Customer>();

    public DbSet<CustomerMembership> CustomerMemberships => Set<CustomerMembership>();

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

    public async Task<IReadOnlyList<Customer>> ListActiveCustomersAsync(CancellationToken cancellationToken)
    {
        return await Customers
            .Where(customer => customer.Status == CustomerStatus.Active)
            .OrderBy(customer => customer.Name)
            .ToListAsync(cancellationToken);
    }

    public async Task<IReadOnlyList<Customer>> ListActiveCustomersForUserAsync(
        Guid userId,
        CancellationToken cancellationToken)
    {
        return await CustomerMemberships
            .Where(membership => membership.UserId == userId
                && membership.IsActive
                && membership.Customer.Status == CustomerStatus.Active)
            .Select(membership => membership.Customer)
            .OrderBy(customer => customer.Name)
            .ToListAsync(cancellationToken);
    }

    public async Task<Customer?> FindCustomerAsync(Guid id, CancellationToken cancellationToken)
    {
        return await Customers.FirstOrDefaultAsync(customer => customer.Id == id, cancellationToken);
    }

    public async Task<IReadOnlyList<CustomerMembership>> ListCustomerMembershipsAsync(
        Guid customerId,
        CancellationToken cancellationToken)
    {
        return await CustomerMemberships
            .Include(membership => membership.User)
            .Where(membership => membership.CustomerId == customerId)
            .OrderBy(membership => membership.User.DisplayName)
            .ToListAsync(cancellationToken);
    }

    public async Task<CustomerMembership?> FindCustomerMembershipAsync(
        Guid id,
        CancellationToken cancellationToken)
    {
        return await CustomerMemberships
            .Include(membership => membership.Customer)
            .Include(membership => membership.User)
            .FirstOrDefaultAsync(membership => membership.Id == id, cancellationToken);
    }

    public async Task<CustomerMembership?> FindCustomerMembershipForUserAsync(
        Guid customerId,
        Guid userId,
        CancellationToken cancellationToken)
    {
        return await CustomerMemberships
            .Include(membership => membership.Customer)
            .Include(membership => membership.User)
            .FirstOrDefaultAsync(
                membership => membership.CustomerId == customerId && membership.UserId == userId,
                cancellationToken);
    }

    public void AddUser(User user)
    {
        Users.Add(user);
    }

    public void AddExternalIdentity(ExternalIdentity externalIdentity)
    {
        ExternalIdentities.Add(externalIdentity);
    }

    public void AddCustomer(Customer customer)
    {
        Customers.Add(customer);
    }

    public void AddCustomerMembership(CustomerMembership customerMembership)
    {
        CustomerMemberships.Add(customerMembership);
    }

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.ApplyConfigurationsFromAssembly(typeof(NodeControlDbContext).Assembly);
    }
}
