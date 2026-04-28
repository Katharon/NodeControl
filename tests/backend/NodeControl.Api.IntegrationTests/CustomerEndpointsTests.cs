using System.Net;
using System.Net.Http.Json;
using System.Text.Json;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using NodeControl.Application.Abstractions.Persistence;
using NodeControl.Application.Customers;
using NodeControl.Domain.Audit;
using NodeControl.Domain.Customers;
using NodeControl.Domain.Nodes;
using NodeControl.Domain.Users;

namespace NodeControl.Api.IntegrationTests;

public sealed class CustomerEndpointsTests
{
    [Fact]
    public async Task Platform_admin_can_list_users()
    {
        await using var factory = new CustomerApiFactory(isPlatformAdmin: true);
        factory.SeedCurrentUser();
        var targetUser = User.Create("Demo Operator", "operator@nodecontrol.local", false, TestTime);
        factory.DbContext.AddUser(targetUser);
        using var client = factory.CreateClient();

        using var response = await client.GetAsync("/api/v1/users?q=operator");

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        using var document = JsonDocument.Parse(await response.Content.ReadAsStringAsync());
        var result = Assert.Single(document.RootElement.EnumerateArray());
        Assert.Equal(targetUser.Id, result.GetProperty("id").GetGuid());
        Assert.Equal("Demo Operator", result.GetProperty("displayName").GetString());
        Assert.Equal("operator@nodecontrol.local", result.GetProperty("email").GetString());
        Assert.True(result.GetProperty("isActive").GetBoolean());
        Assert.False(result.GetProperty("isPlatformAdmin").GetBoolean());
        Assert.True(result.TryGetProperty("createdAt", out _));
        Assert.True(result.TryGetProperty("lastLoginAt", out _));
        Assert.False(result.TryGetProperty("normalizedEmail", out _));
    }

    [Fact]
    public async Task Normal_user_cannot_list_users()
    {
        await using var factory = CustomerApiFactory.ForNormalUser();
        factory.SeedCurrentUser();
        using var client = factory.CreateClient();

        using var response = await client.GetAsync("/api/v1/users");

        Assert.Equal(HttpStatusCode.Forbidden, response.StatusCode);
    }

    [Fact]
    public async Task Platform_admin_can_get_user_detail()
    {
        await using var factory = new CustomerApiFactory(isPlatformAdmin: true);
        factory.SeedCurrentUser();
        var targetUser = User.Create("Demo Operator", "operator@nodecontrol.local", false, TestTime);
        factory.DbContext.AddUser(targetUser);
        factory.DbContext.AddExternalIdentity(ExternalIdentity.Create(
            targetUser,
            "fake",
            "demo-operator",
            targetUser.Email,
            targetUser.DisplayName,
            TestTime));
        using var client = factory.CreateClient();

        using var response = await client.GetAsync($"/api/v1/users/{targetUser.Id}");

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        using var document = JsonDocument.Parse(await response.Content.ReadAsStringAsync());
        Assert.Equal(targetUser.Id, document.RootElement.GetProperty("id").GetGuid());
        var externalIdentity = Assert.Single(document.RootElement.GetProperty("externalIdentities").EnumerateArray());
        Assert.Equal("fake", externalIdentity.GetProperty("provider").GetString());
        Assert.Equal("demo-operator", externalIdentity.GetProperty("subject").GetString());
        Assert.False(externalIdentity.TryGetProperty("emailAtLogin", out _));
    }

    [Fact]
    public async Task Platform_admin_can_list_all_active_customers()
    {
        await using var factory = new CustomerApiFactory(isPlatformAdmin: true);
        factory.DbContext.AddCustomer(Customer.Create("Customer A", "customer-a", null, TestTime));
        factory.DbContext.AddCustomer(Customer.Create("Customer B", "customer-b", null, TestTime));
        using var client = factory.CreateClient();

        using var response = await client.GetAsync("/api/v1/customers");

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        using var document = JsonDocument.Parse(await response.Content.ReadAsStringAsync());
        Assert.Equal(2, document.RootElement.GetArrayLength());
    }

