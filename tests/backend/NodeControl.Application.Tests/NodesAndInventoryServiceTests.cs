using NodeControl.Application.Auth;
using NodeControl.Application.Authorization;
using NodeControl.Application.ControlNodes;
using NodeControl.Application.Customers;
using NodeControl.Application.InventoryGroups;
using NodeControl.Application.ManagedNodes;
using NodeControl.Application.Abstractions.Time;
using NodeControl.Domain.Customers;
using NodeControl.Domain.Inventories;
using NodeControl.Domain.Nodes;
using NodeControl.Domain.Secrets;
using NodeControl.Domain.Users;

namespace NodeControl.Application.Tests;

public sealed class NodesAndInventoryServiceTests
{
    [Fact]
    public async Task ControlNodeService_creates_control_node_when_user_has_manage_nodes()
    {
        var fixture = TestFixture.Create(CustomerRole.Owner);
        var service = fixture.CreateControlNodeService();

        var result = await service.CreateAsync(fixture.CurrentUser, fixture.Customer.Id, new CreateControlNodeRequest(
            "Primary control",
            "control-01.local"));

        Assert.Null(result.Error);
        Assert.NotNull(result.Value);
        Assert.Single(fixture.Db.ControlNodes);
    }

    [Fact]
    public async Task ControlNodeService_rejects_create_when_user_only_has_view_nodes()
    {
        var fixture = TestFixture.Create(CustomerRole.Viewer);
        var service = fixture.CreateControlNodeService();

        var result = await service.CreateAsync(fixture.CurrentUser, fixture.Customer.Id, new CreateControlNodeRequest(
            "Primary control",
            "control-01.local"));

        Assert.Equal(CustomerServiceError.Forbidden, result.Error);
        Assert.Empty(fixture.Db.ControlNodes);
    }

    [Fact]
    public async Task ControlNodeService_creates_control_node_with_ssh_private_key_secret_reference()
    {
        var fixture = TestFixture.Create(CustomerRole.Owner);
        var secret = Secret.Create(fixture.Customer.Id, "Control key", "control-key", null, SecretKind.SshPrivateKey, "protected-key", TestTime);
        fixture.Db.AddSecret(secret);

        var result = await fixture.CreateControlNodeService().CreateAsync(
            fixture.CurrentUser,
            fixture.Customer.Id,
            new CreateControlNodeRequest(
                "Remote control",
                "control-01.example.test",
                22,
                "ansible",
                secret.Id,
                "/var/lib/nodecontrol/remote-runs"));

        Assert.Null(result.Error);
        Assert.Equal("ansible", result.Value!.SshUsername);
        Assert.Equal(secret.Id, result.Value.SshPrivateKeySecretId);
        Assert.Equal("/var/lib/nodecontrol/remote-runs", result.Value.RemoteWorkspaceRoot);
    }

    [Fact]
    public async Task ControlNodeService_rejects_remote_credential_secret_from_another_customer()
    {
        var fixture = TestFixture.Create(CustomerRole.Owner);
        var otherCustomer = Customer.Create("Other Customer", "other-customer", null, TestTime);
        var otherSecret = Secret.Create(otherCustomer.Id, "Control key", "control-key", null, SecretKind.SshPrivateKey, "protected-key", TestTime);
        fixture.Db.AddCustomer(otherCustomer);
        fixture.Db.AddSecret(otherSecret);

        var result = await fixture.CreateControlNodeService().CreateAsync(
            fixture.CurrentUser,
            fixture.Customer.Id,
            new CreateControlNodeRequest(
                "Remote control",
                "control-01.example.test",
                22,
                "ansible",
                otherSecret.Id,
                "/var/lib/nodecontrol/remote-runs"));

        Assert.Equal(CustomerServiceError.BadRequest, result.Error);
        Assert.Empty(fixture.Db.ControlNodes);
    }

    [Fact]
    public async Task ManagedNodeService_creates_managed_node_when_user_has_manage_nodes()
    {
        var fixture = TestFixture.Create(CustomerRole.Owner);
        var service = fixture.CreateManagedNodeService();

        var result = await service.CreateAsync(fixture.CurrentUser, fixture.Customer.Id, new CreateManagedNodeRequest(
            "web_01",
            "10.0.0.10"));

        Assert.Null(result.Error);
        Assert.NotNull(result.Value);
        Assert.Single(fixture.Db.ManagedNodes);
    }

