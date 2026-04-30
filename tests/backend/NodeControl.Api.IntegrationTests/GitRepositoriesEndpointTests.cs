using System.Net;
using System.Net.Http.Json;
using System.Text.Json;
using System.Text.Json.Serialization;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using NodeControl.Application.Abstractions.Persistence;
using NodeControl.Application.GitRepositories;
using NodeControl.Domain.Customers;
using NodeControl.Domain.GitRepositories;
using NodeControl.Domain.Users;

namespace NodeControl.Api.IntegrationTests;

public sealed class GitRepositoriesEndpointTests
{
    [Fact]
    public async Task Get_git_repositories_requires_view_playbooks()
    {
        await using var factory = GitRepositoryApiFactory.Create(CustomerRole.Viewer);
        var seeded = factory.SeedCurrentUserAndCustomer();
        factory.Db.AddGitRepository(GitRepository.Create(seeded.Customer.Id, "Repo", "https://github.com/acme/automation", "main", null, null, TestTime));
        using var client = factory.CreateClient();

        using var response = await client.GetAsync($"/api/v1/customers/{seeded.Customer.Id}/git-repositories");

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
    }

    [Fact]
    public async Task Post_git_repositories_requires_manage_playbooks()
    {
        await using var factory = GitRepositoryApiFactory.Create(CustomerRole.Viewer);
        var seeded = factory.SeedCurrentUserAndCustomer();
        using var client = factory.CreateClient();

        using var response = await client.PostAsJsonAsync(
            $"/api/v1/customers/{seeded.Customer.Id}/git-repositories",
            ValidCreateRequest(),
            JsonOptions);

        Assert.Equal(HttpStatusCode.Forbidden, response.StatusCode);
    }

    [Fact]
    public async Task Manage_playbooks_can_create_update_and_archive_git_repository()
    {
        await using var factory = GitRepositoryApiFactory.Create(CustomerRole.Owner);
        var seeded = factory.SeedCurrentUserAndCustomer();
        using var client = factory.CreateClient();

        using var createResponse = await client.PostAsJsonAsync(
            $"/api/v1/customers/{seeded.Customer.Id}/git-repositories",
            ValidCreateRequest(),
            JsonOptions);
        var repository = await createResponse.Content.ReadFromJsonAsync<GitRepositoryDto>(JsonOptions);
        using var updateResponse = await client.PutAsJsonAsync(
            $"/api/v1/customers/{seeded.Customer.Id}/git-repositories/{repository!.Id}",
            new UpdateGitRepositoryRequest("Repo 2", "git@github.com:acme/automation.git", "develop", "v1.0.0", "playbooks"),
            JsonOptions);
        using var deleteResponse = await client.DeleteAsync(
            $"/api/v1/customers/{seeded.Customer.Id}/git-repositories/{repository.Id}");

        Assert.Equal(HttpStatusCode.Created, createResponse.StatusCode);
        Assert.Equal(HttpStatusCode.OK, updateResponse.StatusCode);
        Assert.Equal(HttpStatusCode.OK, deleteResponse.StatusCode);
    }

