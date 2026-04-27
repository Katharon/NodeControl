using NodeControl.Application.Abstractions.Time;
using NodeControl.Application.Audit;
using NodeControl.Application.Auth;
using NodeControl.Application.Authorization;
using NodeControl.Application.Customers;
using NodeControl.Domain.Audit;
using NodeControl.Domain.Customers;
using NodeControl.Domain.Users;

namespace NodeControl.Application.Tests;

public sealed class AuditLogServiceTests
{
    [Fact]
    public async Task AuditLogWriter_creates_entry_with_expected_fields()
    {
        var fixture = TestFixture.Create(CustomerRole.Owner);
        var entityId = Guid.NewGuid();

        await fixture.CreateWriter().WriteAsync(new AuditLogWriteRequest(
            fixture.Customer.Id,
            fixture.CurrentUser.Id,
            fixture.CurrentUser.DisplayName,
            AuditActorType.User,
            "job.created",
            "Job",
            entityId,
            "Deploy App",
            AuditOutcome.Succeeded,
            "Job 'Deploy App' was created.",
            """{"jobSlug":"deploy-app"}"""), CancellationToken.None);

        var entry = Assert.Single(fixture.Db.AuditLogEntries);
        Assert.Equal(fixture.Customer.Id, entry.CustomerId);
        Assert.Equal(fixture.CurrentUser.Id, entry.ActorUserId);
        Assert.Equal("job.created", entry.Action);
        Assert.Equal("Job", entry.EntityType);
        Assert.Equal(entityId, entry.EntityId);
        Assert.Equal(AuditOutcome.Succeeded, entry.Outcome);
        Assert.Equal("Job 'Deploy App' was created.", entry.Message);
        Assert.Equal(TestTime, entry.CreatedAtUtc);
    }

    [Fact]
    public async Task AuditLogWriter_allows_global_entries_without_customer()
    {
        var fixture = TestFixture.Create(CustomerRole.Owner);

        await fixture.CreateWriter().WriteAsync(new AuditLogWriteRequest(
            null,
            null,
            null,
            AuditActorType.System,
            "system.started",
            "System",
            null,
            null,
            AuditOutcome.Succeeded,
            "System started."), CancellationToken.None);

        Assert.Null(Assert.Single(fixture.Db.AuditLogEntries).CustomerId);
    }

    [Fact]
    public async Task List_requires_view_audit_logs()
    {
        var fixture = TestFixture.Create(CustomerRole.Viewer);

        var result = await fixture.CreateService().ListAsync(
            fixture.CurrentUser,
            fixture.Customer.Id,
            EmptyQuery);

        Assert.Equal(CustomerServiceError.Forbidden, result.Error);
    }

    [Fact]
    public async Task Auditor_can_list_customer_audit_entries_newest_first()
    {
        var fixture = TestFixture.Create(CustomerRole.Auditor);
        var older = fixture.AddEntry("job.created", "Job", Guid.NewGuid(), AuditOutcome.Succeeded, TestTime.AddMinutes(-5));
        var newer = fixture.AddEntry("schedule.updated", "Schedule", Guid.NewGuid(), AuditOutcome.Succeeded, TestTime);

        var result = await fixture.CreateService().ListAsync(fixture.CurrentUser, fixture.Customer.Id, EmptyQuery);

        Assert.Null(result.Error);
        Assert.Equal([newer.Id, older.Id], result.Value!.Items.Select(item => item.Id).ToArray());
    }

    [Fact]
    public async Task List_does_not_return_entries_from_another_customer()
    {
        var fixture = TestFixture.Create(CustomerRole.Auditor);
        var other = fixture.AddOtherCustomer();
        var own = fixture.AddEntry("job.created", "Job", Guid.NewGuid(), AuditOutcome.Succeeded, TestTime);
        fixture.AddEntry("job.created", "Job", Guid.NewGuid(), AuditOutcome.Succeeded, TestTime, other.Customer.Id);

        var result = await fixture.CreateService().ListAsync(fixture.CurrentUser, fixture.Customer.Id, EmptyQuery);

        var item = Assert.Single(result.Value!.Items);
        Assert.Equal(own.Id, item.Id);
    }

