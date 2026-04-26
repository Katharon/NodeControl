using NodeControl.Application.Abstractions.Authorization;
using NodeControl.Application.Abstractions.Persistence;
using NodeControl.Application.Authorization;
using NodeControl.Application.Auth;
using NodeControl.Domain.Authorization;
using NodeControl.Domain.Customers;
using NodeControl.Domain.Users;

namespace NodeControl.Application.Tests;

public sealed class CustomerAuthorizationServiceTests
{
    [Fact]
    public async Task Membership_in_one_customer_does_not_grant_access_to_another_customer()
    {
        var context = new TestNodeControlDbContext();
        var user = User.Create("Normal User", "normal@nodecontrol.local", false, TestTime);
        var customerA = Customer.Create("Customer A", "customer-a", null, TestTime);
        var customerB = Customer.Create("Customer B", "customer-b", null, TestTime);
        context.AddUser(user);
        context.AddCustomer(customerA);
        context.AddCustomer(customerB);
        context.AddCustomerMembership(CustomerMembership.Create(customerA, user, CustomerRole.Owner, TestTime));

        var service = new CustomerAuthorizationService(context);
        var currentUser = CurrentUser(user);

        var result = await service.AuthorizeAsync(
            currentUser,
            customerB.Id,
            Permission.ViewCustomer,
            CancellationToken.None);

        Assert.Equal(CustomerAuthorizationResult.Forbidden, result);
    }

    [Fact]
    public async Task Inactive_membership_grants_no_access()
    {
        var context = new TestNodeControlDbContext();
        var user = User.Create("Normal User", "normal@nodecontrol.local", false, TestTime);
        var customer = Customer.Create("Customer A", "customer-a", null, TestTime);
        var membership = CustomerMembership.Create(customer, user, CustomerRole.Owner, TestTime);
        membership.Deactivate(TestTime.AddMinutes(1));
        context.AddUser(user);
        context.AddCustomer(customer);
        context.AddCustomerMembership(membership);

        var service = new CustomerAuthorizationService(context);
        var result = await service.AuthorizeAsync(
            CurrentUser(user),
            customer.Id,
            Permission.ViewCustomer,
            CancellationToken.None);

        Assert.Equal(CustomerAuthorizationResult.Forbidden, result);
    }

    private static DateTimeOffset TestTime => new(2026, 4, 27, 8, 0, 0, TimeSpan.Zero);

    private static CurrentUserDto CurrentUser(User user)
    {
        return new CurrentUserDto(
            user.Id,
            user.DisplayName,
            user.Email,
            user.IsActive,
            user.IsPlatformAdmin,
            "fake",
            user.Id.ToString());
    }

    private sealed class TestNodeControlDbContext : INodeControlDbContext
    {
        private readonly List<Customer> customers = [];
        private readonly List<CustomerMembership> memberships = [];
        private readonly List<User> users = [];

        public Task<ExternalIdentity?> FindExternalIdentityAsync(
            string provider,
            string subject,
            CancellationToken cancellationToken)
        {
            return Task.FromResult<ExternalIdentity?>(null);
        }

        public Task<User?> FindUserAsync(Guid id, CancellationToken cancellationToken)
        {
            return Task.FromResult(users.FirstOrDefault(user => user.Id == id));
        }

        public Task<IReadOnlyList<Customer>> ListActiveCustomersAsync(CancellationToken cancellationToken)
        {
            return Task.FromResult<IReadOnlyList<Customer>>(
                customers.Where(customer => customer.Status == CustomerStatus.Active).ToArray());
        }

        public Task<IReadOnlyList<Customer>> ListActiveCustomersForUserAsync(
            Guid userId,
            CancellationToken cancellationToken)
        {
            return Task.FromResult<IReadOnlyList<Customer>>(
                memberships
                    .Where(membership => membership.UserId == userId
                        && membership.IsActive
                        && membership.Customer.Status == CustomerStatus.Active)
                    .Select(membership => membership.Customer)
                    .ToArray());
        }

        public Task<Customer?> FindCustomerAsync(Guid id, CancellationToken cancellationToken)
        {
            return Task.FromResult(customers.FirstOrDefault(customer => customer.Id == id));
        }

        public Task<IReadOnlyList<CustomerMembership>> ListCustomerMembershipsAsync(
            Guid customerId,
            CancellationToken cancellationToken)
        {
            return Task.FromResult<IReadOnlyList<CustomerMembership>>(
                memberships.Where(membership => membership.CustomerId == customerId).ToArray());
        }

        public Task<CustomerMembership?> FindCustomerMembershipAsync(Guid id, CancellationToken cancellationToken)
        {
            return Task.FromResult(memberships.FirstOrDefault(membership => membership.Id == id));
        }

        public Task<CustomerMembership?> FindCustomerMembershipForUserAsync(
            Guid customerId,
            Guid userId,
            CancellationToken cancellationToken)
        {
            return Task.FromResult(memberships.FirstOrDefault(membership =>
                membership.CustomerId == customerId && membership.UserId == userId));
        }

        public void AddUser(User user)
        {
            users.Add(user);
        }

        public void AddExternalIdentity(ExternalIdentity externalIdentity)
        {
        }

        public void AddCustomer(Customer customer)
        {
            customers.Add(customer);
        }

        public void AddCustomerMembership(CustomerMembership customerMembership)
        {
            memberships.Add(customerMembership);
        }

        public Task<int> SaveChangesAsync(CancellationToken cancellationToken)
        {
            return Task.FromResult(1);
        }
    }
}
