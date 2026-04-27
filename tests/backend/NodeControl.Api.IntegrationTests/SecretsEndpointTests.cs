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
using NodeControl.Application.Secrets;
using NodeControl.Domain.Audit;
using NodeControl.Domain.Customers;
using NodeControl.Domain.Secrets;
using NodeControl.Domain.Users;

namespace NodeControl.Api.IntegrationTests;

public sealed class SecretsEndpointTests
{
    [Fact]
    public async Task Get_secrets_requires_view_secrets()
    {
        await using var factory = SecretApiFactory.Create(CustomerRole.Viewer);
        var seeded = factory.SeedCurrentUserAndCustomer();
        factory.Db.AddSecret(Secret.Create(
            seeded.Customer.Id,
            "API Token",
            "api-token",
            null,
            SecretKind.ApiToken,
            "protected-value",
            TestTime));
        using var client = factory.CreateClient();

        using var response = await client.GetAsync($"/api/v1/customers/{seeded.Customer.Id}/secrets");

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
    }

    [Fact]
    public async Task Post_put_rotate_and_delete_secrets_require_manage_secrets()
    {
        await using var factory = SecretApiFactory.Create(CustomerRole.Viewer);
        var seeded = factory.SeedCurrentUserAndCustomer();
        var secret = Secret.Create(
            seeded.Customer.Id,
            "API Token",
            "api-token",
            null,
            SecretKind.ApiToken,
            "protected-value",
            TestTime);
        factory.Db.AddSecret(secret);
        using var client = factory.CreateClient();

        using var postResponse = await client.PostAsJsonAsync(
            $"/api/v1/customers/{seeded.Customer.Id}/secrets",
            ValidCreateRequest("new-token", "secret-value"),
            JsonOptions);
        using var putResponse = await client.PutAsJsonAsync(
            $"/api/v1/customers/{seeded.Customer.Id}/secrets/{secret.Id}",
            new UpdateSecretRequest("API Token", "api-token", null, "ApiToken"),
            JsonOptions);
        using var rotateResponse = await client.PostAsJsonAsync(
            $"/api/v1/customers/{seeded.Customer.Id}/secrets/{secret.Id}/rotate",
            new RotateSecretRequest("new-secret-value"),
            JsonOptions);
        using var deleteResponse = await client.DeleteAsync($"/api/v1/customers/{seeded.Customer.Id}/secrets/{secret.Id}");

        Assert.Equal(HttpStatusCode.Forbidden, postResponse.StatusCode);
        Assert.Equal(HttpStatusCode.Forbidden, putResponse.StatusCode);
        Assert.Equal(HttpStatusCode.Forbidden, rotateResponse.StatusCode);
        Assert.Equal(HttpStatusCode.Forbidden, deleteResponse.StatusCode);
    }

    [Fact]
    public async Task Get_secret_by_id_rejects_user_without_view_secrets()
    {
        await using var factory = SecretApiFactory.Create(CustomerRole.Auditor);
        var seeded = factory.SeedCurrentUserAndCustomer();
        var secret = Secret.Create(seeded.Customer.Id, "API Token", "api-token", null, SecretKind.ApiToken, "protected-value", TestTime);
        factory.Db.AddSecret(secret);
        using var client = factory.CreateClient();

        using var response = await client.GetAsync($"/api/v1/customers/{seeded.Customer.Id}/secrets/{secret.Id}");

        Assert.Equal(HttpStatusCode.Forbidden, response.StatusCode);
    }

