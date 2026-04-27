using NodeControl.Application.Abstractions.Time;
using NodeControl.Application.Auth;
using NodeControl.Application.Authorization;
using NodeControl.Application.Customers;
using NodeControl.Application.JobRuns;
using NodeControl.Application.Jobs;
using NodeControl.Domain.Customers;
using NodeControl.Domain.Inventories;
using NodeControl.Domain.Jobs;
using NodeControl.Domain.Nodes;
using NodeControl.Domain.Playbooks;
using NodeControl.Domain.Users;
using NodeControl.Domain.VariableSets;

namespace NodeControl.Application.Tests;

public sealed class JobsAndJobRunsServiceTests
{
    [Fact]
    public async Task JobService_creates_job_when_user_has_manage_playbooks()
    {
        var fixture = TestFixture.Create(CustomerRole.Owner);

        var result = await fixture.CreateJobService().CreateAsync(
            fixture.CurrentUser,
            fixture.Customer.Id,
            fixture.ValidJobRequest("deploy-app"));

        Assert.Null(result.Error);
        Assert.Single(fixture.Db.Jobs);
        Assert.Equal(JobStatus.Active, result.Value!.Status);
        Assert.Equal(1800, result.Value.DefaultTimeoutSeconds);
    }

    [Fact]
    public async Task JobService_rejects_create_when_user_only_has_view_playbooks()
    {
        var fixture = TestFixture.Create(CustomerRole.Viewer);

        var result = await fixture.CreateJobService().CreateAsync(
            fixture.CurrentUser,
            fixture.Customer.Id,
            fixture.ValidJobRequest("deploy-app"));

        Assert.Equal(CustomerServiceError.Forbidden, result.Error);
        Assert.Empty(fixture.Db.Jobs);
    }

    [Fact]
    public async Task JobService_rejects_duplicate_slug_within_same_customer()
    {
        var fixture = TestFixture.Create(CustomerRole.Owner);
        var service = fixture.CreateJobService();

        await service.CreateAsync(fixture.CurrentUser, fixture.Customer.Id, fixture.ValidJobRequest("deploy-app"));
        var result = await service.CreateAsync(fixture.CurrentUser, fixture.Customer.Id, fixture.ValidJobRequest("deploy-app"));

        Assert.Equal(CustomerServiceError.Conflict, result.Error);
    }

    [Fact]
    public async Task JobService_allows_same_slug_in_different_customers()
    {
        var fixture = TestFixture.Create(CustomerRole.Owner);
        var other = fixture.AddOtherCustomer();
        var service = fixture.CreateJobService();

        var first = await service.CreateAsync(fixture.CurrentUser, fixture.Customer.Id, fixture.ValidJobRequest("deploy-app"));
        var second = await service.CreateAsync(other.CurrentUser, other.Customer.Id, fixture.ValidJobRequest("deploy-app", other.Resources));

        Assert.Null(first.Error);
        Assert.Null(second.Error);
        Assert.Equal(2, fixture.Db.Jobs.Count);
    }

    [Fact]
    public async Task JobService_rejects_control_node_from_different_customer()
    {
        var fixture = TestFixture.Create(CustomerRole.Owner);
        var other = fixture.AddOtherCustomer();

        var result = await fixture.CreateJobService().CreateAsync(
            fixture.CurrentUser,
            fixture.Customer.Id,
            fixture.ValidJobRequest("deploy-app") with { ControlNodeId = other.Resources.ControlNode.Id });

        Assert.Equal(CustomerServiceError.BadRequest, result.Error);
    }

    [Fact]
    public async Task JobService_rejects_inventory_group_from_different_customer()
    {
        var fixture = TestFixture.Create(CustomerRole.Owner);
        var other = fixture.AddOtherCustomer();

        var result = await fixture.CreateJobService().CreateAsync(
            fixture.CurrentUser,
            fixture.Customer.Id,
            fixture.ValidJobRequest("deploy-app") with { InventoryGroupId = other.Resources.InventoryGroup.Id });

        Assert.Equal(CustomerServiceError.BadRequest, result.Error);
    }