    [Fact]
    public async Task Detail_loads_by_customer_and_entry_id()
    {
        var fixture = TestFixture.Create(CustomerRole.Auditor);
        var other = fixture.AddOtherCustomer();
        var own = fixture.AddEntry("job.created", "Job", Guid.NewGuid(), AuditOutcome.Succeeded, TestTime);
        var crossTenant = fixture.AddEntry("job.created", "Job", Guid.NewGuid(), AuditOutcome.Succeeded, TestTime, other.Customer.Id);

        var ownResult = await fixture.CreateService().GetAsync(fixture.CurrentUser, fixture.Customer.Id, own.Id);
        var crossTenantResult = await fixture.CreateService().GetAsync(fixture.CurrentUser, fixture.Customer.Id, crossTenant.Id);

        Assert.Null(ownResult.Error);
        Assert.Equal(CustomerServiceError.NotFound, crossTenantResult.Error);
    }

    [Fact]
    public async Task List_respects_limit_and_caps_max_limit()
    {
        var fixture = TestFixture.Create(CustomerRole.Auditor);
        for (var i = 0; i < 510; i++)
        {
            fixture.AddEntry("job.created", "Job", Guid.NewGuid(), AuditOutcome.Succeeded, TestTime.AddMinutes(i));
        }

        var limited = await fixture.CreateService().ListAsync(
            fixture.CurrentUser,
            fixture.Customer.Id,
            EmptyQuery with { Limit = 3 });
        var capped = await fixture.CreateService().ListAsync(
            fixture.CurrentUser,
            fixture.Customer.Id,
            EmptyQuery with { Limit = 1000 });

        Assert.Equal(3, limited.Value!.Items.Count);
        Assert.Equal(AuditLogService.MaxLimit, capped.Value!.Items.Count);
    }

    [Fact]
    public async Task List_filters_by_supported_fields()
    {
        var fixture = TestFixture.Create(CustomerRole.Auditor);
        var entityId = Guid.NewGuid();
        var actorId = fixture.CurrentUser.Id;
        var matching = fixture.AddEntry("job.updated", "Job", entityId, AuditOutcome.Failed, TestTime, actorUserId: actorId);
        fixture.AddEntry("schedule.updated", "Schedule", Guid.NewGuid(), AuditOutcome.Succeeded, TestTime.AddMinutes(-10));

        var result = await fixture.CreateService().ListAsync(
            fixture.CurrentUser,
            fixture.Customer.Id,
            new AuditLogQuery(
                "job.updated",
                "Job",
                entityId,
                actorId,
                "Failed",
                TestTime.AddMinutes(-1),
                TestTime.AddMinutes(1),
                null));

        var item = Assert.Single(result.Value!.Items);
        Assert.Equal(matching.Id, item.Id);
    }

    private static AuditLogQuery EmptyQuery => new(null, null, null, null, null, null, null, null);

    private static DateTimeOffset TestTime => new(2026, 4, 27, 10, 0, 0, TimeSpan.Zero);

    private static CurrentUserDto CurrentUser(User user)
    {
        return new CurrentUserDto(user.Id, user.DisplayName, user.Email, true, false, "fake", user.Email);
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

        public AuditLogEntry AddEntry(
            string action,
            string entityType,
            Guid entityId,
            AuditOutcome outcome,
            DateTimeOffset createdAtUtc,
            Guid? customerId = null,
            Guid? actorUserId = null)
        {
            var entry = AuditLogEntry.Create(
                customerId ?? Customer.Id,
                actorUserId,
                actorUserId is null ? null : CurrentUser.DisplayName,
                actorUserId is null ? AuditActorType.System : AuditActorType.User,
                action,
                entityType,
                entityId,
                entityType,
                outcome,
                $"{action} message",
                null,
                null,
                null,
                createdAtUtc);
            Db.AddAuditLogEntry(entry);
            return entry;
        }

        public AuditLogWriter CreateWriter()
        {
            return new AuditLogWriter(Db, clock, new EmptyRequestAuditContext());
        }

        public AuditLogService CreateService()
        {
            return new AuditLogService(Db, new CustomerAuthorizationService(Db), CreateWriter());
        }
    }

    private sealed class TestClock : IClock
    {
        public DateTimeOffset UtcNow { get; set; } = TestTime;
    }
}
