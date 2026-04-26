using NodeControl.Application.Abstractions.Persistence;
using NodeControl.Application.Abstractions.Time;
using NodeControl.Application.Auth;
using NodeControl.Domain.Customers;
using NodeControl.Domain.Users;

namespace NodeControl.Application.Tests;

public sealed class UserProvisioningServiceTests
{
    [Fact]
    public async Task Creates_user_for_new_external_identity()
    {
        var clock = new TestClock(new DateTimeOffset(2026, 4, 26, 12, 0, 0, TimeSpan.Zero));
        var dbContext = new TestNodeControlDbContext();
        var service = new UserProvisioningService(dbContext, clock);

        var user = await service.ProvisionUserAsync(new ExternalUserInfo(
            "fake",
            "dev-admin",
            "dev-admin@nodecontrol.local",
            "Dev Admin",
            true));

        Assert.NotEqual(Guid.Empty, user.Id);
        Assert.Equal("Dev Admin", user.DisplayName);
        Assert.Equal("dev-admin@nodecontrol.local", user.Email);
        Assert.Equal("DEV-ADMIN@NODECONTROL.LOCAL", user.NormalizedEmail);
        Assert.True(user.IsActive);
        Assert.True(user.IsPlatformAdmin);
        Assert.Single(dbContext.Users);
        Assert.Single(dbContext.ExternalIdentities);
    }

    [Fact]
    public async Task Reuses_user_for_existing_external_identity()
    {
        var clock = new TestClock(new DateTimeOffset(2026, 4, 26, 12, 0, 0, TimeSpan.Zero));
        var dbContext = new TestNodeControlDbContext();
        var service = new UserProvisioningService(dbContext, clock);

        var firstUser = await service.ProvisionUserAsync(new ExternalUserInfo(
            "fake",
            "dev-admin",
            "dev-admin@nodecontrol.local",
            "Dev Admin"));

        clock.UtcNow = clock.UtcNow.AddMinutes(5);

        var secondUser = await service.ProvisionUserAsync(new ExternalUserInfo(
            "fake",
            "dev-admin",
            "dev-admin@nodecontrol.local",
            "Dev Admin"));

        Assert.Equal(firstUser.Id, secondUser.Id);
        Assert.Single(dbContext.Users);
        Assert.Single(dbContext.ExternalIdentities);
    }

    [Fact]
    public async Task Updates_last_seen_at_and_last_login_at_on_repeated_login()
    {
        var firstLoginAt = new DateTimeOffset(2026, 4, 26, 12, 0, 0, TimeSpan.Zero);
        var secondLoginAt = firstLoginAt.AddMinutes(10);
        var clock = new TestClock(firstLoginAt);
        var dbContext = new TestNodeControlDbContext();
        var service = new UserProvisioningService(dbContext, clock);

        var user = await service.ProvisionUserAsync(new ExternalUserInfo(
            "fake",
            "dev-admin",
            "dev-admin@nodecontrol.local",
            "Dev Admin"));

        clock.UtcNow = secondLoginAt;

        await service.ProvisionUserAsync(new ExternalUserInfo(
            "fake",
            "dev-admin",
            "dev-admin@nodecontrol.local",
            "Dev Admin"));

        var externalIdentity = Assert.Single(dbContext.ExternalIdentities);
        Assert.Equal(secondLoginAt, externalIdentity.LastSeenAt);
        Assert.Equal(secondLoginAt, user.LastLoginAt);
    }

    private sealed class TestClock(DateTimeOffset utcNow) : IClock
    {
        public DateTimeOffset UtcNow { get; set; } = utcNow;
    }

    private sealed class TestNodeControlDbContext : INodeControlDbContext
    {
        public List<User> Users { get; } = [];

        public List<ExternalIdentity> ExternalIdentities { get; } = [];

        public Task<ExternalIdentity?> FindExternalIdentityAsync(
            string provider,
            string subject,
            CancellationToken cancellationToken)
        {
            return Task.FromResult(ExternalIdentities.FirstOrDefault(externalIdentity =>
                externalIdentity.Provider == provider && externalIdentity.Subject == subject));
        }

        public Task<User?> FindUserAsync(Guid id, CancellationToken cancellationToken)
        {
            return Task.FromResult(Users.FirstOrDefault(user => user.Id == id));
        }

        public Task<IReadOnlyList<Customer>> ListActiveCustomersAsync(CancellationToken cancellationToken)
        {
            return Task.FromResult<IReadOnlyList<Customer>>([]);
        }

        public Task<IReadOnlyList<Customer>> ListActiveCustomersForUserAsync(
            Guid userId,
            CancellationToken cancellationToken)
        {
            return Task.FromResult<IReadOnlyList<Customer>>([]);
        }

        public Task<Customer?> FindCustomerAsync(Guid id, CancellationToken cancellationToken)
        {
            return Task.FromResult<Customer?>(null);
        }

        public Task<IReadOnlyList<CustomerMembership>> ListCustomerMembershipsAsync(
            Guid customerId,
            CancellationToken cancellationToken)
        {
            return Task.FromResult<IReadOnlyList<CustomerMembership>>([]);
        }

        public Task<CustomerMembership?> FindCustomerMembershipAsync(
            Guid id,
            CancellationToken cancellationToken)
        {
            return Task.FromResult<CustomerMembership?>(null);
        }

        public Task<CustomerMembership?> FindCustomerMembershipForUserAsync(
            Guid customerId,
            Guid userId,
            CancellationToken cancellationToken)
        {
            return Task.FromResult<CustomerMembership?>(null);
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
        }

        public void AddCustomerMembership(CustomerMembership customerMembership)
        {
        }

        public Task<int> SaveChangesAsync(CancellationToken cancellationToken)
        {
            return Task.FromResult(1);
        }
    }
}