    [Fact]
    public async Task JobService_rejects_playbook_from_different_customer()
    {
        var fixture = TestFixture.Create(CustomerRole.Owner);
        var other = fixture.AddOtherCustomer();

        var result = await fixture.CreateJobService().CreateAsync(
            fixture.CurrentUser,
            fixture.Customer.Id,
            fixture.ValidJobRequest("deploy-app") with { PlaybookId = other.Resources.Playbook.Id });

        Assert.Equal(CustomerServiceError.BadRequest, result.Error);
    }

    [Fact]
    public async Task JobService_rejects_variable_set_from_different_customer()
    {
        var fixture = TestFixture.Create(CustomerRole.Owner);
        var other = fixture.AddOtherCustomer();

        var result = await fixture.CreateJobService().CreateAsync(
            fixture.CurrentUser,
            fixture.Customer.Id,
            fixture.ValidJobRequest("deploy-app") with { VariableSetId = other.Resources.VariableSet.Id });

        Assert.Equal(CustomerServiceError.BadRequest, result.Error);
    }

    [Fact]
    public async Task JobService_archives_instead_of_hard_deleting()
    {
        var fixture = TestFixture.Create(CustomerRole.Owner);
        var create = await fixture.CreateJobService().CreateAsync(
            fixture.CurrentUser,
            fixture.Customer.Id,
            fixture.ValidJobRequest("deploy-app"));

        var result = await fixture.CreateJobService().ArchiveAsync(
            fixture.CurrentUser,
            fixture.Customer.Id,
            create.Value!.Id);

        Assert.Null(result.Error);
        Assert.Single(fixture.Db.Jobs);
        Assert.Equal(JobStatus.Archived, fixture.Db.Jobs[0].Status);
    }

    [Fact]
    public async Task JobRunService_creates_queued_manual_job_run_when_user_has_run_jobs()
    {
        var fixture = TestFixture.Create(CustomerRole.Operator);
        var job = fixture.AddValidJob();

        var result = await fixture.CreateJobRunService().CreateManualAsync(
            fixture.CurrentUser,
            fixture.Customer.Id,
            job.Id);

        Assert.Null(result.Error);
        Assert.Single(fixture.Db.JobRuns);
        Assert.Equal(JobRunStatus.Queued, result.Value!.Status);
        Assert.Equal(JobRunTriggerType.Manual, result.Value.TriggerType);
        Assert.Equal(fixture.CurrentUser.Id, result.Value.TriggeredByUserId);
    }

    [Fact]
    public async Task JobRunService_rejects_manual_run_when_user_lacks_run_jobs()
    {
        var fixture = TestFixture.Create(CustomerRole.Viewer);
        var job = fixture.AddValidJob();

        var result = await fixture.CreateJobRunService().CreateManualAsync(
            fixture.CurrentUser,
            fixture.Customer.Id,
            job.Id);

        Assert.Equal(CustomerServiceError.Forbidden, result.Error);
        Assert.Empty(fixture.Db.JobRuns);
    }

    [Fact]
    public async Task JobRunService_rejects_manual_run_for_archived_job()
    {
        var fixture = TestFixture.Create(CustomerRole.Operator);
        var job = fixture.AddValidJob();
        job.Archive(TestTime);

        var result = await fixture.CreateJobRunService().CreateManualAsync(
            fixture.CurrentUser,
            fixture.Customer.Id,
            job.Id);

        Assert.Equal(CustomerServiceError.BadRequest, result.Error);
    }

    [Fact]
    public async Task JobRunService_rejects_manual_run_when_referenced_playbook_is_archived()
    {
        var fixture = TestFixture.Create(CustomerRole.Operator);
        var job = fixture.AddValidJob();
        fixture.Resources.Playbook.Archive(TestTime);

        var result = await fixture.CreateJobRunService().CreateManualAsync(
            fixture.CurrentUser,
            fixture.Customer.Id,
            job.Id);

        Assert.Equal(CustomerServiceError.BadRequest, result.Error);
    }

    [Fact]
    public async Task JobRunService_rejects_manual_run_when_referenced_control_node_is_archived()
    {
        var fixture = TestFixture.Create(CustomerRole.Operator);
        var job = fixture.AddValidJob();
        fixture.Resources.ControlNode.Archive(TestTime);

        var result = await fixture.CreateJobRunService().CreateManualAsync(
            fixture.CurrentUser,
            fixture.Customer.Id,
            job.Id);

        Assert.Equal(CustomerServiceError.BadRequest, result.Error);
    }

