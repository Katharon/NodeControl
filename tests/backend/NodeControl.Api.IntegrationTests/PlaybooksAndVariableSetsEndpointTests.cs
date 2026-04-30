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
using NodeControl.Application.Playbooks;
using NodeControl.Application.VariableSets;
using NodeControl.Domain.Customers;
using NodeControl.Domain.Playbooks;
using NodeControl.Domain.Users;
using NodeControl.Domain.VariableSets;

namespace NodeControl.Api.IntegrationTests;

public sealed class PlaybooksAndVariableSetsEndpointTests
{
    [Fact]
    public async Task Get_playbooks_requires_view_playbooks()
    {
        await using var factory = DefinitionApiFactory.Create(CustomerRole.Viewer);
        var seeded = factory.SeedCurrentUserAndCustomer();
        factory.Db.AddPlaybook(Playbook.Create(
            seeded.Customer.Id,
            "Deploy App",
            "deploy-app",
            null,
            PlaybookSourceType.InlineYaml,
            ValidYaml,
            null,
            TestTime));
        using var client = factory.CreateClient();

        using var response = await client.GetAsync($"/api/v1/customers/{seeded.Customer.Id}/playbooks");

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
    }

    [Fact]
    public async Task Post_playbooks_requires_manage_playbooks()
    {
        await using var factory = DefinitionApiFactory.Create(CustomerRole.Viewer);
        var seeded = factory.SeedCurrentUserAndCustomer();
        using var client = factory.CreateClient();

        using var response = await client.PostAsJsonAsync(
            $"/api/v1/customers/{seeded.Customer.Id}/playbooks",
            ValidPlaybookRequest("deploy-app"),
            JsonOptions);

        Assert.Equal(HttpStatusCode.Forbidden, response.StatusCode);
    }

    [Fact]
    public async Task Get_playbook_by_id_rejects_cross_tenant_access()
    {
        await using var factory = DefinitionApiFactory.Create(CustomerRole.Owner);
        var seeded = factory.SeedCurrentUserAndCustomer();
        var otherCustomer = Customer.Create("Other Customer", "other-customer", null, TestTime);
        var otherPlaybook = Playbook.Create(otherCustomer.Id, "Deploy App", "deploy-app", null, PlaybookSourceType.InlineYaml, ValidYaml, null, TestTime);
        factory.Db.AddCustomer(otherCustomer);
        factory.Db.AddPlaybook(otherPlaybook);
        using var client = factory.CreateClient();

        using var response = await client.GetAsync(
            $"/api/v1/customers/{seeded.Customer.Id}/playbooks/{otherPlaybook.Id}");

        Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
    }

    [Fact]
    public async Task Get_variable_set_by_id_rejects_cross_tenant_access()
    {
        await using var factory = DefinitionApiFactory.Create(CustomerRole.Owner);
        var seeded = factory.SeedCurrentUserAndCustomer();
        var otherCustomer = Customer.Create("Other Customer", "other-customer", null, TestTime);
        var otherVariableSet = VariableSet.Create(otherCustomer.Id, "Defaults", "defaults", null, VariableSetFormat.Yaml, "package_name: nginx\n", false, TestTime);
        factory.Db.AddCustomer(otherCustomer);
        factory.Db.AddVariableSet(otherVariableSet);
        using var client = factory.CreateClient();

        using var response = await client.GetAsync(
            $"/api/v1/customers/{seeded.Customer.Id}/variable-sets/{otherVariableSet.Id}");

        Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
    }

    [Fact]
    public async Task View_playbooks_cannot_update_or_archive_resources()
    {
        await using var factory = DefinitionApiFactory.Create(CustomerRole.Viewer);
        var seeded = factory.SeedCurrentUserAndCustomer();
        var playbook = Playbook.Create(seeded.Customer.Id, "Deploy App", "deploy-app", null, PlaybookSourceType.InlineYaml, ValidYaml, null, TestTime);
        factory.Db.AddPlaybook(playbook);
        using var client = factory.CreateClient();

        using var putResponse = await client.PutAsJsonAsync(
            $"/api/v1/customers/{seeded.Customer.Id}/playbooks/{playbook.Id}",
            new UpdatePlaybookRequest("Deploy App", "deploy-app", null, PlaybookSourceType.InlineYaml, ValidYaml, null),
            JsonOptions);
        using var deleteResponse = await client.DeleteAsync(
            $"/api/v1/customers/{seeded.Customer.Id}/playbooks/{playbook.Id}");

        Assert.Equal(HttpStatusCode.Forbidden, putResponse.StatusCode);
        Assert.Equal(HttpStatusCode.Forbidden, deleteResponse.StatusCode);
    }