    [Fact]
    public async Task Normal_user_can_list_only_membership_customers()
    {
        await using var factory = CustomerApiFactory.ForNormalUser();
        var user = factory.SeedCurrentUser();
        var customerA = Customer.Create("Customer A", "customer-a", null, TestTime);
        var customerB = Customer.Create("Customer B", "customer-b", null, TestTime);
        factory.DbContext.AddCustomer(customerA);
        factory.DbContext.AddCustomer(customerB);
        factory.DbContext.AddCustomerMembership(CustomerMembership.Create(customerA, user, CustomerRole.Viewer, TestTime));
        using var client = factory.CreateClient();

        using var response = await client.GetAsync("/api/v1/customers");

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        using var document = JsonDocument.Parse(await response.Content.ReadAsStringAsync());
        Assert.Equal(1, document.RootElement.GetArrayLength());
        Assert.Equal(customerA.Id, document.RootElement[0].GetProperty("id").GetGuid());
    }

    [Fact]
    public async Task User_without_membership_cannot_access_another_customer()
    {
        await using var factory = CustomerApiFactory.ForNormalUser();
        var user = factory.SeedCurrentUser();
        var customerA = Customer.Create("Customer A", "customer-a", null, TestTime);
        var customerB = Customer.Create("Customer B", "customer-b", null, TestTime);
        factory.DbContext.AddCustomer(customerA);
        factory.DbContext.AddCustomer(customerB);
        factory.DbContext.AddCustomerMembership(CustomerMembership.Create(customerA, user, CustomerRole.Owner, TestTime));
        using var client = factory.CreateClient();

        using var response = await client.GetAsync($"/api/v1/customers/{customerB.Id}");

        Assert.Equal(HttpStatusCode.Forbidden, response.StatusCode);
    }

    [Fact]
    public async Task Inactive_membership_grants_no_access()
    {
        await using var factory = CustomerApiFactory.ForNormalUser();
        var user = factory.SeedCurrentUser();
        var customer = Customer.Create("Customer A", "customer-a", null, TestTime);
        var membership = CustomerMembership.Create(customer, user, CustomerRole.Owner, TestTime);
        membership.Deactivate(TestTime.AddMinutes(1));
        factory.DbContext.AddCustomer(customer);
        factory.DbContext.AddCustomerMembership(membership);
        using var client = factory.CreateClient();

        using var response = await client.GetAsync($"/api/v1/customers/{customer.Id}");

        Assert.Equal(HttpStatusCode.Forbidden, response.StatusCode);
    }

    [Fact]
    public async Task Viewer_cannot_update_customer()
    {
        await using var factory = CustomerApiFactory.ForNormalUser();
        var user = factory.SeedCurrentUser();
        var customer = Customer.Create("Customer A", "customer-a", null, TestTime);
        factory.DbContext.AddCustomer(customer);
        factory.DbContext.AddCustomerMembership(CustomerMembership.Create(customer, user, CustomerRole.Viewer, TestTime));
        using var client = factory.CreateClient();

        using var response = await client.PutAsJsonAsync(
            $"/api/v1/customers/{customer.Id}",
            new UpdateCustomerRequest("Updated Customer", "updated-customer", null));

        Assert.Equal(HttpStatusCode.Forbidden, response.StatusCode);
    }

    [Fact]
    public async Task Owner_can_manage_memberships()
    {
        await using var factory = CustomerApiFactory.ForNormalUser();
        var user = factory.SeedCurrentUser();
        var customer = Customer.Create("Customer A", "customer-a", null, TestTime);
        factory.DbContext.AddCustomer(customer);
        factory.DbContext.AddCustomerMembership(CustomerMembership.Create(customer, user, CustomerRole.Owner, TestTime));
        using var client = factory.CreateClient();

        using var response = await client.GetAsync($"/api/v1/customers/{customer.Id}/memberships");

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
    }

    [Fact]
    public async Task Admin_cannot_manage_memberships()
    {
        await using var factory = CustomerApiFactory.ForNormalUser();
        var user = factory.SeedCurrentUser();
        var customer = Customer.Create("Customer A", "customer-a", null, TestTime);
        factory.DbContext.AddCustomer(customer);
        factory.DbContext.AddCustomerMembership(CustomerMembership.Create(customer, user, CustomerRole.Admin, TestTime));
        using var client = factory.CreateClient();

        using var response = await client.GetAsync($"/api/v1/customers/{customer.Id}/memberships");

        Assert.Equal(HttpStatusCode.Forbidden, response.StatusCode);
    }