    [Fact]
    public async Task JobRunService_rejects_manual_run_when_referenced_variable_set_is_archived()
    {
        var fixture = TestFixture.Create(CustomerRole.Operator);
        var job = fixture.AddValidJob();
        fixture.Resources.VariableSet.Archive(TestTime);

        var result = await fixture.CreateJobRunService().CreateManualAsync(
            fixture.CurrentUser,
            fixture.Customer.Id,
            job.Id);

        Assert.Equal(CustomerServiceError.BadRequest, result.Error);
    }

    [Fact]
    public async Task JobRunService_cancels_queued_job_run_when_user_has_cancel_job_runs()
    {
        var fixture = TestFixture.Create(CustomerRole.Owner);
        var jobRun = fixture.AddQueuedJobRun();

        var result = await fixture.CreateJobRunService().CancelAsync(
            fixture.CurrentUser,
            fixture.Customer.Id,
            jobRun.Id,
            new CancelJobRunRequest("maintenance window closed"));

        Assert.Null(result.Error);
        Assert.Equal(JobRunStatus.Cancelled, jobRun.Status);
        Assert.Equal(TestTime, jobRun.FinishedAt);
        Assert.Equal(TestTime, jobRun.CancellationRequestedAtUtc);
        Assert.Equal(fixture.CurrentUser.Id, jobRun.CancellationRequestedByUserId);
        Assert.Equal("maintenance window closed", jobRun.CancellationReason);
    }

    [Fact]
    public async Task JobRunService_marks_running_job_run_as_cancelling()
    {
        var fixture = TestFixture.Create(CustomerRole.Owner);
        var jobRun = fixture.AddQueuedJobRun();
        jobRun.MarkRunning(TestTime);

        var result = await fixture.CreateJobRunService().CancelAsync(
            fixture.CurrentUser,
            fixture.Customer.Id,
            jobRun.Id,
            new CancelJobRunRequest("stop"));

        Assert.Null(result.Error);
        Assert.Equal(JobRunStatus.Cancelling, jobRun.Status);
        Assert.Null(jobRun.FinishedAt);
        Assert.Equal("stop", jobRun.CancellationReason);
    }

    [Fact]
    public async Task JobRunService_cancel_is_idempotent_for_already_cancelling_job_run()
    {
        var fixture = TestFixture.Create(CustomerRole.Owner);
        var jobRun = fixture.AddQueuedJobRun();
        jobRun.MarkRunning(TestTime);
        jobRun.RequestCancellation(fixture.CurrentUser.Id, "first", TestTime);

        var result = await fixture.CreateJobRunService().CancelAsync(
            fixture.CurrentUser,
            fixture.Customer.Id,
            jobRun.Id,
            new CancelJobRunRequest("second"));

        Assert.Null(result.Error);
        Assert.Equal(JobRunStatus.Cancelling, jobRun.Status);
        Assert.Equal("first", jobRun.CancellationReason);
    }

    [Theory]
    [InlineData(JobRunStatus.Succeeded)]
    [InlineData(JobRunStatus.Failed)]
    [InlineData(JobRunStatus.Cancelled)]
    [InlineData(JobRunStatus.TimedOut)]
    public async Task JobRunService_rejects_cancel_for_terminal_job_run(JobRunStatus terminalStatus)
    {
        var fixture = TestFixture.Create(CustomerRole.Owner);
        var jobRun = fixture.AddTerminalJobRun(terminalStatus);

        var result = await fixture.CreateJobRunService().CancelAsync(
            fixture.CurrentUser,
            fixture.Customer.Id,
            jobRun.Id,
            new CancelJobRunRequest(null));

        Assert.Equal(CustomerServiceError.Conflict, result.Error);
    }

    [Fact]
    public async Task JobRunService_rejects_cancel_without_permission()
    {
        var fixture = TestFixture.Create(CustomerRole.Operator);
        var jobRun = fixture.AddQueuedJobRun();

        var result = await fixture.CreateJobRunService().CancelAsync(
            fixture.CurrentUser,
            fixture.Customer.Id,
            jobRun.Id,
            new CancelJobRunRequest(null));

        Assert.Equal(CustomerServiceError.Forbidden, result.Error);
    }