    [Fact]
    public async Task Manage_playbooks_can_create_update_and_archive_resources()
    {
        await using var factory = DefinitionApiFactory.Create(CustomerRole.Owner);
        var seeded = factory.SeedCurrentUserAndCustomer();
        using var client = factory.CreateClient();

        using var createResponse = await client.PostAsJsonAsync(
            $"/api/v1/customers/{seeded.Customer.Id}/playbooks",
            ValidPlaybookRequest("deploy-app"),
            JsonOptions);
        var playbook = await createResponse.Content.ReadFromJsonAsync<PlaybookDto>(JsonOptions);

        using var updateResponse = await client.PutAsJsonAsync(
            $"/api/v1/customers/{seeded.Customer.Id}/playbooks/{playbook!.Id}",
            new UpdatePlaybookRequest("Deploy App 2", "deploy-app-2", null, PlaybookSourceType.InlineYaml, ValidYaml, null),
            JsonOptions);
        using var deleteResponse = await client.DeleteAsync(
            $"/api/v1/customers/{seeded.Customer.Id}/playbooks/{playbook.Id}");

        Assert.Equal(HttpStatusCode.Created, createResponse.StatusCode);
        Assert.Equal(HttpStatusCode.OK, updateResponse.StatusCode);
        Assert.Equal(HttpStatusCode.OK, deleteResponse.StatusCode);
    }

    [Fact]
    public async Task Post_playbook_rejects_invalid_artifact_entry_and_duplicate_paths()
    {
        await using var factory = DefinitionApiFactory.Create(CustomerRole.Owner);
        var seeded = factory.SeedCurrentUserAndCustomer();
        using var client = factory.CreateClient();

        using var missingEntryResponse = await client.PostAsJsonAsync(
            $"/api/v1/customers/{seeded.Customer.Id}/playbooks",
            ValidArtifactPlaybookRequest("missing-entry") with { EntryFilePath = "missing.yml" },
            JsonOptions);
        using var duplicatePathResponse = await client.PostAsJsonAsync(
            $"/api/v1/customers/{seeded.Customer.Id}/playbooks",
            ValidArtifactPlaybookRequest("duplicate-path") with
            {
                ArtifactFiles =
                [
                    new PlaybookArtifactFileDto("site.yml", "- hosts: all\n"),
                    new PlaybookArtifactFileDto("roles/app/tasks/main.yml", "- debug:\n    msg: hello\n"),
                    new PlaybookArtifactFileDto("roles\\app\\tasks\\main.yml", "- debug:\n    msg: duplicate\n")
                ]
            },
            JsonOptions);

        Assert.Equal(HttpStatusCode.BadRequest, missingEntryResponse.StatusCode);
        Assert.Equal(HttpStatusCode.BadRequest, duplicatePathResponse.StatusCode);
        Assert.Empty(factory.Db.Playbooks);
    }

    [Fact]
    public async Task Platform_admin_can_manage_all_customer_definitions()
    {
        await using var factory = DefinitionApiFactory.CreatePlatformAdmin();
        var customer = Customer.Create("Customer A", "customer-a", null, TestTime);
        factory.Db.AddCustomer(customer);
        using var client = factory.CreateClient();

        using var playbookResponse = await client.PostAsJsonAsync(
            $"/api/v1/customers/{customer.Id}/playbooks",
            ValidPlaybookRequest("deploy-app"),
            JsonOptions);
        using var variableSetResponse = await client.PostAsJsonAsync(
            $"/api/v1/customers/{customer.Id}/variable-sets",
            new CreateVariableSetRequest("Defaults", "defaults", null, VariableSetFormat.Json, "{\"packageName\":\"nginx\"}", false),
            JsonOptions);

        Assert.Equal(HttpStatusCode.Created, playbookResponse.StatusCode);
        Assert.Equal(HttpStatusCode.Created, variableSetResponse.StatusCode);
    }

    private static CreatePlaybookRequest ValidPlaybookRequest(string slug)
    {
        return new CreatePlaybookRequest("Deploy App", slug, null, PlaybookSourceType.InlineYaml, ValidYaml, null);
    }

    private static CreatePlaybookRequest ValidArtifactPlaybookRequest(string slug)
    {
        return new CreatePlaybookRequest(
            "Deploy App",
            slug,
            null,
            PlaybookSourceType.ArtifactDirectory,
            null,
            "site.yml",
            [
                new PlaybookArtifactFileDto("site.yml", "- hosts: all\n  roles:\n    - app\n"),
                new PlaybookArtifactFileDto("roles/app/tasks/main.yml", "- debug:\n    msg: hello\n")
            ]);
    }

    private static readonly JsonSerializerOptions JsonOptions = new(JsonSerializerDefaults.Web)
    {
        Converters = { new JsonStringEnumConverter(allowIntegerValues: false) }
    };

    private const string ValidYaml = "- hosts: all\n  tasks:\n    - debug:\n        msg: hello\n";

    private static DateTimeOffset TestTime => new(2026, 4, 27, 10, 0, 0, TimeSpan.Zero);

