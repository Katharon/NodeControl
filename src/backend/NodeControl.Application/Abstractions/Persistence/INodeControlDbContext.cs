using NodeControl.Domain.Users;
using NodeControl.Domain.Customers;

namespace NodeControl.Application.Abstractions.Persistence;

public interface INodeControlDbContext
{
    Task<ExternalIdentity?> FindExternalIdentityAsync(
        string provider,
        string subject,
        CancellationToken cancellationToken);

    Task<User?> FindUserAsync(Guid id, CancellationToken cancellationToken);

    Task<IReadOnlyList<Customer>> ListActiveCustomersAsync(CancellationToken cancellationToken);

    Task<IReadOnlyList<Customer>> ListActiveCustomersForUserAsync(Guid userId, CancellationToken cancellationToken);

    Task<Customer?> FindCustomerAsync(Guid id, CancellationToken cancellationToken);

    Task<IReadOnlyList<CustomerMembership>> ListCustomerMembershipsAsync(
        Guid customerId,
        CancellationToken cancellationToken);

    Task<CustomerMembership?> FindCustomerMembershipAsync(Guid id, CancellationToken cancellationToken);

    Task<CustomerMembership?> FindCustomerMembershipForUserAsync(
        Guid customerId,
        Guid userId,
        CancellationToken cancellationToken);

    void AddUser(User user);

    void AddExternalIdentity(ExternalIdentity externalIdentity);

    void AddCustomer(Customer customer);

    void AddCustomerMembership(CustomerMembership customerMembership);

    Task<int> SaveChangesAsync(CancellationToken cancellationToken);
}