    [Fact]
    public async Task Owner_can_search_safe_user_fields_for_membership_selection()
    {
        await using var factory = CustomerApiFactory.ForNormalUser();
        var user = factory.SeedCurrentUser();
        var customer = Customer.Create("Customer A", "customer-a", null, TestTime);
        var targetUser = User.Create("Demo Operator", "operator@nodecontrol.local", false, TestTime);
        factory.DbContext.AddCustomer(customer);
        factory.DbContext.AddUser(targetUser);
        factory.DbContext.AddCustomerMembership(CustomerMembership.Create(customer, user, CustomerRole.Owner, TestTime));
        using var client = factory.CreateClient();

        using var response = await client.GetAsync($"/api/v1/customers/{customer.Id}/users/lookup?query=operator");

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        using var document = JsonDocument.Parse(await response.Content.ReadAsStringAsync());
        var result = document.RootElement[0];
        Assert.Equal(targetUser.Id, result.GetProperty("id").GetGuid());
        Assert.Equal("Demo Operator", result.GetProperty("displayName").GetString());
        Assert.Equal("operator@nodecontrol.local", result.GetProperty("email").GetString());
        Assert.True(result.GetProperty("isActive").GetBoolean());
        Assert.False(result.GetProperty("isPlatformAdmin").GetBoolean());
        Assert.False(result.TryGetProperty("normalizedEmail", out _));
        Assert.False(result.TryGetProperty("createdAt", out _));
        Assert.False(result.TryGetProperty("lastLoginAt", out _));
    }

    [Fact]
    public async Task Membership_candidates_exclude_existing_active_members()
    {
        await using var factory = CustomerApiFactory.ForNormalUser();
        var user = factory.SeedCurrentUser();
        var customer = Customer.Create("Customer A", "customer-a", null, TestTime);
        var targetUser = User.Create("Demo Operator", "operator@nodecontrol.local", false, TestTime);
        var existingUser = User.Create("Existing Operator", "existing@nodecontrol.local", false, TestTime);
        factory.DbContext.AddCustomer(customer);
        factory.DbContext.AddUser(targetUser);
        factory.DbContext.AddUser(existingUser);
        factory.DbContext.AddCustomerMembership(CustomerMembership.Create(customer, user, CustomerRole.Owner, TestTime));
        factory.DbContext.AddCustomerMembership(CustomerMembership.Create(customer, existingUser, CustomerRole.Operator, TestTime));
        using var client = factory.CreateClient();

        using var response = await client.GetAsync($"/api/v1/customers/{customer.Id}/membership-candidates?query=operator");

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        using var document = JsonDocument.Parse(await response.Content.ReadAsStringAsync());
        Assert.Contains(document.RootElement.EnumerateArray(), element =>
            element.GetProperty("id").GetGuid() == targetUser.Id);
        Assert.DoesNotContain(document.RootElement.EnumerateArray(), element =>
            element.GetProperty("id").GetGuid() == existingUser.Id);
    }

    [Fact]
    public async Task Membership_candidate_search_requires_manage_memberships_permission()
    {
        await using var factory = CustomerApiFactory.ForNormalUser();
        var user = factory.SeedCurrentUser();
        var customer = Customer.Create("Customer A", "customer-a", null, TestTime);
        factory.DbContext.AddCustomer(customer);
        factory.DbContext.AddCustomerMembership(CustomerMembership.Create(customer, user, CustomerRole.Admin, TestTime));
        using var client = factory.CreateClient();

        using var response = await client.GetAsync($"/api/v1/customers/{customer.Id}/membership-candidates?query=normal");

        Assert.Equal(HttpStatusCode.Forbidden, response.StatusCode);
    }

    [Fact]
    public async Task Membership_candidate_search_is_customer_scoped()
    {
        await using var factory = CustomerApiFactory.ForNormalUser();
        var user = factory.SeedCurrentUser();
        var customerA = Customer.Create("Customer A", "customer-a", null, TestTime);
        var customerB = Customer.Create("Customer B", "customer-b", null, TestTime);
        factory.DbContext.AddCustomer(customerA);
        factory.DbContext.AddCustomer(customerB);
        factory.DbContext.AddCustomerMembership(CustomerMembership.Create(customerA, user, CustomerRole.Owner, TestTime));
        using var client = factory.CreateClient();

        using var response = await client.GetAsync($"/api/v1/customers/{customerB.Id}/membership-candidates?query=normal");

        Assert.Equal(HttpStatusCode.Forbidden, response.StatusCode);
    }

    [Fact]
    public async Task User_lookup_requires_manage_memberships_permission()
    {
        await using var factory = CustomerApiFactory.ForNormalUser();
        var user = factory.SeedCurrentUser();
        var customer = Customer.Create("Customer A", "customer-a", null, TestTime);
        factory.DbContext.AddCustomer(customer);
        factory.DbContext.AddCustomerMembership(CustomerMembership.Create(customer, user, CustomerRole.Admin, TestTime));
        using var client = factory.CreateClient();

        using var response = await client.GetAsync($"/api/v1/customers/{customer.Id}/users/lookup?query=normal");

        Assert.Equal(HttpStatusCode.Forbidden, response.StatusCode);
    }