    [Fact]
    public async Task Cross_tenant_access_is_rejected_for_detail_update_and_archive()
    {
        await using var factory = GitRepositoryApiFactory.Create(CustomerRole.Owner);
        var seeded = factory.SeedCurrentUserAndCustomer();
        var otherCustomer = Customer.Create("Other Customer", "other-customer", null, TestTime);
        var otherRepository = GitRepository.Create(otherCustomer.Id, "Repo", "https://github.com/acme/other", "main", null, null, TestTime);
        factory.Db.AddCustomer(otherCustomer);
        factory.Db.AddGitRepository(otherRepository);
        using var client = factory.CreateClient();

        using var getResponse = await client.GetAsync(
            $"/api/v1/customers/{seeded.Customer.Id}/git-repositories/{otherRepository.Id}");
        using var putResponse = await client.PutAsJsonAsync(
            $"/api/v1/customers/{seeded.Customer.Id}/git-repositories/{otherRepository.Id}",
            new UpdateGitRepositoryRequest("Repo", "https://github.com/acme/other", "main", null, null),
            JsonOptions);
        using var deleteResponse = await client.DeleteAsync(
            $"/api/v1/customers/{seeded.Customer.Id}/git-repositories/{otherRepository.Id}");

        Assert.Equal(HttpStatusCode.NotFound, getResponse.StatusCode);
        Assert.Equal(HttpStatusCode.NotFound, putResponse.StatusCode);
        Assert.Equal(HttpStatusCode.NotFound, deleteResponse.StatusCode);
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

    private static readonly JsonSerializerOptions JsonOptions = new(JsonSerializerDefaults.Web)
    {
        Converters = { new JsonStringEnumConverter(allowIntegerValues: false) }
    };

    private static DateTimeOffset TestTime => new(2026, 4, 30, 10, 0, 0, TimeSpan.Zero);

    private sealed record Seeded(User User, Customer Customer);

    private sealed class GitRepositoryApiFactory(CustomerRole role) : WebApplicationFactory<Program>
    {
        private const string Provider = "fake";
        private const string Subject = "git-repository-user";
        private const string Email = "git-repository-user@nodecontrol.local";
        private const string DisplayName = "Git Repository User";

        public GitRepositoryApiDbContext Db { get; } = new();

        public static GitRepositoryApiFactory Create(CustomerRole role)
        {
            return new GitRepositoryApiFactory(role);
        }

        public Seeded SeedCurrentUserAndCustomer()
        {
            var user = User.Create(DisplayName, Email, false, TestTime);
            var identity = ExternalIdentity.Create(user, Provider, Subject, Email, DisplayName, TestTime);
            var customer = Customer.Create("Customer A", "customer-a", null, TestTime);
            Db.AddUser(user);
            Db.AddExternalIdentity(identity);
            Db.AddCustomer(customer);
            Db.AddCustomerMembership(CustomerMembership.Create(customer, user, role, TestTime));

            return new Seeded(user, customer);
        }

        protected override void ConfigureWebHost(IWebHostBuilder builder)
        {
            builder.UseEnvironment("Development");

            builder.ConfigureAppConfiguration((_, configurationBuilder) =>
            {
                configurationBuilder.Sources.Clear();
                configurationBuilder.AddInMemoryCollection(new Dictionary<string, string?>
                {
                    ["Auth:Mode"] = "Fake",
                    ["Auth:Fake:Provider"] = Provider,
                    ["Auth:Fake:Subject"] = Subject,
                    ["Auth:Fake:Email"] = Email,
                    ["Auth:Fake:DisplayName"] = DisplayName
                });
            });

            builder.ConfigureServices(services =>
            {
                services.RemoveAll<INodeControlDbContext>();
                services.AddSingleton<INodeControlDbContext>(Db);
            });
        }
    }

    private sealed class GitRepositoryApiDbContext : INodeControlDbContext
    {
        private readonly object syncRoot = new();

        public List<User> Users { get; } = [];

        public List<ExternalIdentity> ExternalIdentities { get; } = [];

        public List<Customer> Customers { get; } = [];

        public List<CustomerMembership> CustomerMemberships { get; } = [];

        public List<GitRepository> GitRepositories { get; } = [];

        public Task<ExternalIdentity?> FindExternalIdentityAsync(string provider, string subject, CancellationToken cancellationToken)
        {
            lock (syncRoot)
            {
                return Task.FromResult(ExternalIdentities.FirstOrDefault(identity =>
                    identity.Provider == provider && identity.Subject == subject));
            }
        }

        public Task<User?> FindUserAsync(Guid id, CancellationToken cancellationToken)
        {
            lock (syncRoot)
            {
                return Task.FromResult(Users.FirstOrDefault(user => user.Id == id));
            }
        }

        public Task<IReadOnlyList<Customer>> ListActiveCustomersAsync(CancellationToken cancellationToken)
        {
            lock (syncRoot)
            {
                return Task.FromResult<IReadOnlyList<Customer>>(
                    Customers.Where(customer => customer.Status == CustomerStatus.Active).ToArray());
            }
        }

        public Task<IReadOnlyList<Customer>> ListActiveCustomersForUserAsync(Guid userId, CancellationToken cancellationToken)
        {
            lock (syncRoot)
            {
                return Task.FromResult<IReadOnlyList<Customer>>(
                    CustomerMemberships
                        .Where(membership => membership.UserId == userId
                            && membership.IsActive
                            && membership.Customer.Status == CustomerStatus.Active)
                        .Select(membership => membership.Customer)
                        .ToArray());
            }
        }

        public Task<Customer?> FindCustomerAsync(Guid id, CancellationToken cancellationToken)
        {
            lock (syncRoot)
            {
                return Task.FromResult(Customers.FirstOrDefault(customer => customer.Id == id));
            }
        }

        public Task<IReadOnlyList<CustomerMembership>> ListCustomerMembershipsAsync(Guid customerId, CancellationToken cancellationToken)
        {
            lock (syncRoot)
            {
                return Task.FromResult<IReadOnlyList<CustomerMembership>>(
                    CustomerMemberships.Where(membership => membership.CustomerId == customerId).ToArray());
            }
        }

        public Task<CustomerMembership?> FindCustomerMembershipAsync(Guid id, CancellationToken cancellationToken)
        {
            lock (syncRoot)
            {
                return Task.FromResult(CustomerMemberships.FirstOrDefault(membership => membership.Id == id));
            }
        }

        public Task<CustomerMembership?> FindCustomerMembershipForUserAsync(Guid customerId, Guid userId, CancellationToken cancellationToken)
        {
            lock (syncRoot)
            {
                return Task.FromResult(CustomerMemberships.FirstOrDefault(membership =>
                    membership.CustomerId == customerId && membership.UserId == userId));
            }
        }

        public Task<IReadOnlyList<GitRepository>> ListActiveGitRepositoriesAsync(Guid customerId, CancellationToken cancellationToken)
        {
            lock (syncRoot)
            {
                return Task.FromResult<IReadOnlyList<GitRepository>>(
                    GitRepositories
                        .Where(repository => repository.CustomerId == customerId && repository.Status == GitRepositoryStatus.Active)
                        .ToArray());
            }
        }

        public Task<GitRepository?> FindGitRepositoryAsync(Guid customerId, Guid gitRepositoryId, CancellationToken cancellationToken)
        {
            lock (syncRoot)
            {
                return Task.FromResult(GitRepositories.FirstOrDefault(repository =>
                    repository.CustomerId == customerId && repository.Id == gitRepositoryId));
            }
        }

        public void AddUser(User user)
        {
            lock (syncRoot)
            {
                Users.Add(user);
            }
        }

        public void AddExternalIdentity(ExternalIdentity externalIdentity)
        {
            lock (syncRoot)
            {
                ExternalIdentities.Add(externalIdentity);
            }
        }

        public void AddCustomer(Customer customer)
        {
            lock (syncRoot)
            {
                Customers.Add(customer);
            }
        }

        public void AddCustomerMembership(CustomerMembership customerMembership)
        {
            lock (syncRoot)
            {
                CustomerMemberships.Add(customerMembership);
            }
        }

        public void AddGitRepository(GitRepository gitRepository)
        {
            lock (syncRoot)
            {
                GitRepositories.Add(gitRepository);
            }
        }

        public Task<int> SaveChangesAsync(CancellationToken cancellationToken)
        {
            return Task.FromResult(1);
        }
    }
}