    [Fact]
    public async Task JobRunService_rejects_cross_tenant_cancel()
    {
        var fixture = TestFixture.Create(CustomerRole.Owner);
        var other = fixture.AddOtherCustomer();
        var otherJob = fixture.AddValidJob(other.Customer.Id, other.Resources);
        var otherRun = JobRun.CreateManual(otherJob, other.CurrentUser.Id, TestTime);
        fixture.Db.AddJobRun(otherRun);

        var result = await fixture.CreateJobRunService().CancelAsync(
            fixture.CurrentUser,
            fixture.Customer.Id,
            otherRun.Id,
            new CancelJobRunRequest(null));

        Assert.Equal(CustomerServiceError.NotFound, result.Error);
    }

    [Theory]
    [InlineData(JobRunStatus.Failed)]
    [InlineData(JobRunStatus.TimedOut)]
    [InlineData(JobRunStatus.Cancelled)]
    public async Task JobRunService_retries_terminal_unsuccessful_job_run(JobRunStatus terminalStatus)
    {
        var fixture = TestFixture.Create(CustomerRole.Operator);
        var original = fixture.AddTerminalJobRun(terminalStatus);

        var result = await fixture.CreateJobRunService().RetryAsync(
            fixture.CurrentUser,
            fixture.Customer.Id,
            original.Id);

        Assert.Null(result.Error);
        var retry = fixture.Db.JobRuns.Single(jobRun => jobRun.Id != original.Id);
        Assert.Equal(JobRunStatus.Queued, retry.Status);
        Assert.Equal(JobRunTriggerType.Retry, retry.TriggerType);
        Assert.Equal(fixture.CurrentUser.Id, retry.TriggeredByUserId);
        Assert.Equal(original.Id, retry.RetriedFromJobRunId);
        Assert.Equal(original.RetryAttempt + 1, retry.RetryAttempt);
        Assert.Equal(terminalStatus, original.Status);
    }

    [Theory]
    [InlineData(JobRunStatus.Queued)]
    [InlineData(JobRunStatus.Running)]
    [InlineData(JobRunStatus.Cancelling)]
    [InlineData(JobRunStatus.Succeeded)]
    public async Task JobRunService_rejects_retry_for_unsupported_status(JobRunStatus status)
    {
        var fixture = TestFixture.Create(CustomerRole.Operator);
        var jobRun = fixture.AddJobRunWithStatus(status);

        var result = await fixture.CreateJobRunService().RetryAsync(
            fixture.CurrentUser,
            fixture.Customer.Id,
            jobRun.Id);

        Assert.Equal(CustomerServiceError.Conflict, result.Error);
    }

    [Fact]
    public async Task JobRunService_rejects_retry_without_permission()
    {
        var fixture = TestFixture.Create(CustomerRole.Viewer);
        var jobRun = fixture.AddTerminalJobRun(JobRunStatus.Failed);

        var result = await fixture.CreateJobRunService().RetryAsync(
            fixture.CurrentUser,
            fixture.Customer.Id,
            jobRun.Id);

        Assert.Equal(CustomerServiceError.Forbidden, result.Error);
    }

    [Fact]
    public async Task JobRunService_rejects_retry_when_referenced_job_is_archived()
    {
        var fixture = TestFixture.Create(CustomerRole.Operator);
        var jobRun = fixture.AddTerminalJobRun(JobRunStatus.Failed);
        fixture.Db.Jobs.Single(job => job.Id == jobRun.JobId).Archive(TestTime);

        var result = await fixture.CreateJobRunService().RetryAsync(
            fixture.CurrentUser,
            fixture.Customer.Id,
            jobRun.Id);

        Assert.Equal(CustomerServiceError.BadRequest, result.Error);
    }

    [Fact]
    public async Task JobRunService_rejects_retry_when_referenced_playbook_is_archived()
    {
        var fixture = TestFixture.Create(CustomerRole.Operator);
        var jobRun = fixture.AddTerminalJobRun(JobRunStatus.Failed);
        fixture.Resources.Playbook.Archive(TestTime);

        var result = await fixture.CreateJobRunService().RetryAsync(
            fixture.CurrentUser,
            fixture.Customer.Id,
            jobRun.Id);

        Assert.Equal(CustomerServiceError.BadRequest, result.Error);
    }