    [Fact]
    public async Task User_lookup_is_customer_scoped()
    {
        await using var factory = CustomerApiFactory.ForNormalUser();
        var user = factory.SeedCurrentUser();
        var customerA = Customer.Create("Customer A", "customer-a", null, TestTime);
        var customerB = Customer.Create("Customer B", "customer-b", null, TestTime);
        factory.DbContext.AddCustomer(customerA);
        factory.DbContext.AddCustomer(customerB);
        factory.DbContext.AddCustomerMembership(CustomerMembership.Create(customerA, user, CustomerRole.Owner, TestTime));
        using var client = factory.CreateClient();

        using var response = await client.GetAsync($"/api/v1/customers/{customerB.Id}/users/lookup?query=normal");

        Assert.Equal(HttpStatusCode.Forbidden, response.StatusCode);
    }

    [Fact]
    public async Task Owner_can_queue_control_node_connection_check()
    {
        await using var factory = CustomerApiFactory.ForNormalUser();
        var user = factory.SeedCurrentUser();
        var customer = Customer.Create("Customer A", "customer-a", null, TestTime);
        var controlNode = ControlNode.Create(customer.Id, "control-1", "127.0.0.1", 22, null, TestTime);
        factory.DbContext.AddCustomer(customer);
        factory.DbContext.AddControlNode(controlNode);
        factory.DbContext.AddCustomerMembership(CustomerMembership.Create(customer, user, CustomerRole.Owner, TestTime));
        using var client = factory.CreateClient();

        using var response = await client.PostAsync(
            $"/api/v1/customers/{customer.Id}/control-nodes/{controlNode.Id}/connection-checks",
            null);

        Assert.Equal(HttpStatusCode.Created, response.StatusCode);
        using var document = JsonDocument.Parse(await response.Content.ReadAsStringAsync());
        Assert.Equal("Queued", document.RootElement.GetProperty("status").GetString());
        Assert.Equal(controlNode.Id, document.RootElement.GetProperty("controlNodeId").GetGuid());
        Assert.Single(factory.DbContext.HostConnectionChecks);
    }

    [Fact]
    public async Task Owner_can_queue_managed_node_connection_check()
    {
        await using var factory = CustomerApiFactory.ForNormalUser();
        var user = factory.SeedCurrentUser();
        var customer = Customer.Create("Customer A", "customer-a", null, TestTime);
        var managedNode = ManagedNode.Create(customer.Id, "host_1", "127.0.0.1", 22, null, null, null, TestTime);
        factory.DbContext.AddCustomer(customer);
        factory.DbContext.AddManagedNode(managedNode);
        factory.DbContext.AddCustomerMembership(CustomerMembership.Create(customer, user, CustomerRole.Owner, TestTime));
        using var client = factory.CreateClient();

        using var response = await client.PostAsync(
            $"/api/v1/customers/{customer.Id}/managed-nodes/{managedNode.Id}/connection-checks",
            null);

        Assert.Equal(HttpStatusCode.Created, response.StatusCode);
        Assert.Single(factory.DbContext.HostConnectionChecks);
    }

    [Fact]
    public async Task Queue_connection_check_requires_manage_nodes_permission()
    {
        await using var factory = CustomerApiFactory.ForNormalUser();
        var user = factory.SeedCurrentUser();
        var customer = Customer.Create("Customer A", "customer-a", null, TestTime);
        var managedNode = ManagedNode.Create(customer.Id, "host_1", "127.0.0.1", 22, null, null, null, TestTime);
        factory.DbContext.AddCustomer(customer);
        factory.DbContext.AddManagedNode(managedNode);
        factory.DbContext.AddCustomerMembership(CustomerMembership.Create(customer, user, CustomerRole.Viewer, TestTime));
        using var client = factory.CreateClient();

        using var response = await client.PostAsync(
            $"/api/v1/customers/{customer.Id}/managed-nodes/{managedNode.Id}/connection-checks",
            null);

        Assert.Equal(HttpStatusCode.Forbidden, response.StatusCode);
        Assert.Empty(factory.DbContext.HostConnectionChecks);
    }

