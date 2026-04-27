using NodeControl.Application.Audit;
using NodeControl.Application.Abstractions.Time;
using NodeControl.Application.Auth;
using NodeControl.Application.Authorization;
using NodeControl.Application.Customers;
using NodeControl.Application.Schedules;
using NodeControl.Domain.Customers;
using NodeControl.Domain.Jobs;
using NodeControl.Domain.Users;

namespace NodeControl.Application.Tests;

public sealed class JobScheduleServiceTests
{
    [Fact]
    public async Task JobScheduleService_creates_schedule_when_user_has_manage_schedules()
    {
        var fixture = TestFixture.Create(CustomerRole.Owner);

        var result = await fixture.CreateScheduleService().CreateAsync(
            fixture.CurrentUser,
            fixture.Customer.Id,
            fixture.ValidCreateRequest("nightly"));

        Assert.Null(result.Error);
        Assert.Single(fixture.Db.JobSchedules);
        Assert.Equal("Active", result.Value!.Status);
        Assert.NotNull(result.Value.NextRunAtUtc);
        Assert.Equal("schedule.created", Assert.Single(fixture.Db.AuditLogEntries).Action);
    }

    [Fact]
    public async Task JobScheduleService_rejects_create_when_user_lacks_manage_schedules()
    {
        var fixture = TestFixture.Create(CustomerRole.Viewer);

        var result = await fixture.CreateScheduleService().CreateAsync(
            fixture.CurrentUser,
            fixture.Customer.Id,
            fixture.ValidCreateRequest("nightly"));

        Assert.Equal(CustomerServiceError.Forbidden, result.Error);
        Assert.Empty(fixture.Db.JobSchedules);
    }

    [Fact]
    public async Task JobScheduleService_rejects_duplicate_slug_within_same_customer()
    {
        var fixture = TestFixture.Create(CustomerRole.Owner);
        var service = fixture.CreateScheduleService();

        await service.CreateAsync(fixture.CurrentUser, fixture.Customer.Id, fixture.ValidCreateRequest("nightly"));
        var result = await service.CreateAsync(fixture.CurrentUser, fixture.Customer.Id, fixture.ValidCreateRequest("nightly"));

        Assert.Equal(CustomerServiceError.Conflict, result.Error);
    }

    [Fact]
    public async Task JobScheduleService_allows_same_slug_in_different_customers()
    {
        var fixture = TestFixture.Create(CustomerRole.Owner);
        var other = fixture.AddOtherCustomer();
        var service = fixture.CreateScheduleService();

        var first = await service.CreateAsync(fixture.CurrentUser, fixture.Customer.Id, fixture.ValidCreateRequest("nightly"));
        var second = await service.CreateAsync(other.CurrentUser, other.Customer.Id, fixture.ValidCreateRequest("nightly", other.Job));

        Assert.Null(first.Error);
        Assert.Null(second.Error);
        Assert.Equal(2, fixture.Db.JobSchedules.Count);
    }

    [Fact]
    public async Task JobScheduleService_rejects_job_from_different_customer()
    {
        var fixture = TestFixture.Create(CustomerRole.Owner);
        var other = fixture.AddOtherCustomer();

        var result = await fixture.CreateScheduleService().CreateAsync(
            fixture.CurrentUser,
            fixture.Customer.Id,
            fixture.ValidCreateRequest("nightly", other.Job));

        Assert.Equal(CustomerServiceError.BadRequest, result.Error);
    }

    [Fact]
    public async Task JobScheduleService_rejects_archived_job()
    {
        var fixture = TestFixture.Create(CustomerRole.Owner);
        fixture.Job.Archive(TestTime);

        var result = await fixture.CreateScheduleService().CreateAsync(
            fixture.CurrentUser,
            fixture.Customer.Id,
            fixture.ValidCreateRequest("nightly"));

        Assert.Equal(CustomerServiceError.BadRequest, result.Error);
    }

    [Theory]
    [InlineData("not cron", "UTC")]
    [InlineData("60 * * * *", "UTC")]
    [InlineData("0 * * * *", "Not/AZone")]
    public async Task JobScheduleService_rejects_invalid_schedule_definition(string cronExpression, string timeZoneId)
    {
        var fixture = TestFixture.Create(CustomerRole.Owner);

        var result = await fixture.CreateScheduleService().CreateAsync(
            fixture.CurrentUser,
            fixture.Customer.Id,
            fixture.ValidCreateRequest("nightly") with { CronExpression = cronExpression, TimeZoneId = timeZoneId });

        Assert.Equal(CustomerServiceError.BadRequest, result.Error);
    }