    private sealed record Seeded(User User, Customer Customer);

    private sealed class DefinitionApiFactory(
        CustomerRole role,
        bool isPlatformAdmin = false) : WebApplicationFactory<Program>
    {
        private const string Provider = "fake";
        private const string Subject = "definition-user";
        private const string Email = "definition-user@nodecontrol.local";
        private const string DisplayName = "Definition User";

        public DefinitionApiDbContext Db { get; } = new();

        public static DefinitionApiFactory Create(CustomerRole role)
        {
            return new DefinitionApiFactory(role);
        }

        public static DefinitionApiFactory CreatePlatformAdmin()
        {
            return new DefinitionApiFactory(CustomerRole.Viewer, isPlatformAdmin: true);
        }

        public Seeded SeedCurrentUserAndCustomer()
        {
            var user = User.Create(DisplayName, Email, isPlatformAdmin, TestTime);
            var identity = ExternalIdentity.Create(user, Provider, Subject, Email, DisplayName, TestTime);
            var customer = Customer.Create("Customer A", "customer-a", null, TestTime);
            Db.AddUser(user);
            Db.AddExternalIdentity(identity);
            Db.AddCustomer(customer);
            if (!isPlatformAdmin)
            {
                Db.AddCustomerMembership(CustomerMembership.Create(customer, user, role, TestTime));
            }

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
                    ["Auth:Fake:DisplayName"] = DisplayName,
                    ["Auth:Fake:IsPlatformAdmin"] = isPlatformAdmin.ToString()
                });
            });

            builder.ConfigureServices(services =>
            {
                services.RemoveAll<INodeControlDbContext>();
                services.AddSingleton<INodeControlDbContext>(Db);
            });
        }
    }

    private sealed class DefinitionApiDbContext : INodeControlDbContext
    {
        private readonly object syncRoot = new();

        public List<User> Users { get; } = [];

        public List<ExternalIdentity> ExternalIdentities { get; } = [];

        public List<Customer> Customers { get; } = [];

        public List<CustomerMembership> CustomerMemberships { get; } = [];

        public List<Playbook> Playbooks { get; } = [];

        public List<VariableSet> VariableSets { get; } = [];

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

        public Task<IReadOnlyList<Playbook>> ListActivePlaybooksAsync(Guid customerId, CancellationToken cancellationToken)
        {
            lock (syncRoot)
            {
                return Task.FromResult<IReadOnlyList<Playbook>>(
                    Playbooks.Where(playbook => playbook.CustomerId == customerId && playbook.Status == PlaybookStatus.Active).ToArray());
            }
        }

        public Task<Playbook?> FindPlaybookAsync(Guid customerId, Guid playbookId, CancellationToken cancellationToken)
        {
            lock (syncRoot)
            {
                return Task.FromResult(Playbooks.FirstOrDefault(playbook =>
                    playbook.CustomerId == customerId && playbook.Id == playbookId));
            }
        }

        public Task<Playbook?> FindPlaybookBySlugAsync(Guid customerId, string slug, CancellationToken cancellationToken)
        {
            lock (syncRoot)
            {
                return Task.FromResult(Playbooks.FirstOrDefault(playbook =>
                    playbook.CustomerId == customerId && playbook.Slug == slug));
            }
        }

        public Task<IReadOnlyList<VariableSet>> ListActiveVariableSetsAsync(Guid customerId, CancellationToken cancellationToken)
        {
            lock (syncRoot)
            {
                return Task.FromResult<IReadOnlyList<VariableSet>>(
                    VariableSets.Where(variableSet => variableSet.CustomerId == customerId && variableSet.Status == VariableSetStatus.Active).ToArray());
            }
        }

        public Task<VariableSet?> FindVariableSetAsync(Guid customerId, Guid variableSetId, CancellationToken cancellationToken)
        {
            lock (syncRoot)
            {
                return Task.FromResult(VariableSets.FirstOrDefault(variableSet =>
                    variableSet.CustomerId == customerId && variableSet.Id == variableSetId));
            }
        }

        public Task<VariableSet?> FindVariableSetBySlugAsync(Guid customerId, string slug, CancellationToken cancellationToken)
        {
            lock (syncRoot)
            {
                return Task.FromResult(VariableSets.FirstOrDefault(variableSet =>
                    variableSet.CustomerId == customerId && variableSet.Slug == slug));
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

        public void AddPlaybook(Playbook playbook)
        {
            lock (syncRoot)
            {
                Playbooks.Add(playbook);
            }
        }

        public void AddVariableSet(VariableSet variableSet)
        {
            lock (syncRoot)
            {
                VariableSets.Add(variableSet);
            }
        }

        public Task<int> SaveChangesAsync(CancellationToken cancellationToken)
        {
            return Task.FromResult(1);
        }
    }
}