    [Fact]
    public async Task Queue_connection_check_rejects_cross_tenant_managed_node()
    {
        await using var factory = CustomerApiFactory.ForNormalUser();
        var user = factory.SeedCurrentUser();
        var customerA = Customer.Create("Customer A", "customer-a", null, TestTime);
        var customerB = Customer.Create("Customer B", "customer-b", null, TestTime);
        var managedNode = ManagedNode.Create(customerB.Id, "host_1", "127.0.0.1", 22, null, null, null, TestTime);
        factory.DbContext.AddCustomer(customerA);
        factory.DbContext.AddCustomer(customerB);
        factory.DbContext.AddManagedNode(managedNode);
        factory.DbContext.AddCustomerMembership(CustomerMembership.Create(customerA, user, CustomerRole.Owner, TestTime));
        using var client = factory.CreateClient();

        using var response = await client.PostAsync(
            $"/api/v1/customers/{customerA.Id}/managed-nodes/{managedNode.Id}/connection-checks",
            null);

        Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
        Assert.Empty(factory.DbContext.HostConnectionChecks);
    }

    [Fact]
    public async Task Viewer_can_list_host_connection_checks()
    {
        await using var factory = CustomerApiFactory.ForNormalUser();
        var user = factory.SeedCurrentUser();
        var customer = Customer.Create("Customer A", "customer-a", null, TestTime);
        var managedNode = ManagedNode.Create(customer.Id, "host_1", "127.0.0.1", 22, null, null, null, TestTime);
        var check = HostConnectionCheck.CreateForManagedNode(managedNode, user.Id, TestTime);
        factory.DbContext.AddCustomer(customer);
        factory.DbContext.AddManagedNode(managedNode);
        factory.DbContext.AddHostConnectionCheck(check);
        factory.DbContext.AddCustomerMembership(CustomerMembership.Create(customer, user, CustomerRole.Viewer, TestTime));
        using var client = factory.CreateClient();

        using var response = await client.GetAsync($"/api/v1/customers/{customer.Id}/host-connection-checks");

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        using var document = JsonDocument.Parse(await response.Content.ReadAsStringAsync());
        var result = Assert.Single(document.RootElement.EnumerateArray());
        Assert.Equal(check.Id, result.GetProperty("id").GetGuid());
    }

    [Fact]
    public async Task Host_health_summary_is_customer_scoped()
    {
        await using var factory = CustomerApiFactory.ForNormalUser();
        var user = factory.SeedCurrentUser();
        var customerA = Customer.Create("Customer A", "customer-a", null, TestTime);
        var customerB = Customer.Create("Customer B", "customer-b", null, TestTime);
        var visibleNode = ManagedNode.Create(customerA.Id, "host_a", "127.0.0.1", 22, null, null, null, TestTime);
        var hiddenNode = ManagedNode.Create(customerB.Id, "host_b", "192.0.2.10", 22, null, null, null, TestTime);
        factory.DbContext.AddCustomer(customerA);
        factory.DbContext.AddCustomer(customerB);
        factory.DbContext.AddManagedNode(visibleNode);
        factory.DbContext.AddManagedNode(hiddenNode);
        factory.DbContext.AddCustomerMembership(CustomerMembership.Create(customerA, user, CustomerRole.Viewer, TestTime));
        using var client = factory.CreateClient();

        using var response = await client.GetAsync($"/api/v1/customers/{customerA.Id}/host-health");

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        using var document = JsonDocument.Parse(await response.Content.ReadAsStringAsync());
        var targets = document.RootElement.GetProperty("targets").EnumerateArray().ToArray();
        Assert.Contains(targets, target => target.GetProperty("targetId").GetGuid() == visibleNode.Id);
        Assert.DoesNotContain(targets, target => target.GetProperty("targetId").GetGuid() == hiddenNode.Id);
    }

    [Fact]
    public async Task Owner_can_create_membership_for_selected_user()
    {
        await using var factory = CustomerApiFactory.ForNormalUser();
        var user = factory.SeedCurrentUser();
        var customer = Customer.Create("Customer A", "customer-a", null, TestTime);
        var targetUser = User.Create("Demo Viewer", "viewer@nodecontrol.local", false, TestTime);
        factory.DbContext.AddCustomer(customer);
        factory.DbContext.AddUser(targetUser);
        factory.DbContext.AddCustomerMembership(CustomerMembership.Create(customer, user, CustomerRole.Owner, TestTime));
        using var client = factory.CreateClient();

        using var response = await client.PostAsJsonAsync(
            $"/api/v1/customers/{customer.Id}/memberships",
            new { userId = targetUser.Id, role = "Viewer" });

        Assert.Equal(HttpStatusCode.Created, response.StatusCode);
        Assert.Contains(factory.DbContext.CustomerMemberships, membership =>
            membership.CustomerId == customer.Id
            && membership.UserId == targetUser.Id
            && membership.Role == CustomerRole.Viewer);
    }

