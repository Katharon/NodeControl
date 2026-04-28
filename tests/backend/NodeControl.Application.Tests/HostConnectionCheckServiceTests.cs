using NodeControl.Application.Audit;
using NodeControl.Application.Abstractions.HostConnectivity;
using NodeControl.Application.Abstractions.Time;
using NodeControl.Application.Authorization;
using NodeControl.Application.Auth;
using NodeControl.Application.Customers;
using NodeControl.Application.HostConnectionChecks;
using NodeControl.Domain.Customers;
using NodeControl.Domain.Nodes;
using NodeControl.Domain.Users;

namespace NodeControl.Application.Tests;

public sealed class HostConnectionCheckServiceTests
{
    [Fact]
    public async Task Owner_can_queue_control_node_check()
    {
        var context = new NodeControlTestDbContext();
        var service = CreateService(context);
        var user = SeedUser(context, CustomerRole.Owner);
        var controlNode = ControlNode.Create(user.Customer.Id, "control-1", "127.0.0.1", 22, null, TestTime);
        context.AddControlNode(controlNode);

        var result = await service.QueueControlNodeCheckAsync(
            CurrentUser(user.User),
            user.Customer.Id,
            controlNode.Id,
            CancellationToken.None);

        Assert.Null(result.Error);
        var check = Assert.Single(context.HostConnectionChecks);
        Assert.Equal(controlNode.Id, check.ControlNodeId);
        Assert.Equal(HostConnectionCheckStatus.Queued, check.Status);
        Assert.Single(context.AuditLogEntries);
    }

    [Fact]
    public async Task Viewer_cannot_queue_managed_node_check()
    {
        var context = new NodeControlTestDbContext();
        var service = CreateService(context);
        var user = SeedUser(context, CustomerRole.Viewer);
        var managedNode = ManagedNode.Create(user.Customer.Id, "host_1", "127.0.0.1", 22, null, null, null, TestTime);
        context.AddManagedNode(managedNode);

        var result = await service.QueueManagedNodeCheckAsync(
            CurrentUser(user.User),
            user.Customer.Id,
            managedNode.Id,
            CancellationToken.None);

        Assert.Equal(CustomerServiceError.Forbidden, result.Error);
        Assert.Empty(context.HostConnectionChecks);
    }

    [Fact]
    public async Task Queue_control_node_check_rejects_cross_tenant_node()
    {
        var context = new NodeControlTestDbContext();
        var service = CreateService(context);
        var user = SeedUser(context, CustomerRole.Owner);
        var otherCustomer = Customer.Create("Other Customer", "other-customer", null, TestTime);
        var otherControlNode = ControlNode.Create(otherCustomer.Id, "control-2", "127.0.0.1", 22, null, TestTime);
        context.AddCustomer(otherCustomer);
        context.AddControlNode(otherControlNode);

        var result = await service.QueueControlNodeCheckAsync(
            CurrentUser(user.User),
            user.Customer.Id,
            otherControlNode.Id,
            CancellationToken.None);

        Assert.Equal(CustomerServiceError.NotFound, result.Error);
        Assert.Empty(context.HostConnectionChecks);
    }

    [Fact]
    public async Task Viewer_can_list_host_connection_checks()
    {
        var context = new NodeControlTestDbContext();
        var service = CreateService(context);
        var user = SeedUser(context, CustomerRole.Viewer);
        var managedNode = ManagedNode.Create(user.Customer.Id, "host_1", "127.0.0.1", 22, null, null, null, TestTime);
        var check = HostConnectionCheck.CreateForManagedNode(managedNode, user.User.Id, TestTime);
        context.AddManagedNode(managedNode);
        context.AddHostConnectionCheck(check);

        var result = await service.ListAsync(CurrentUser(user.User), user.Customer.Id, cancellationToken: CancellationToken.None);

        Assert.Null(result.Error);
        var dto = Assert.Single(result.Value!);
        Assert.Equal(check.Id, dto.Id);
    }

    [Fact]
    public async Task Processor_marks_successful_check_as_succeeded()
    {
        var context = new NodeControlTestDbContext();
        var user = SeedUser(context, CustomerRole.Owner);
        var managedNode = ManagedNode.Create(user.Customer.Id, "host_1", "127.0.0.1", 22, null, null, null, TestTime);
        var check = HostConnectionCheck.CreateForManagedNode(managedNode, user.User.Id, TestTime);
        context.AddManagedNode(managedNode);
        context.AddHostConnectionCheck(check);
        var clock = new TestClock(TestTime.AddSeconds(1));
        var processor = new HostConnectionCheckProcessor(
            context,
            new FakeConnectivityChecker(new HostConnectivityCheckResult(true, false, "reachable")),
            clock,
            new AuditLogWriter(context, clock, new EmptyRequestAuditContext()));

        var processed = await processor.ProcessOldestQueuedAsync(CancellationToken.None);

        Assert.True(processed);
        Assert.Equal(HostConnectionCheckStatus.Succeeded, check.Status);
        Assert.Equal("reachable", check.ResultMessage);
        Assert.NotNull(check.DurationMs);
        Assert.Single(context.AuditLogEntries);
    }

    [Fact]
    public async Task Processor_marks_timeout_check_as_timed_out()
    {
        var context = new NodeControlTestDbContext();
        var user = SeedUser(context, CustomerRole.Owner);
        var managedNode = ManagedNode.Create(user.Customer.Id, "host_1", "192.0.2.10", 22, null, null, null, TestTime);
        var check = HostConnectionCheck.CreateForManagedNode(managedNode, user.User.Id, TestTime);
        context.AddManagedNode(managedNode);
        context.AddHostConnectionCheck(check);
        var clock = new TestClock(TestTime.AddSeconds(1));
        var processor = new HostConnectionCheckProcessor(
            context,
            new FakeConnectivityChecker(new HostConnectivityCheckResult(false, true, "timed out")),
            clock,
            new AuditLogWriter(context, clock, new EmptyRequestAuditContext()));

        await processor.ProcessOldestQueuedAsync(CancellationToken.None);

        Assert.Equal(HostConnectionCheckStatus.TimedOut, check.Status);
        Assert.Equal("timed out", check.ErrorMessage);
    }

    private static DateTimeOffset TestTime => new(2026, 4, 28, 8, 0, 0, TimeSpan.Zero);

    private static HostConnectionCheckService CreateService(NodeControlTestDbContext context)
    {
        var clock = new TestClock(TestTime);
        return new HostConnectionCheckService(
            context,
            new CustomerAuthorizationService(context),
            clock,
            new AuditLogWriter(context, clock, new EmptyRequestAuditContext()));
    }

    private static SeededUser SeedUser(NodeControlTestDbContext context, CustomerRole role)
    {
        var user = User.Create("Normal User", "normal@nodecontrol.local", false, TestTime);
        var customer = Customer.Create("Customer A", "customer-a", null, TestTime);
        context.AddUser(user);
        context.AddCustomer(customer);
        context.AddCustomerMembership(CustomerMembership.Create(customer, user, role, TestTime));
        return new SeededUser(user, customer);
    }

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

    private sealed record SeededUser(User User, Customer Customer);

    private sealed class TestClock(DateTimeOffset utcNow) : IClock
    {
        public DateTimeOffset UtcNow { get; set; } = utcNow;
    }

    private sealed class FakeConnectivityChecker(HostConnectivityCheckResult result) : IHostConnectivityChecker
    {
        public Task<HostConnectivityCheckResult> CheckTcpAsync(
            string hostname,
            int port,
            TimeSpan timeout,
            CancellationToken cancellationToken)
        {
            return Task.FromResult(result);
        }
    }
}
