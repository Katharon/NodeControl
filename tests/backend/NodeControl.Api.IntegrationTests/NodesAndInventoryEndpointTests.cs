using System.Net;
using System.Net.Http.Json;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using NodeControl.Application.Abstractions.Persistence;
using NodeControl.Application.ControlNodes;
using NodeControl.Application.InventoryGroups;
using NodeControl.Application.ManagedNodes;
using NodeControl.Domain.Customers;
using NodeControl.Domain.Inventories;
using NodeControl.Domain.Nodes;
using NodeControl.Domain.Users;

namespace NodeControl.Api.IntegrationTests;

public sealed class NodesAndInventoryEndpointTests
{
    [Fact]
    public async Task Get_preview_requires_view_nodes()
    {
        await using var factory = NodeApiFactory.Create(CustomerRole.Viewer);
        var seeded = factory.SeedCurrentUserAndCustomer();
        var group = InventoryGroup.Create(seeded.Customer.Id, "webservers", null, TestTime);
        var managedNode = ManagedNode.Create(seeded.Customer.Id, "web_01", "10.0.0.10", 22, null, null, null, TestTime);
        factory.Db.AddInventoryGroup(group);
        factory.Db.AddManagedNode(managedNode);
        factory.Db.AddInventoryGroupNode(InventoryGroupNode.Create(group, managedNode, TestTime));
        using var client = factory.CreateClient();

        using var response = await client.GetAsync(
            $"/api/v1/customers/{seeded.Customer.Id}/inventory-groups/{group.Id}/preview");

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
    }

    [Fact]
    public async Task Post_endpoint_requires_manage_nodes()
    {
        await using var factory = NodeApiFactory.Create(CustomerRole.Viewer);
        var seeded = factory.SeedCurrentUserAndCustomer();
        using var client = factory.CreateClient();

        using var response = await client.PostAsJsonAsync(
            $"/api/v1/customers/{seeded.Customer.Id}/control-nodes",
            new CreateControlNodeRequest("Control 01", "control-01.local"));

        Assert.Equal(HttpStatusCode.Forbidden, response.StatusCode);
    }

    [Fact]
    public async Task Put_endpoint_requires_manage_nodes()
    {
        await using var factory = NodeApiFactory.Create(CustomerRole.Viewer);
        var seeded = factory.SeedCurrentUserAndCustomer();
        var managedNode = ManagedNode.Create(seeded.Customer.Id, "web_01", "10.0.0.10", 22, null, null, null, TestTime);
        factory.Db.AddManagedNode(managedNode);
        using var client = factory.CreateClient();

        using var response = await client.PutAsJsonAsync(
            $"/api/v1/customers/{seeded.Customer.Id}/managed-nodes/{managedNode.Id}",
            new UpdateManagedNodeRequest("web_01", "10.0.0.12"));

        Assert.Equal(HttpStatusCode.Forbidden, response.StatusCode);
    }

    [Fact]
    public async Task Delete_endpoint_requires_manage_nodes()
    {
        await using var factory = NodeApiFactory.Create(CustomerRole.Viewer);
        var seeded = factory.SeedCurrentUserAndCustomer();
        var group = InventoryGroup.Create(seeded.Customer.Id, "webservers", null, TestTime);
        factory.Db.AddInventoryGroup(group);
        using var client = factory.CreateClient();

        using var response = await client.DeleteAsync(
            $"/api/v1/customers/{seeded.Customer.Id}/inventory-groups/{group.Id}");

        Assert.Equal(HttpStatusCode.Forbidden, response.StatusCode);
    }