    [Fact]
    public async Task User_without_customer_access_cannot_create_membership()
    {
        await using var factory = CustomerApiFactory.ForNormalUser();
        var user = factory.SeedCurrentUser();
        var customerA = Customer.Create("Customer A", "customer-a", null, TestTime);
        var customerB = Customer.Create("Customer B", "customer-b", null, TestTime);
        var targetUser = User.Create("Demo Viewer", "viewer@nodecontrol.local", false, TestTime);
        factory.DbContext.AddCustomer(customerA);
        factory.DbContext.AddCustomer(customerB);
        factory.DbContext.AddUser(targetUser);
        factory.DbContext.AddCustomerMembership(CustomerMembership.Create(customerA, user, CustomerRole.Owner, TestTime));
        using var client = factory.CreateClient();

        using var response = await client.PostAsJsonAsync(
            $"/api/v1/customers/{customerB.Id}/memberships",
            new { userId = targetUser.Id, role = "Viewer" });

        Assert.Equal(HttpStatusCode.Forbidden, response.StatusCode);
        Assert.DoesNotContain(factory.DbContext.CustomerMemberships, membership =>
            membership.CustomerId == customerB.Id && membership.UserId == targetUser.Id);
    }

    private static DateTimeOffset TestTime => new(2026, 4, 27, 8, 0, 0, TimeSpan.Zero);