    [Fact]
    public async Task ManagedNodeService_rejects_cross_tenant_access()
    {
        var fixture = TestFixture.Create(CustomerRole.Owner);
        var otherCustomer = Customer.Create("Other Customer", "other-customer", null, TestTime);
        fixture.Db.AddCustomer(otherCustomer);
        var service = fixture.CreateManagedNodeService();

        var result = await service.CreateAsync(fixture.CurrentUser, otherCustomer.Id, new CreateManagedNodeRequest(
            "web_01",
            "10.0.0.10"));

        Assert.Equal(CustomerServiceError.Forbidden, result.Error);
        Assert.Empty(fixture.Db.ManagedNodes);
    }

    [Fact]
    public async Task ManagedNodeService_archives_instead_of_hard_deleting()
    {
        var fixture = TestFixture.Create(CustomerRole.Owner);
        var managedNode = ManagedNode.Create(fixture.Customer.Id, "web_01", "10.0.0.10", 22, null, null, null, TestTime);
        fixture.Db.AddManagedNode(managedNode);
        var service = fixture.CreateManagedNodeService();

        var result = await service.ArchiveAsync(fixture.CurrentUser, fixture.Customer.Id, managedNode.Id);

        Assert.Null(result.Error);
        Assert.Single(fixture.Db.ManagedNodes);
        Assert.Equal(ManagedNodeStatus.Archived, fixture.Db.ManagedNodes[0].Status);
    }

    [Fact]
    public async Task InventoryGroupService_creates_group_when_user_has_manage_nodes()
    {
        var fixture = TestFixture.Create(CustomerRole.Owner);
        var service = fixture.CreateInventoryGroupService();

        var result = await service.CreateAsync(fixture.CurrentUser, fixture.Customer.Id, new CreateInventoryGroupRequest("webservers"));

        Assert.Null(result.Error);
        Assert.Single(fixture.Db.InventoryGroups);
    }

    [Fact]
    public async Task InventoryGroupService_rejects_duplicate_group_name_within_same_customer()
    {
        var fixture = TestFixture.Create(CustomerRole.Owner);
        var service = fixture.CreateInventoryGroupService();

        await service.CreateAsync(fixture.CurrentUser, fixture.Customer.Id, new CreateInventoryGroupRequest("webservers"));
        var result = await service.CreateAsync(fixture.CurrentUser, fixture.Customer.Id, new CreateInventoryGroupRequest("webservers"));

        Assert.Equal(CustomerServiceError.Conflict, result.Error);
    }

    [Fact]
    public async Task InventoryGroupService_allows_same_group_name_in_different_customers()
    {
        var fixture = TestFixture.Create(CustomerRole.Owner);
        var otherCustomer = Customer.Create("Other Customer", "other-customer", null, TestTime);
        var otherUser = User.Create("Other User", "other@nodecontrol.local", false, TestTime);
        fixture.Db.AddCustomer(otherCustomer);
        fixture.Db.AddUser(otherUser);
        fixture.Db.AddCustomerMembership(CustomerMembership.Create(otherCustomer, otherUser, CustomerRole.Owner, TestTime));
        var otherCurrentUser = CurrentUser(otherUser);
        var service = fixture.CreateInventoryGroupService();

        var first = await service.CreateAsync(fixture.CurrentUser, fixture.Customer.Id, new CreateInventoryGroupRequest("webservers"));
        var second = await service.CreateAsync(otherCurrentUser, otherCustomer.Id, new CreateInventoryGroupRequest("webservers"));

        Assert.Null(first.Error);
        Assert.Null(second.Error);
        Assert.Equal(2, fixture.Db.InventoryGroups.Count);
    }

    [Fact]
    public async Task InventoryGroupService_adds_managed_node_to_group_when_both_belong_to_same_customer()
    {
        var fixture = TestFixture.Create(CustomerRole.Owner);
        var group = InventoryGroup.Create(fixture.Customer.Id, "webservers", null, TestTime);
        var managedNode = ManagedNode.Create(fixture.Customer.Id, "web_01", "10.0.0.10", 22, null, null, null, TestTime);
        fixture.Db.AddInventoryGroup(group);
        fixture.Db.AddManagedNode(managedNode);
        var service = fixture.CreateInventoryGroupService();

        var result = await service.AddManagedNodeAsync(
            fixture.CurrentUser,
            fixture.Customer.Id,
            group.Id,
            new AddManagedNodeToInventoryGroupRequest(managedNode.Id));

        Assert.Null(result.Error);
        Assert.Single(fixture.Db.InventoryGroupNodes);
    }