    [Fact]
    public async Task Secret_api_never_returns_plaintext_or_protected_value()
    {
        await using var factory = SecretApiFactory.Create(CustomerRole.Owner);
        var seeded = factory.SeedCurrentUserAndCustomer();
        using var client = factory.CreateClient();

        using var createResponse = await client.PostAsJsonAsync(
            $"/api/v1/customers/{seeded.Customer.Id}/secrets",
            ValidCreateRequest("api-token", "unique-secret-value"),
            JsonOptions);
        var createBody = await createResponse.Content.ReadAsStringAsync();
        var secret = JsonSerializer.Deserialize<SecretDto>(createBody, JsonOptions);
        var protectedValue = factory.Db.Secrets.Single().ProtectedValue;
        using var detailResponse = await client.GetAsync($"/api/v1/customers/{seeded.Customer.Id}/secrets/{secret!.Id}");
        var detailBody = await detailResponse.Content.ReadAsStringAsync();

        Assert.Equal(HttpStatusCode.Created, createResponse.StatusCode);
        Assert.DoesNotContain("unique-secret-value", createBody);
        Assert.DoesNotContain(protectedValue, createBody);
        Assert.DoesNotContain("protectedValue", createBody, StringComparison.OrdinalIgnoreCase);
        Assert.DoesNotContain("unique-secret-value", detailBody);
        Assert.DoesNotContain(protectedValue, detailBody);
        Assert.DoesNotContain("protectedValue", detailBody, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public async Task Manage_secrets_can_update_rotate_and_archive()
    {
        await using var factory = SecretApiFactory.Create(CustomerRole.Owner);
        var seeded = factory.SeedCurrentUserAndCustomer();
        using var client = factory.CreateClient();

        using var createResponse = await client.PostAsJsonAsync(
            $"/api/v1/customers/{seeded.Customer.Id}/secrets",
            ValidCreateRequest("api-token", "one"),
            JsonOptions);
        var secret = await createResponse.Content.ReadFromJsonAsync<SecretDto>(JsonOptions);
        var originalProtectedValue = factory.Db.Secrets.Single().ProtectedValue;
        using var updateResponse = await client.PutAsJsonAsync(
            $"/api/v1/customers/{seeded.Customer.Id}/secrets/{secret!.Id}",
            new UpdateSecretRequest("API Token 2", "api-token-2", null, "ApiToken"),
            JsonOptions);
        using var rotateResponse = await client.PostAsJsonAsync(
            $"/api/v1/customers/{seeded.Customer.Id}/secrets/{secret.Id}/rotate",
            new RotateSecretRequest("two"),
            JsonOptions);
        var rotated = await rotateResponse.Content.ReadFromJsonAsync<SecretDto>(JsonOptions);
        using var deleteResponse = await client.DeleteAsync($"/api/v1/customers/{seeded.Customer.Id}/secrets/{secret.Id}");

        Assert.Equal(HttpStatusCode.OK, updateResponse.StatusCode);
        Assert.Equal(HttpStatusCode.OK, rotateResponse.StatusCode);
        Assert.Equal(HttpStatusCode.OK, deleteResponse.StatusCode);
        Assert.NotEqual(originalProtectedValue, factory.Db.Secrets.Single().ProtectedValue);
        Assert.NotNull(rotated!.LastRotatedAtUtc);
        Assert.Equal(SecretStatus.Archived, factory.Db.Secrets.Single().Status);
    }

    [Fact]
    public async Task Cross_tenant_access_is_rejected_for_detail_update_rotate_and_archive()
    {
        await using var factory = SecretApiFactory.Create(CustomerRole.Owner);
        var seeded = factory.SeedCurrentUserAndCustomer();
        var otherCustomer = Customer.Create("Other Customer", "other-customer", null, TestTime);
        var otherSecret = Secret.Create(otherCustomer.Id, "API Token", "api-token", null, SecretKind.ApiToken, "protected-value", TestTime);
        factory.Db.AddCustomer(otherCustomer);
        factory.Db.AddSecret(otherSecret);
        using var client = factory.CreateClient();

        using var getResponse = await client.GetAsync($"/api/v1/customers/{seeded.Customer.Id}/secrets/{otherSecret.Id}");
        using var putResponse = await client.PutAsJsonAsync(
            $"/api/v1/customers/{seeded.Customer.Id}/secrets/{otherSecret.Id}",
            new UpdateSecretRequest("API Token", "api-token", null, "ApiToken"),
            JsonOptions);
        using var rotateResponse = await client.PostAsJsonAsync(
            $"/api/v1/customers/{seeded.Customer.Id}/secrets/{otherSecret.Id}/rotate",
            new RotateSecretRequest("new-value"),
            JsonOptions);
        using var deleteResponse = await client.DeleteAsync($"/api/v1/customers/{seeded.Customer.Id}/secrets/{otherSecret.Id}");

        Assert.Equal(HttpStatusCode.NotFound, getResponse.StatusCode);
        Assert.Equal(HttpStatusCode.NotFound, putResponse.StatusCode);
        Assert.Equal(HttpStatusCode.NotFound, rotateResponse.StatusCode);
        Assert.Equal(HttpStatusCode.NotFound, deleteResponse.StatusCode);
    }

    [Fact]
    public async Task List_endpoint_does_not_return_other_customer_secrets()
    {
        await using var factory = SecretApiFactory.Create(CustomerRole.Owner);
        var seeded = factory.SeedCurrentUserAndCustomer();
        var otherCustomer = Customer.Create("Other Customer", "other-customer", null, TestTime);
        factory.Db.AddCustomer(otherCustomer);
        factory.Db.AddSecret(Secret.Create(seeded.Customer.Id, "API Token", "api-token", null, SecretKind.ApiToken, "protected-value", TestTime));
        factory.Db.AddSecret(Secret.Create(otherCustomer.Id, "Other Token", "other-token", null, SecretKind.ApiToken, "protected-value", TestTime));
        using var client = factory.CreateClient();

        var secrets = await client.GetFromJsonAsync<SecretDto[]>($"/api/v1/customers/{seeded.Customer.Id}/secrets", JsonOptions);

        Assert.Single(secrets!);
        Assert.Equal("api-token", secrets![0].Slug);
    }

    [Fact]
    public async Task Audit_entries_do_not_contain_secret_values()
    {
        await using var factory = SecretApiFactory.Create(CustomerRole.Owner);
        var seeded = factory.SeedCurrentUserAndCustomer();
        using var client = factory.CreateClient();

        using var response = await client.PostAsJsonAsync(
            $"/api/v1/customers/{seeded.Customer.Id}/secrets",
            ValidCreateRequest("api-token", "unique-secret-value"),
            JsonOptions);

        Assert.Equal(HttpStatusCode.Created, response.StatusCode);
        var metadata = factory.Db.AuditLogEntries.Single().MetadataJson;
        Assert.DoesNotContain("unique-secret-value", metadata);
        Assert.DoesNotContain(factory.Db.Secrets.Single().ProtectedValue, metadata);
    }

    [Fact]
    public async Task Secret_reference_validation_endpoint_returns_metadata_only()
    {
        await using var factory = SecretApiFactory.Create(CustomerRole.Viewer);
        var seeded = factory.SeedCurrentUserAndCustomer();
        var secret = Secret.Create(
            seeded.Customer.Id,
            "API Token",
            "api-token",
            null,
            SecretKind.ApiToken,
            "protected-secret-value",
            TestTime);
        factory.Db.AddSecret(secret);
        using var client = factory.CreateClient();

        using var response = await client.PostAsJsonAsync(
            $"/api/v1/customers/{seeded.Customer.Id}/secret-references/validate",
            new ValidateSecretReferencesRequest("token: secret://api-token"),
            JsonOptions);
        var body = await response.Content.ReadAsStringAsync();

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        Assert.Contains("api-token", body);
        Assert.DoesNotContain("protected-secret-value", body);
        Assert.DoesNotContain("protectedValue", body, StringComparison.OrdinalIgnoreCase);
    }

    private static CreateSecretRequest ValidCreateRequest(string slug, string value)
    {
        return new CreateSecretRequest("API Token", slug, null, "ApiToken", value);
    }

    private static readonly JsonSerializerOptions JsonOptions = new(JsonSerializerDefaults.Web)
    {
        Converters = { new JsonStringEnumConverter(allowIntegerValues: false) }
    };

    private static DateTimeOffset TestTime => new(2026, 4, 27, 10, 0, 0, TimeSpan.Zero);

    private sealed record Seeded(User User, Customer Customer);

    private sealed class SecretApiFactory(CustomerRole role) : WebApplicationFactory<Program>
    {
        private const string Provider = "fake";
        private const string Subject = "secret-user";
        private const string Email = "secret-user@nodecontrol.local";
        private const string DisplayName = "Secret User";

        public SecretApiDbContext Db { get; } = new();

        public static SecretApiFactory Create(CustomerRole role)
        {
            return new SecretApiFactory(role);
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

    private sealed class SecretApiDbContext : INodeControlDbContext
    {
        private readonly object syncRoot = new();

        public List<User> Users { get; } = [];

        public List<ExternalIdentity> ExternalIdentities { get; } = [];

        public List<Customer> Customers { get; } = [];

        public List<CustomerMembership> CustomerMemberships { get; } = [];

        public List<Secret> Secrets { get; } = [];

        public List<AuditLogEntry> AuditLogEntries { get; } = [];

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

        public Task<IReadOnlyList<Secret>> ListActiveSecretsAsync(Guid customerId, CancellationToken cancellationToken)
        {
            lock (syncRoot)
            {
                return Task.FromResult<IReadOnlyList<Secret>>(
                    Secrets.Where(secret => secret.CustomerId == customerId && secret.Status == SecretStatus.Active).ToArray());
            }
        }

        public Task<Secret?> FindSecretAsync(Guid customerId, Guid secretId, CancellationToken cancellationToken)
        {
            lock (syncRoot)
            {
                return Task.FromResult(Secrets.FirstOrDefault(secret =>
                    secret.CustomerId == customerId && secret.Id == secretId));
            }
        }

        public Task<Secret?> FindSecretBySlugAsync(Guid customerId, string slug, CancellationToken cancellationToken)
        {
            lock (syncRoot)
            {
                return Task.FromResult(Secrets.FirstOrDefault(secret =>
                    secret.CustomerId == customerId && secret.Slug == slug));
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

        public void AddSecret(Secret secret)
        {
            lock (syncRoot)
            {
                Secrets.Add(secret);
            }
        }

        public void AddAuditLogEntry(AuditLogEntry auditLogEntry)
        {
            lock (syncRoot)
            {
                AuditLogEntries.Add(auditLogEntry);
            }
        }

        public Task<int> SaveChangesAsync(CancellationToken cancellationToken)
        {
            return Task.FromResult(1);
        }
    }
}