    [Fact]
    public async Task JobRunService_rejects_retry_when_referenced_control_node_is_archived()
    {
        var fixture = TestFixture.Create(CustomerRole.Operator);
        var jobRun = fixture.AddTerminalJobRun(JobRunStatus.Failed);
        fixture.Resources.ControlNode.Archive(TestTime);

        var result = await fixture.CreateJobRunService().RetryAsync(
            fixture.CurrentUser,
            fixture.Customer.Id,
            jobRun.Id);

        Assert.Equal(CustomerServiceError.BadRequest, result.Error);
    }

    [Fact]
    public async Task JobRunService_rejects_retry_when_referenced_variable_set_is_archived()
    {
        var fixture = TestFixture.Create(CustomerRole.Operator);
        var jobRun = fixture.AddTerminalJobRun(JobRunStatus.Failed);
        fixture.Resources.VariableSet.Archive(TestTime);

        var result = await fixture.CreateJobRunService().RetryAsync(
            fixture.CurrentUser,
            fixture.Customer.Id,
            jobRun.Id);

        Assert.Equal(CustomerServiceError.BadRequest, result.Error);
    }

    [Fact]
    public async Task JobRunService_rejects_cross_tenant_retry()
    {
        var fixture = TestFixture.Create(CustomerRole.Owner);
        var other = fixture.AddOtherCustomer();
        var otherJob = fixture.AddValidJob(other.Customer.Id, other.Resources);
        var otherRun = JobRun.CreateManual(otherJob, other.CurrentUser.Id, TestTime);
        otherRun.MarkRunning(TestTime);
        otherRun.MarkFailed(1, "failed", TestTime);
        fixture.Db.AddJobRun(otherRun);

        var result = await fixture.CreateJobRunService().RetryAsync(
            fixture.CurrentUser,
            fixture.Customer.Id,
            otherRun.Id);

        Assert.Equal(CustomerServiceError.NotFound, result.Error);
    }

    private static DateTimeOffset TestTime => new(2026, 4, 27, 10, 0, 0, TimeSpan.Zero);

    private static CurrentUserDto CurrentUser(User user)
    {
        return new CurrentUserDto(user.Id, user.DisplayName, user.Email, true, false, "fake", user.Email);
    }

    private sealed record CustomerUser(Customer Customer, CurrentUserDto CurrentUser, DefinitionResources Resources);

    private sealed record DefinitionResources(
        ControlNode ControlNode,
        InventoryGroup InventoryGroup,
        Playbook Playbook,
        VariableSet VariableSet);

    private sealed class TestFixture
    {
        private readonly TestClock clock = new();

        private TestFixture(NodeControlTestDbContext db, Customer customer, CurrentUserDto currentUser, DefinitionResources resources)
        {
            Db = db;
            Customer = customer;
            CurrentUser = currentUser;
            Resources = resources;
        }

        public NodeControlTestDbContext Db { get; }

        public Customer Customer { get; }

        public CurrentUserDto CurrentUser { get; }

        public DefinitionResources Resources { get; }

        public static TestFixture Create(CustomerRole role)
        {
            var db = new NodeControlTestDbContext();
            var user = User.Create("Test User", "test@nodecontrol.local", false, TestTime);
            var customer = Customer.Create("Customer A", "customer-a", null, TestTime);
            db.AddUser(user);
            db.AddCustomer(customer);
            db.AddCustomerMembership(CustomerMembership.Create(customer, user, role, TestTime));
            var resources = AddResources(db, customer.Id, "a");

            return new TestFixture(db, customer, CurrentUser(user), resources);
        }

        public CustomerUser AddOtherCustomer()
        {
            var user = User.Create("Other User", "other@nodecontrol.local", false, TestTime);
            var customer = Customer.Create("Customer B", "customer-b", null, TestTime);
            Db.AddUser(user);
            Db.AddCustomer(customer);
            Db.AddCustomerMembership(CustomerMembership.Create(customer, user, CustomerRole.Owner, TestTime));
            var resources = AddResources(Db, customer.Id, "b");
            return new CustomerUser(customer, CurrentUser(user), resources);
        }