    [Fact]
    public async Task Customer_scoped_endpoint_rejects_cross_tenant_node_access()
    {
        await using var factory = NodeApiFactory.Create(CustomerRole.Owner);
        var seeded = factory.SeedCurrentUserAndCustomer();
        var otherCustomer = Customer.Create("Other Customer", "other-customer", null, TestTime);
        var otherNode = ManagedNode.Create(otherCustomer.Id, "web_01", "10.0.0.10", 22, null, null, null, TestTime);
        factory.Db.AddCustomer(otherCustomer);
        factory.Db.AddManagedNode(otherNode);
        using var client = factory.CreateClient();

        using var response = await client.GetAsync(
            $"/api/v1/customers/{seeded.Customer.Id}/managed-nodes/{otherNode.Id}");

        Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
    }

    private static DateTimeOffset TestTime => new(2026, 4, 27, 9, 0, 0, TimeSpan.Zero);

    private sealed record Seeded(User User, Customer Customer);

    private sealed class NodeApiFactory(CustomerRole role) : WebApplicationFactory<Program>
    {
        private const string Provider = "fake";
        private const string Subject = "node-user";
        private const string Email = "node-user@nodecontrol.local";
        private const string DisplayName = "Node User";

        public NodeApiDbContext Db { get; } = new();

        public static NodeApiFactory Create(CustomerRole role)
        {
            return new NodeApiFactory(role);
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
                    ["Auth:Fake:DisplayName"] = DisplayName,
                    ["Auth:Fake:IsPlatformAdmin"] = "false"
                });
            });

            builder.ConfigureServices(services =>
            {
                services.RemoveAll<INodeControlDbContext>();
                services.AddSingleton<INodeControlDbContext>(Db);
            });
        }
    }

    private sealed class NodeApiDbContext : INodeControlDbContext
    {
        private readonly object syncRoot = new();

        public List<User> Users { get; } = [];

        public List<ExternalIdentity> ExternalIdentities { get; } = [];

        public List<Customer> Customers { get; } = [];

        public List<CustomerMembership> CustomerMemberships { get; } = [];

        public List<ManagedNode> ManagedNodes { get; } = [];

        public List<InventoryGroup> InventoryGroups { get; } = [];

        public List<InventoryGroupNode> InventoryGroupNodes { get; } = [];

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

        public Task<Customer?> FindCustomerAsync(Guid id, CancellationToken cancellationToken)
        {
            lock (syncRoot)
            {
                return Task.FromResult(Customers.FirstOrDefault(customer => customer.Id == id));
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

        public Task<InventoryGroup?> FindInventoryGroupAsync(
            Guid customerId,
            Guid inventoryGroupId,
            CancellationToken cancellationToken)
        {
            lock (syncRoot)
            {
                return Task.FromResult(InventoryGroups.FirstOrDefault(group =>
                    group.CustomerId == customerId && group.Id == inventoryGroupId));
            }
        }

        public Task<IReadOnlyList<ManagedNode>> ListActiveManagedNodesForInventoryGroupAsync(
            Guid inventoryGroupId,
            CancellationToken cancellationToken)
        {
            lock (syncRoot)
            {
                return Task.FromResult<IReadOnlyList<ManagedNode>>(
                    InventoryGroupNodes
                        .Where(link => link.InventoryGroupId == inventoryGroupId)
                        .Join(
                            ManagedNodes,
                            link => link.ManagedNodeId,
                            managedNode => managedNode.Id,
                            (_, managedNode) => managedNode)
                        .Where(managedNode => managedNode.Status == ManagedNodeStatus.Active)
                        .ToArray());
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

        public void AddManagedNode(ManagedNode managedNode)
        {
            lock (syncRoot)
            {
                ManagedNodes.Add(managedNode);
            }
        }

        public void AddInventoryGroup(InventoryGroup inventoryGroup)
        {
            lock (syncRoot)
            {
                InventoryGroups.Add(inventoryGroup);
            }
        }

        public void AddInventoryGroupNode(InventoryGroupNode inventoryGroupNode)
        {
            lock (syncRoot)
            {
                InventoryGroupNodes.Add(inventoryGroupNode);
            }
        }

        public Task<int> SaveChangesAsync(CancellationToken cancellationToken)
        {
            return Task.FromResult(1);
        }
    }
}
