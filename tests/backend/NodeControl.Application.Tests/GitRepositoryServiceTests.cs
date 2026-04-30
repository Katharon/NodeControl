using NodeControl.Application.Abstractions.Time;
using NodeControl.Application.Auth;
using NodeControl.Application.Authorization;
using NodeControl.Application.Customers;
using NodeControl.Application.GitRepositories;
using NodeControl.Domain.Customers;
using NodeControl.Domain.GitRepositories;
using NodeControl.Domain.Users;

namespace NodeControl.Application.Tests;

public sealed class GitRepositoryServiceTests
{
    [Fact]
    public async Task GitRepositoryService_creates_repository_when_user_has_manage_playbooks()
    {
        var fixture = TestFixture.Create(CustomerRole.Owner);

        var result = await fixture.CreateService().CreateAsync(
            fixture.CurrentUser,
            fixture.Customer.Id,
            ValidCreateRequest());

        Assert.Null(result.Error);
        Assert.Single(fixture.Db.GitRepositories);
        Assert.Equal("ansible/playbooks", result.Value!.Subpath);
    }

    [Fact]
    public async Task GitRepositoryService_rejects_create_when_user_lacks_manage_playbooks()
    {
        var fixture = TestFixture.Create(CustomerRole.Viewer);

        var result = await fixture.CreateService().CreateAsync(
            fixture.CurrentUser,
            fixture.Customer.Id,
            ValidCreateRequest());

        Assert.Equal(CustomerServiceError.Forbidden, result.Error);
        Assert.Empty(fixture.Db.GitRepositories);
    }

    [Theory]
    [InlineData("not a url")]
    [InlineData("ftp://github.com/acme/automation")]
    public async Task GitRepositoryService_rejects_invalid_repository_url(string repositoryUrl)
    {
        var fixture = TestFixture.Create(CustomerRole.Owner);

        var result = await fixture.CreateService().CreateAsync(
            fixture.CurrentUser,
            fixture.Customer.Id,
            ValidCreateRequest() with { RepositoryUrl = repositoryUrl });

        Assert.Equal(CustomerServiceError.BadRequest, result.Error);
    }

    [Theory]
    [InlineData("../playbooks")]
    [InlineData("/playbooks")]
    [InlineData("playbooks/")]
    public async Task GitRepositoryService_rejects_unsafe_subpaths(string subpath)
    {
        var fixture = TestFixture.Create(CustomerRole.Owner);

        var result = await fixture.CreateService().CreateAsync(
            fixture.CurrentUser,
            fixture.Customer.Id,
            ValidCreateRequest() with { Subpath = subpath });

        Assert.Equal(CustomerServiceError.BadRequest, result.Error);
    }

    [Fact]
    public async Task GitRepositoryService_rejects_cross_tenant_detail_access()
    {
        var fixture = TestFixture.Create(CustomerRole.Owner);
        var other = fixture.AddOtherCustomer();
        var repository = GitRepository.Create(
            other.Customer.Id,
            "Other Repo",
            "https://github.com/acme/other",
            "main",
            null,
            null,
            TestTime);
        fixture.Db.AddGitRepository(repository);

        var result = await fixture.CreateService().GetAsync(
            fixture.CurrentUser,
            fixture.Customer.Id,
            repository.Id);

        Assert.Equal(CustomerServiceError.NotFound, result.Error);
    }

    [Fact]
    public async Task GitRepositoryService_archives_instead_of_hard_deleting()
    {
        var fixture = TestFixture.Create(CustomerRole.Owner);
        var create = await fixture.CreateService().CreateAsync(
            fixture.CurrentUser,
            fixture.Customer.Id,
            ValidCreateRequest());

        var result = await fixture.CreateService().ArchiveAsync(
            fixture.CurrentUser,
            fixture.Customer.Id,
            create.Value!.Id);

        Assert.Null(result.Error);
        Assert.Single(fixture.Db.GitRepositories);
        Assert.Equal(GitRepositoryStatus.Archived, fixture.Db.GitRepositories[0].Status);
    }

    private static CreateGitRepositoryRequest ValidCreateRequest()
    {
        return new CreateGitRepositoryRequest(
            "Automation Repo",
            "https://github.com/acme/automation",
            "main",
            null,
            "ansible/playbooks");
    }

    private sealed record CustomerUser(Customer Customer, CurrentUserDto CurrentUser);

    private sealed class TestFixture
    {
        private readonly TestClock clock = new();

        private TestFixture(NodeControlTestDbContext db, Customer customer, CurrentUserDto currentUser)
        {
            Db = db;
            Customer = customer;
            CurrentUser = currentUser;
        }

        public NodeControlTestDbContext Db { get; }

        public Customer Customer { get; }

        public CurrentUserDto CurrentUser { get; }

        public static TestFixture Create(CustomerRole role)
        {
            var db = new NodeControlTestDbContext();
            var user = User.Create("Test User", "test@nodecontrol.local", false, TestTime);
            var customer = Customer.Create("Customer A", "customer-a", null, TestTime);
            db.AddUser(user);
            db.AddCustomer(customer);
            db.AddCustomerMembership(CustomerMembership.Create(customer, user, role, TestTime));

            return new TestFixture(db, customer, CurrentUser(user));
        }

        public CustomerUser AddOtherCustomer()
        {
            var user = User.Create("Other User", "other@nodecontrol.local", false, TestTime);
            var customer = Customer.Create("Customer B", "customer-b", null, TestTime);
            Db.AddUser(user);
            Db.AddCustomer(customer);
            Db.AddCustomerMembership(CustomerMembership.Create(customer, user, CustomerRole.Owner, TestTime));
            return new CustomerUser(customer, CurrentUser(user));
        }

        public GitRepositoryService CreateService()
        {
            return new GitRepositoryService(Db, new CustomerAuthorizationService(Db), clock);
        }
    }

    private sealed class TestClock : IClock
    {
        public DateTimeOffset UtcNow { get; set; } = TestTime;
    }

    private static DateTimeOffset TestTime => new(2026, 4, 30, 10, 0, 0, TimeSpan.Zero);

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
}