        public CreateJobRequest ValidJobRequest(string slug, DefinitionResources? resources = null)
        {
            var selected = resources ?? Resources;
            return new CreateJobRequest(
                "Deploy App",
                slug,
                null,
                selected.ControlNode.Id,
                selected.InventoryGroup.Id,
                selected.Playbook.Id,
                selected.VariableSet.Id);
        }

        public Job AddValidJob()
        {
            return AddValidJob(Customer.Id, Resources);
        }

        public Job AddValidJob(Guid customerId, DefinitionResources resources)
        {
            var job = Job.Create(
                customerId,
                "Deploy App",
                $"deploy-app-{Db.Jobs.Count + 1}",
                null,
                resources.ControlNode.Id,
                resources.InventoryGroup.Id,
                resources.Playbook.Id,
                resources.VariableSet.Id,
                1800,
                TestTime);
            Db.AddJob(job);
            return job;
        }

        public JobRun AddQueuedJobRun()
        {
            var job = AddValidJob();
            var jobRun = JobRun.CreateManual(job, CurrentUser.Id, TestTime);
            Db.AddJobRun(jobRun);
            return jobRun;
        }

        public JobRun AddTerminalJobRun(JobRunStatus status)
        {
            return AddJobRunWithStatus(status);
        }

        public JobRun AddJobRunWithStatus(JobRunStatus status)
        {
            var jobRun = AddQueuedJobRun();
            switch (status)
            {
                case JobRunStatus.Queued:
                    break;
                case JobRunStatus.Running:
                    jobRun.MarkRunning(TestTime);
                    break;
                case JobRunStatus.Cancelling:
                    jobRun.MarkRunning(TestTime);
                    jobRun.RequestCancellation(CurrentUser.Id, "cancel", TestTime);
                    break;
                case JobRunStatus.Succeeded:
                    jobRun.MarkRunning(TestTime);
                    jobRun.MarkSucceeded(0, TestTime);
                    break;
                case JobRunStatus.Failed:
                    jobRun.MarkRunning(TestTime);
                    jobRun.MarkFailed(1, "failed", TestTime);
                    break;
                case JobRunStatus.Cancelled:
                    jobRun.MarkRunning(TestTime);
                    jobRun.MarkCancelled(130, "cancelled", TestTime);
                    break;
                case JobRunStatus.TimedOut:
                    jobRun.MarkRunning(TestTime);
                    jobRun.MarkTimedOut(null, "timeout", TestTime);
                    break;
                default:
                    throw new ArgumentOutOfRangeException(nameof(status));
            }

            return jobRun;
        }

        public JobService CreateJobService()
        {
            return new JobService(Db, new CustomerAuthorizationService(Db), clock);
        }

        public JobRunService CreateJobRunService()
        {
            return new JobRunService(Db, new CustomerAuthorizationService(Db), clock);
        }

        private static DefinitionResources AddResources(NodeControlTestDbContext db, Guid customerId, string suffix)
        {
            var controlNode = ControlNode.Create(customerId, $"control-{suffix}", $"control-{suffix}.local", 22, null, TestTime);
            var inventoryGroup = InventoryGroup.Create(customerId, $"web-{suffix}", null, TestTime);
            var playbook = Playbook.Create(
                customerId,
                $"Deploy {suffix}",
                $"deploy-{suffix}",
                null,
                PlaybookSourceType.InlineYaml,
                "- hosts: all\n  tasks:\n    - debug:\n        msg: hello\n",
                null,
                TestTime);
            var variableSet = VariableSet.Create(
                customerId,
                $"Defaults {suffix}",
                $"defaults-{suffix}",
                null,
                VariableSetFormat.Yaml,
                "package_name: nginx\n",
                false,
                TestTime);

            db.AddControlNode(controlNode);
            db.AddInventoryGroup(inventoryGroup);
            db.AddPlaybook(playbook);
            db.AddVariableSet(variableSet);

            return new DefinitionResources(controlNode, inventoryGroup, playbook, variableSet);
        }
    }

    private sealed class TestClock : IClock
    {
        public DateTimeOffset UtcNow { get; set; } = TestTime;
    }
}