    private sealed class CustomerApiFactory(
        bool isPlatformAdmin = false,
        string subject = "normal-user",
        string email = "normal@nodecontrol.local",
        string displayName = "Normal User")
        : WebApplicationFactory<Program>
    {
        public TestNodeControlDbContext DbContext { get; } = new();

        public static CustomerApiFactory ForNormalUser()
        {
            return new CustomerApiFactory();
        }

        public User SeedCurrentUser()
        {
            var user = User.Create(displayName, email, isPlatformAdmin, TestTime);
            var identity = ExternalIdentity.Create(user, "fake", subject, email, displayName, TestTime);
            DbContext.AddUser(user);
            DbContext.AddExternalIdentity(identity);
            return user;
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
                    ["Auth:Fake:Provider"] = "fake",
                    ["Auth:Fake:Subject"] = subject,
                    ["Auth:Fake:Email"] = email,
                    ["Auth:Fake:DisplayName"] = displayName,
                    ["Auth:Fake:IsPlatformAdmin"] = isPlatformAdmin.ToString()
                });
            });

            builder.ConfigureServices(services =>
            {
                services.RemoveAll<INodeControlDbContext>();
                services.AddSingleton<INodeControlDbContext>(DbContext);
            });
        }
    }

    public sealed class TestNodeControlDbContext : INodeControlDbContext
    {
        private readonly object syncRoot = new();

        public List<User> Users { get; } = [];

        public List<ExternalIdentity> ExternalIdentities { get; } = [];

        public List<Customer> Customers { get; } = [];

        public List<CustomerMembership> CustomerMemberships { get; } = [];

        public List<ControlNode> ControlNodes { get; } = [];

        public List<ManagedNode> ManagedNodes { get; } = [];

        public List<HostConnectionCheck> HostConnectionChecks { get; } = [];

        public List<AuditLogEntry> AuditLogEntries { get; } = [];

        public Task<ExternalIdentity?> FindExternalIdentityAsync(
            string provider,
            string subject,
            CancellationToken cancellationToken)
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

        public Task<IReadOnlyList<User>> ListUsersAsync(
            string? query,
            bool includeInactive,
            int limit,
            CancellationToken cancellationToken)
        {
            lock (syncRoot)
            {
                var normalizedQuery = string.IsNullOrWhiteSpace(query)
                    ? string.Empty
                    : query.Trim().ToUpperInvariant();

                return Task.FromResult<IReadOnlyList<User>>(
                    Users
                        .Where(user => (includeInactive || user.IsActive)
                            && (normalizedQuery.Length == 0
                                || user.NormalizedEmail.Contains(normalizedQuery)
                                || user.DisplayName.ToUpperInvariant().Contains(normalizedQuery)))
                        .OrderBy(user => user.DisplayName)
                        .ThenBy(user => user.Email)
                        .Take(Math.Clamp(limit, 1, 200))
                        .ToArray());
            }
        }

        public Task<IReadOnlyList<ExternalIdentity>> ListExternalIdentitiesForUsersAsync(
            IReadOnlyCollection<Guid> userIds,
            CancellationToken cancellationToken)
        {
            lock (syncRoot)
            {
                return Task.FromResult<IReadOnlyList<ExternalIdentity>>(
                    ExternalIdentities
                        .Where(externalIdentity => userIds.Contains(externalIdentity.UserId))
                        .OrderBy(externalIdentity => externalIdentity.Provider)
                        .ThenBy(externalIdentity => externalIdentity.Subject)
                        .ToArray());
            }
        }

        public Task<IReadOnlyList<User>> SearchUsersAsync(
            string? query,
            int limit,
            CancellationToken cancellationToken)
        {
            lock (syncRoot)
            {
                var normalizedQuery = string.IsNullOrWhiteSpace(query)
                    ? string.Empty
                    : query.Trim().ToUpperInvariant();

                return Task.FromResult<IReadOnlyList<User>>(
                    Users
                        .Where(user => user.IsActive
                            && (normalizedQuery.Length == 0
                                || user.NormalizedEmail.Contains(normalizedQuery)
                                || user.DisplayName.ToUpperInvariant().Contains(normalizedQuery)))
                        .OrderBy(user => user.DisplayName)
                        .ThenBy(user => user.Email)
                        .Take(limit)
                        .ToArray());
            }
        }

        public Task<IReadOnlyList<User>> SearchMembershipCandidateUsersAsync(
            Guid customerId,
            string? query,
            int limit,
            CancellationToken cancellationToken)
        {
            lock (syncRoot)
            {
                var normalizedQuery = string.IsNullOrWhiteSpace(query)
                    ? string.Empty
                    : query.Trim().ToUpperInvariant();

                return Task.FromResult<IReadOnlyList<User>>(
                    Users
                        .Where(user => user.IsActive
                            && !CustomerMemberships.Any(membership =>
                                membership.CustomerId == customerId
                                && membership.UserId == user.Id
                                && membership.IsActive)
                            && (normalizedQuery.Length == 0
                                || user.NormalizedEmail.Contains(normalizedQuery)
                                || user.DisplayName.ToUpperInvariant().Contains(normalizedQuery)))
                        .OrderBy(user => user.DisplayName)
                        .ThenBy(user => user.Email)
                        .Take(Math.Clamp(limit, 1, 50))
                        .ToArray());
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

        public Task<IReadOnlyList<Customer>> ListActiveCustomersForUserAsync(
            Guid userId,
            CancellationToken cancellationToken)
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

        public Task<IReadOnlyList<CustomerMembership>> ListCustomerMembershipsAsync(
            Guid customerId,
            CancellationToken cancellationToken)
        {
            lock (syncRoot)
            {
                return Task.FromResult<IReadOnlyList<CustomerMembership>>(
                    CustomerMemberships.Where(membership => membership.CustomerId == customerId).ToArray());
            }
        }

        public Task<CustomerMembership?> FindCustomerMembershipAsync(
            Guid id,
            CancellationToken cancellationToken)
        {
            lock (syncRoot)
            {
                return Task.FromResult(CustomerMemberships.FirstOrDefault(membership => membership.Id == id));
            }
        }

        public Task<CustomerMembership?> FindCustomerMembershipForUserAsync(
            Guid customerId,
            Guid userId,
            CancellationToken cancellationToken)
        {
            lock (syncRoot)
            {
                return Task.FromResult(CustomerMemberships.FirstOrDefault(membership =>
                    membership.CustomerId == customerId && membership.UserId == userId));
            }
        }

        public Task<IReadOnlyList<ControlNode>> ListActiveControlNodesAsync(
            Guid customerId,
            CancellationToken cancellationToken)
        {
            lock (syncRoot)
            {
                return Task.FromResult<IReadOnlyList<ControlNode>>(
                    ControlNodes
                        .Where(controlNode => controlNode.CustomerId == customerId
                            && controlNode.Status == ControlNodeStatus.Active)
                        .ToArray());
            }
        }

        public Task<ControlNode?> FindControlNodeAsync(
            Guid customerId,
            Guid controlNodeId,
            CancellationToken cancellationToken)
        {
            lock (syncRoot)
            {
                return Task.FromResult(ControlNodes.FirstOrDefault(controlNode =>
                    controlNode.CustomerId == customerId && controlNode.Id == controlNodeId));
            }
        }

        public Task<ControlNode?> FindControlNodeByNameAsync(
            Guid customerId,
            string name,
            CancellationToken cancellationToken)
        {
            lock (syncRoot)
            {
                return Task.FromResult(ControlNodes.FirstOrDefault(controlNode =>
                    controlNode.CustomerId == customerId && controlNode.Name == name));
            }
        }

        public Task<IReadOnlyList<ManagedNode>> ListActiveManagedNodesAsync(
            Guid customerId,
            CancellationToken cancellationToken)
        {
            lock (syncRoot)
            {
                return Task.FromResult<IReadOnlyList<ManagedNode>>(
                    ManagedNodes
                        .Where(managedNode => managedNode.CustomerId == customerId
                            && managedNode.Status == ManagedNodeStatus.Active)
                        .ToArray());
            }
        }

        public Task<ManagedNode?> FindManagedNodeAsync(
            Guid customerId,
            Guid managedNodeId,
            CancellationToken cancellationToken)
        {
            lock (syncRoot)
            {
                return Task.FromResult(ManagedNodes.FirstOrDefault(managedNode =>
                    managedNode.CustomerId == customerId && managedNode.Id == managedNodeId));
            }
        }

        public Task<ManagedNode?> FindManagedNodeByNameAsync(
            Guid customerId,
            string name,
            CancellationToken cancellationToken)
        {
            lock (syncRoot)
            {
                return Task.FromResult(ManagedNodes.FirstOrDefault(managedNode =>
                    managedNode.CustomerId == customerId && managedNode.Name == name));
            }
        }

        public Task<IReadOnlyList<HostConnectionCheck>> ListHostConnectionChecksAsync(
            Guid customerId,
            HostConnectionTargetType? targetType,
            Guid? controlNodeId,
            Guid? managedNodeId,
            int limit,
            CancellationToken cancellationToken)
        {
            lock (syncRoot)
            {
                var checks = HostConnectionChecks.Where(check => check.CustomerId == customerId);

                if (targetType is not null)
                {
                    checks = checks.Where(check => check.TargetType == targetType);
                }

                if (controlNodeId is not null)
                {
                    checks = checks.Where(check => check.ControlNodeId == controlNodeId);
                }

                if (managedNodeId is not null)
                {
                    checks = checks.Where(check => check.ManagedNodeId == managedNodeId);
                }

                return Task.FromResult<IReadOnlyList<HostConnectionCheck>>(
                    checks
                        .OrderByDescending(check => check.QueuedAtUtc)
                        .Take(limit)
                        .ToArray());
            }
        }

        public Task<IReadOnlyList<HostConnectionCheck>> ListLatestHostConnectionChecksAsync(
            Guid customerId,
            int limit,
            CancellationToken cancellationToken)
        {
            lock (syncRoot)
            {
                return Task.FromResult<IReadOnlyList<HostConnectionCheck>>(
                    HostConnectionChecks
                        .Where(check => check.CustomerId == customerId)
                        .OrderByDescending(check => check.QueuedAtUtc)
                        .Take(limit)
                        .ToArray());
            }
        }

        public Task<HostConnectionCheck?> FindHostConnectionCheckAsync(
            Guid customerId,
            Guid hostConnectionCheckId,
            CancellationToken cancellationToken)
        {
            lock (syncRoot)
            {
                return Task.FromResult(HostConnectionChecks.FirstOrDefault(check =>
                    check.CustomerId == customerId && check.Id == hostConnectionCheckId));
            }
        }

        public Task<HostConnectionCheck?> FindOldestQueuedHostConnectionCheckAsync(CancellationToken cancellationToken)
        {
            lock (syncRoot)
            {
                return Task.FromResult(HostConnectionChecks
                    .Where(check => check.Status == HostConnectionCheckStatus.Queued)
                    .OrderBy(check => check.QueuedAtUtc)
                    .FirstOrDefault());
            }
        }

        public Task<HostConnectionCheckStatus?> GetHostConnectionCheckStatusAsync(
            Guid hostConnectionCheckId,
            CancellationToken cancellationToken)
        {
            lock (syncRoot)
            {
                return Task.FromResult(HostConnectionChecks
                    .Where(check => check.Id == hostConnectionCheckId)
                    .Select(check => (HostConnectionCheckStatus?)check.Status)
                    .FirstOrDefault());
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

        public void AddControlNode(ControlNode controlNode)
        {
            lock (syncRoot)
            {
                ControlNodes.Add(controlNode);
            }
        }

        public void AddManagedNode(ManagedNode managedNode)
        {
            lock (syncRoot)
            {
                ManagedNodes.Add(managedNode);
            }
        }

        public void AddHostConnectionCheck(HostConnectionCheck hostConnectionCheck)
        {
            lock (syncRoot)
            {
                HostConnectionChecks.Add(hostConnectionCheck);
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