    [Fact]
    public async Task InventoryGroupService_rejects_adding_managed_node_from_another_customer()
    {
        var fixture = TestFixture.Create(CustomerRole.Owner);
        var otherCustomer = Customer.Create("Other Customer", "other-customer", null, TestTime);
        var group = InventoryGroup.Create(fixture.Customer.Id, "webservers", null, TestTime);
        var otherNode = ManagedNode.Create(otherCustomer.Id, "web_01", "10.0.0.10", 22, null, null, null, TestTime);
        fixture.Db.AddCustomer(otherCustomer);
        fixture.Db.AddInventoryGroup(group);
        fixture.Db.AddManagedNode(otherNode);
        var service = fixture.CreateInventoryGroupService();

        var result = await service.AddManagedNodeAsync(
            fixture.CurrentUser,
            fixture.Customer.Id,
            group.Id,
            new AddManagedNodeToInventoryGroupRequest(otherNode.Id));

        Assert.Equal(CustomerServiceError.NotFound, result.Error);
        Assert.Empty(fixture.Db.InventoryGroupNodes);
    }

    [Fact]
    public async Task InventoryPreviewService_generates_yaml_preview()
    {
        var fixture = TestFixture.Create(CustomerRole.Viewer);
        var group = InventoryGroup.Create(fixture.Customer.Id, "webservers", null, TestTime);
        var managedNode = ManagedNode.Create(fixture.Customer.Id, "web_01", "10.0.0.10", 22, null, null, null, TestTime);
        fixture.Db.AddInventoryGroup(group);
        fixture.Db.AddManagedNode(managedNode);
        fixture.Db.AddInventoryGroupNode(InventoryGroupNode.Create(group, managedNode, TestTime));
        var service = fixture.CreateInventoryPreviewService();

        var result = await service.GetPreviewAsync(fixture.CurrentUser, fixture.Customer.Id, group.Id);

        Assert.Null(result.Error);
        Assert.NotNull(result.Value);
        Assert.Contains("webservers:", result.Value.Content);
        Assert.Contains("web_01:", result.Value.Content);
        Assert.Contains("ansible_host: 10.0.0.10", result.Value.Content);
        Assert.Contains("ansible_port: 22", result.Value.Content);
    }

    [Fact]
    public async Task InventoryPreviewService_excludes_archived_managed_nodes()
    {
        var fixture = TestFixture.Create(CustomerRole.Viewer);
        var group = InventoryGroup.Create(fixture.Customer.Id, "webservers", null, TestTime);
        var activeNode = ManagedNode.Create(fixture.Customer.Id, "web_01", "10.0.0.10", 22, null, null, null, TestTime);
        var archivedNode = ManagedNode.Create(fixture.Customer.Id, "web_02", "10.0.0.11", 22, null, null, null, TestTime);
        archivedNode.Archive(TestTime.AddMinutes(1));
        fixture.Db.AddInventoryGroup(group);
        fixture.Db.AddManagedNode(activeNode);
        fixture.Db.AddManagedNode(archivedNode);
        fixture.Db.AddInventoryGroupNode(InventoryGroupNode.Create(group, activeNode, TestTime));
        fixture.Db.AddInventoryGroupNode(InventoryGroupNode.Create(group, archivedNode, TestTime));
        var service = fixture.CreateInventoryPreviewService();

        var result = await service.GetPreviewAsync(fixture.CurrentUser, fixture.Customer.Id, group.Id);

        Assert.Null(result.Error);
        Assert.Contains("web_01:", result.Value!.Content);
        Assert.DoesNotContain("web_02:", result.Value.Content);
    }

    private static DateTimeOffset TestTime => new(2026, 4, 27, 9, 0, 0, TimeSpan.Zero);

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

    private sealed class TestClock : IClock
    {
        public DateTimeOffset UtcNow { get; set; } = TestTime;
    }

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

        public ControlNodeService CreateControlNodeService()
        {
            return new ControlNodeService(Db, new CustomerAuthorizationService(Db), clock);
        }

        public ManagedNodeService CreateManagedNodeService()
        {
            return new ManagedNodeService(Db, new CustomerAuthorizationService(Db), clock);
        }

        public InventoryGroupService CreateInventoryGroupService()
        {
            return new InventoryGroupService(Db, new CustomerAuthorizationService(Db), clock);
        }

        public InventoryPreviewService CreateInventoryPreviewService()
        {
            return new InventoryPreviewService(Db, new CustomerAuthorizationService(Db));
        }
    }
}