    [Fact]
    public async Task JobScheduleService_updates_schedule_and_recomputes_next_run()
    {
        var fixture = TestFixture.Create(CustomerRole.Owner);
        var create = await fixture.CreateScheduleService().CreateAsync(
            fixture.CurrentUser,
            fixture.Customer.Id,
            fixture.ValidCreateRequest("nightly"));

        fixture.Clock.UtcNow = TestTime.AddHours(1);
        var result = await fixture.CreateScheduleService().UpdateAsync(
            fixture.CurrentUser,
            fixture.Customer.Id,
            create.Value!.Id,
            new UpdateJobScheduleRequest("Hourly", "hourly", null, fixture.Job.Id, "0 * * * *", "UTC"));

        Assert.Null(result.Error);
        Assert.Equal("hourly", result.Value!.Slug);
        Assert.True(result.Value.NextRunAtUtc > fixture.Clock.UtcNow);
        Assert.Equal("schedule.updated", fixture.Db.AuditLogEntries.Last().Action);
    }

    [Fact]
    public async Task JobScheduleService_pauses_resumes_and_archives_schedule()
    {
        var fixture = TestFixture.Create(CustomerRole.Owner);
        var create = await fixture.CreateScheduleService().CreateAsync(
            fixture.CurrentUser,
            fixture.Customer.Id,
            fixture.ValidCreateRequest("nightly"));

        var paused = await fixture.CreateScheduleService().PauseAsync(fixture.CurrentUser, fixture.Customer.Id, create.Value!.Id);
        var resumed = await fixture.CreateScheduleService().ResumeAsync(fixture.CurrentUser, fixture.Customer.Id, create.Value.Id);
        var archived = await fixture.CreateScheduleService().ArchiveAsync(fixture.CurrentUser, fixture.Customer.Id, create.Value.Id);

        Assert.Equal("Paused", paused.Value!.Status);
        Assert.Null(paused.Value.NextRunAtUtc);
        Assert.Equal("Active", resumed.Value!.Status);
        Assert.NotNull(resumed.Value.NextRunAtUtc);
        Assert.Equal("Archived", archived.Value!.Status);
        Assert.NotNull(fixture.Db.JobSchedules.Single().ArchivedAt);
        Assert.Contains(fixture.Db.AuditLogEntries, entry => entry.Action == "schedule.paused");
        Assert.Contains(fixture.Db.AuditLogEntries, entry => entry.Action == "schedule.resumed");
        Assert.Contains(fixture.Db.AuditLogEntries, entry => entry.Action == "schedule.archived");
    }

    [Fact]
    public async Task List_requires_view_schedules()
    {
        var fixture = TestFixture.Create(CustomerRole.Auditor);

        var result = await fixture.CreateScheduleService().ListAsync(fixture.CurrentUser, fixture.Customer.Id);

        Assert.Equal(CustomerServiceError.Forbidden, result.Error);
    }

    [Fact]
    public async Task Cross_tenant_get_update_pause_resume_archive_are_rejected()
    {
        var fixture = TestFixture.Create(CustomerRole.Owner);
        var other = fixture.AddOtherCustomer();
        var create = await fixture.CreateScheduleService().CreateAsync(
            other.CurrentUser,
            other.Customer.Id,
            fixture.ValidCreateRequest("nightly", other.Job));

        var service = fixture.CreateScheduleService();

        Assert.Equal(CustomerServiceError.NotFound, (await service.GetAsync(fixture.CurrentUser, fixture.Customer.Id, create.Value!.Id)).Error);
        Assert.Equal(CustomerServiceError.NotFound, (await service.UpdateAsync(fixture.CurrentUser, fixture.Customer.Id, create.Value.Id, new UpdateJobScheduleRequest("x", "xx", null, fixture.Job.Id, "0 * * * *", "UTC"))).Error);
        Assert.Equal(CustomerServiceError.NotFound, (await service.PauseAsync(fixture.CurrentUser, fixture.Customer.Id, create.Value.Id)).Error);
        Assert.Equal(CustomerServiceError.NotFound, (await service.ResumeAsync(fixture.CurrentUser, fixture.Customer.Id, create.Value.Id)).Error);
        Assert.Equal(CustomerServiceError.NotFound, (await service.ArchiveAsync(fixture.CurrentUser, fixture.Customer.Id, create.Value.Id)).Error);
    }

    [Fact]
    public async Task ScheduledJobRunService_creates_queued_job_run_for_due_active_schedule()
    {
        var fixture = TestFixture.Create(CustomerRole.Owner);
        var schedule = fixture.AddDueSchedule();

        var count = await fixture.CreateScheduledJobRunService().EnqueueDueSchedulesAsync();

        var jobRun = Assert.Single(fixture.Db.JobRuns);
        Assert.Equal(1, count);
        Assert.Equal(JobRunStatus.Queued, jobRun.Status);
        Assert.Equal(JobRunTriggerType.Scheduled, jobRun.TriggerType);
        Assert.Equal(schedule.Id, jobRun.ScheduleId);
        Assert.Null(jobRun.TriggeredByUserId);
        Assert.Equal(TestTime, schedule.LastRunAtUtc);
        Assert.Equal(jobRun.Id, schedule.LastJobRunId);
        Assert.True(schedule.NextRunAtUtc > TestTime);
        Assert.Equal("job_run.created_scheduled", Assert.Single(fixture.Db.AuditLogEntries).Action);
    }

    [Theory]
    [InlineData(JobScheduleStatus.Paused)]
    [InlineData(JobScheduleStatus.Archived)]
    public async Task ScheduledJobRunService_does_not_create_job_run_for_inactive_schedule(JobScheduleStatus status)
    {
        var fixture = TestFixture.Create(CustomerRole.Owner);
        var schedule = fixture.AddDueSchedule();
        if (status == JobScheduleStatus.Paused)
        {
            schedule.Pause(TestTime);
        }
        else
        {
            schedule.Archive(TestTime);
        }

        var count = await fixture.CreateScheduledJobRunService().EnqueueDueSchedulesAsync();

        Assert.Equal(0, count);
        Assert.Empty(fixture.Db.JobRuns);
    }

    [Fact]
    public async Task ScheduledJobRunService_does_not_create_job_run_when_job_is_archived()
    {
        var fixture = TestFixture.Create(CustomerRole.Owner);
        fixture.AddDueSchedule();
        fixture.Job.Archive(TestTime);

        var count = await fixture.CreateScheduledJobRunService().EnqueueDueSchedulesAsync();

        Assert.Equal(0, count);
        Assert.Empty(fixture.Db.JobRuns);
    }

    private static DateTimeOffset TestTime => new(2026, 4, 27, 10, 0, 0, TimeSpan.Zero);

    private static CurrentUserDto CurrentUser(User user)
    {
        return new CurrentUserDto(user.Id, user.DisplayName, user.Email, true, false, "fake", user.Email);
    }

    private sealed record CustomerJob(Customer Customer, CurrentUserDto CurrentUser, Job Job);

    private sealed class TestFixture
    {
        private TestFixture(NodeControlTestDbContext db, Customer customer, CurrentUserDto currentUser, Job job, TestClock clock)
        {
            Db = db;
            Customer = customer;
            CurrentUser = currentUser;
            Job = job;
            Clock = clock;
        }

        public NodeControlTestDbContext Db { get; }

        public Customer Customer { get; }

        public CurrentUserDto CurrentUser { get; }

        public Job Job { get; }

        public TestClock Clock { get; }

        public static TestFixture Create(CustomerRole role)
        {
            var db = new NodeControlTestDbContext();
            var clock = new TestClock();
            var user = User.Create("Test User", "test@nodecontrol.local", false, TestTime);
            var customer = Customer.Create("Customer A", "customer-a", null, TestTime);
            db.AddUser(user);
            db.AddCustomer(customer);
            db.AddCustomerMembership(CustomerMembership.Create(customer, user, role, TestTime));
            var job = AddJob(db, customer.Id, "a");
            return new TestFixture(db, customer, CurrentUser(user), job, clock);
        }

        public CustomerJob AddOtherCustomer()
        {
            var user = User.Create("Other User", "other@nodecontrol.local", false, TestTime);
            var customer = Customer.Create("Customer B", "customer-b", null, TestTime);
            Db.AddUser(user);
            Db.AddCustomer(customer);
            Db.AddCustomerMembership(CustomerMembership.Create(customer, user, CustomerRole.Owner, TestTime));
            var job = AddJob(Db, customer.Id, "b");
            return new CustomerJob(customer, CurrentUser(user), job);
        }

        public CreateJobScheduleRequest ValidCreateRequest(string slug, Job? job = null)
        {
            return new CreateJobScheduleRequest(
                "Nightly",
                slug,
                null,
                (job ?? Job).Id,
                "0 * * * *",
                "UTC");
        }

        public JobSchedule AddDueSchedule()
        {
            var schedule = JobSchedule.Create(
                Job,
                "Nightly",
                $"nightly-{Db.JobSchedules.Count + 1}",
                null,
                "0 * * * *",
                "UTC",
                TestTime.AddMinutes(-1),
                TestTime.AddHours(-1));
            Db.AddJobSchedule(schedule);
            return schedule;
        }

        public JobScheduleService CreateScheduleService()
        {
            return new JobScheduleService(Db, new CustomerAuthorizationService(Db), new CronScheduleCalculator(), Clock, CreateAuditWriter());
        }

        public ScheduledJobRunService CreateScheduledJobRunService()
        {
            return new ScheduledJobRunService(Db, new CronScheduleCalculator(), Clock, CreateAuditWriter());
        }

        private AuditLogWriter CreateAuditWriter()
        {
            return new AuditLogWriter(Db, Clock, new EmptyRequestAuditContext());
        }

        private static Job AddJob(NodeControlTestDbContext db, Guid customerId, string suffix)
        {
            var job = Job.Create(
                customerId,
                $"Deploy {suffix}",
                $"deploy-{suffix}",
                null,
                Guid.NewGuid(),
                Guid.NewGuid(),
                Guid.NewGuid(),
                null,
                1800,
                TestTime);
            db.AddJob(job);
            return job;
        }
    }

    private sealed class TestClock : IClock
    {
        public DateTimeOffset UtcNow { get; set; } = TestTime;
    }
}
