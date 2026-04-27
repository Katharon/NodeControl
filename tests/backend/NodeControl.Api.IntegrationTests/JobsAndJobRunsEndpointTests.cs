using System.Net;
using System.Net.Http.Json;
using System.Text.Json;
using System.Text.Json.Serialization;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using NodeControl.Application.Audit;
using NodeControl.Application.Abstractions.Persistence;
using NodeControl.Domain.Audit;
using NodeControl.Application.JobRuns;
using NodeControl.Application.Jobs;
using NodeControl.Application.Schedules;
using NodeControl.Domain.Customers;
using NodeControl.Domain.Inventories;
using NodeControl.Domain.Jobs;
using NodeControl.Domain.Nodes;
using NodeControl.Domain.Playbooks;
using NodeControl.Domain.Users;
using NodeControl.Domain.VariableSets;

namespace NodeControl.Api.IntegrationTests;

public sealed class JobsAndJobRunsEndpointTests
{
    [Fact]
    public async Task Get_jobs_requires_view_playbooks()
    {
        await using var factory = DefinitionApiFactory.Create(CustomerRole.Auditor);
        var seeded = factory.SeedCurrentUserAndCustomer();
        using var client = factory.CreateClient();

        using var response = await client.GetAsync($"/api/v1/customers/{seeded.Customer.Id}/jobs");

        Assert.Equal(HttpStatusCode.Forbidden, response.StatusCode);
    }

    [Fact]
    public async Task Post_jobs_requires_manage_playbooks()
    {
        await using var factory = DefinitionApiFactory.Create(CustomerRole.Viewer);
        var seeded = factory.SeedCurrentUserAndCustomer();
        var resources = factory.Db.AddDefinitionResources(seeded.Customer.Id, "a");
        using var client = factory.CreateClient();

        using var response = await client.PostAsJsonAsync(
            $"/api/v1/customers/{seeded.Customer.Id}/jobs",
            ValidJobRequest("deploy-app", resources),
            JsonOptions);

        Assert.Equal(HttpStatusCode.Forbidden, response.StatusCode);
    }

    [Fact]
    public async Task Post_run_requires_run_jobs()
    {
        await using var factory = DefinitionApiFactory.Create(CustomerRole.Viewer);
        var seeded = factory.SeedCurrentUserAndCustomer();
        var resources = factory.Db.AddDefinitionResources(seeded.Customer.Id, "a");
        var job = factory.Db.AddJob(Job.Create(
            seeded.Customer.Id,
            "Deploy App",
            "deploy-app",
            null,
            resources.ControlNode.Id,
            resources.InventoryGroup.Id,
            resources.Playbook.Id,
            resources.VariableSet.Id,
            1800,
            TestTime));
        using var client = factory.CreateClient();

        using var response = await client.PostAsync(
            $"/api/v1/customers/{seeded.Customer.Id}/jobs/{job.Id}/run",
            JsonContent.Create(new { }));

        Assert.Equal(HttpStatusCode.Forbidden, response.StatusCode);
    }

    [Fact]
    public async Task Get_schedules_requires_view_schedules()
    {
        await using var factory = DefinitionApiFactory.Create(CustomerRole.Auditor);
        var seeded = factory.SeedCurrentUserAndCustomer();
        using var client = factory.CreateClient();

        using var response = await client.GetAsync($"/api/v1/customers/{seeded.Customer.Id}/schedules");

        Assert.Equal(HttpStatusCode.Forbidden, response.StatusCode);
    }

    [Fact]
    public async Task Post_schedules_requires_manage_schedules()
    {
        await using var factory = DefinitionApiFactory.Create(CustomerRole.Viewer);
        var seeded = factory.SeedCurrentUserAndCustomer();
        var resources = factory.Db.AddDefinitionResources(seeded.Customer.Id, "a");
        var job = factory.Db.AddJob(Job.Create(
            seeded.Customer.Id,
            "Deploy App",
            "deploy-app",
            null,
            resources.ControlNode.Id,
            resources.InventoryGroup.Id,
            resources.Playbook.Id,
            resources.VariableSet.Id,
            1800,
            TestTime));
        using var client = factory.CreateClient();

        using var response = await client.PostAsJsonAsync(
            $"/api/v1/customers/{seeded.Customer.Id}/schedules",
            new CreateJobScheduleRequest("Nightly", "nightly", null, job.Id, "0 * * * *", "UTC"),
            JsonOptions);

        Assert.Equal(HttpStatusCode.Forbidden, response.StatusCode);
    }

    [Fact]
    public async Task Post_schedules_creates_schedule_for_authorized_member()
    {
        await using var factory = DefinitionApiFactory.Create(CustomerRole.Owner);
        var seeded = factory.SeedCurrentUserAndCustomer();
        var resources = factory.Db.AddDefinitionResources(seeded.Customer.Id, "a");
        var job = factory.Db.AddJob(Job.Create(
            seeded.Customer.Id,
            "Deploy App",
            "deploy-app",
            null,
            resources.ControlNode.Id,
            resources.InventoryGroup.Id,
            resources.Playbook.Id,
            resources.VariableSet.Id,
            1800,
            TestTime));
        using var client = factory.CreateClient();

        using var response = await client.PostAsJsonAsync(
            $"/api/v1/customers/{seeded.Customer.Id}/schedules",
            new CreateJobScheduleRequest("Nightly", "nightly", null, job.Id, "0 * * * *", "UTC"),
            JsonOptions);
        var schedule = await response.Content.ReadFromJsonAsync<JobScheduleDto>(JsonOptions);

        Assert.Equal(HttpStatusCode.Created, response.StatusCode);
        Assert.Equal("Active", schedule!.Status);
        Assert.Single(factory.Db.JobSchedules);
    }

    [Fact]
    public async Task Schedule_mutations_require_manage_schedules()
    {
        await using var factory = DefinitionApiFactory.Create(CustomerRole.Viewer);
        var seeded = factory.SeedCurrentUserAndCustomer();
        var schedule = factory.Db.AddScheduleForCustomer(seeded.Customer.Id);
        using var client = factory.CreateClient();

        var updateRequest = new UpdateJobScheduleRequest("Nightly", "nightly-new", null, schedule.JobId, "0 * * * *", "UTC");

        Assert.Equal(HttpStatusCode.Forbidden, (await client.PutAsJsonAsync($"/api/v1/customers/{seeded.Customer.Id}/schedules/{schedule.Id}", updateRequest, JsonOptions)).StatusCode);
        Assert.Equal(HttpStatusCode.Forbidden, (await client.PostAsync($"/api/v1/customers/{seeded.Customer.Id}/schedules/{schedule.Id}/pause", JsonContent.Create(new { }))).StatusCode);
        Assert.Equal(HttpStatusCode.Forbidden, (await client.PostAsync($"/api/v1/customers/{seeded.Customer.Id}/schedules/{schedule.Id}/resume", JsonContent.Create(new { }))).StatusCode);
        Assert.Equal(HttpStatusCode.Forbidden, (await client.DeleteAsync($"/api/v1/customers/{seeded.Customer.Id}/schedules/{schedule.Id}")).StatusCode);
    }

    [Fact]
    public async Task Schedule_endpoints_reject_cross_tenant_access()
    {
        await using var factory = DefinitionApiFactory.Create(CustomerRole.Owner);
        var seeded = factory.SeedCurrentUserAndCustomer();
        var otherCustomer = Customer.Create("Other Customer", "other-customer", null, TestTime);
        factory.Db.AddCustomer(otherCustomer);
        var otherSchedule = factory.Db.AddScheduleForCustomer(otherCustomer.Id, "other");
        using var client = factory.CreateClient();

        using var getResponse = await client.GetAsync($"/api/v1/customers/{seeded.Customer.Id}/schedules/{otherSchedule.Id}");
        using var pauseResponse = await client.PostAsync($"/api/v1/customers/{seeded.Customer.Id}/schedules/{otherSchedule.Id}/pause", JsonContent.Create(new { }));

        Assert.Equal(HttpStatusCode.NotFound, getResponse.StatusCode);
        Assert.Equal(HttpStatusCode.NotFound, pauseResponse.StatusCode);
    }

    [Fact]
    public async Task Post_run_creates_queued_manual_job_run_with_triggered_user()
    {
        await using var factory = DefinitionApiFactory.Create(CustomerRole.Operator);
        var seeded = factory.SeedCurrentUserAndCustomer();
        var resources = factory.Db.AddDefinitionResources(seeded.Customer.Id, "a");
        var job = factory.Db.AddJob(Job.Create(
            seeded.Customer.Id,
            "Deploy App",
            "deploy-app",
            null,
            resources.ControlNode.Id,
            resources.InventoryGroup.Id,
            resources.Playbook.Id,
            resources.VariableSet.Id,
            1800,
            TestTime));
        using var client = factory.CreateClient();

        using var response = await client.PostAsync(
            $"/api/v1/customers/{seeded.Customer.Id}/jobs/{job.Id}/run",
            JsonContent.Create(new { }));
        var jobRun = await response.Content.ReadFromJsonAsync<JobRunDto>(JsonOptions);

        Assert.Equal(HttpStatusCode.Created, response.StatusCode);
        Assert.Equal(JobRunStatus.Queued, jobRun!.Status);
        Assert.Equal(JobRunTriggerType.Manual, jobRun.TriggerType);
        Assert.Equal(seeded.User.Id, jobRun.TriggeredByUserId);
        Assert.Single(factory.Db.JobRuns);
    }

    [Fact]
    public async Task Get_job_run_by_id_rejects_cross_tenant_access()
    {
        await using var factory = DefinitionApiFactory.Create(CustomerRole.Owner);
        var seeded = factory.SeedCurrentUserAndCustomer();
        var otherCustomer = Customer.Create("Other Customer", "other-customer", null, TestTime);
        var otherResources = factory.Db.AddDefinitionResources(otherCustomer.Id, "b");
        var otherJob = Job.Create(
            otherCustomer.Id,
            "Deploy App",
            "deploy-app",
            null,
            otherResources.ControlNode.Id,
            otherResources.InventoryGroup.Id,
            otherResources.Playbook.Id,
            otherResources.VariableSet.Id,
            1800,
            TestTime);
        var otherRun = JobRun.CreateManual(otherJob, seeded.User.Id, TestTime);
        factory.Db.AddCustomer(otherCustomer);
        factory.Db.AddJob(otherJob);
        factory.Db.AddJobRun(otherRun);
        using var client = factory.CreateClient();

        using var response = await client.GetAsync(
            $"/api/v1/customers/{seeded.Customer.Id}/job-runs/{otherRun.Id}");

        Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
    }

    [Fact]
    public async Task Post_cancel_job_run_requires_cancel_job_runs()
    {
        await using var factory = DefinitionApiFactory.Create(CustomerRole.Operator);
        var seeded = factory.SeedCurrentUserAndCustomer();
        var jobRun = factory.Db.AddJobRunForCustomer(seeded.Customer.Id, seeded.User.Id);
        using var client = factory.CreateClient();

        using var response = await client.PostAsJsonAsync(
            $"/api/v1/customers/{seeded.Customer.Id}/job-runs/{jobRun.Id}/cancel",
            new CancelJobRunRequest("stop"),
            JsonOptions);

        Assert.Equal(HttpStatusCode.Forbidden, response.StatusCode);
    }

    [Fact]
    public async Task Post_cancel_job_run_returns_updated_job_run()
    {
        await using var factory = DefinitionApiFactory.Create(CustomerRole.Owner);
        var seeded = factory.SeedCurrentUserAndCustomer();
        var jobRun = factory.Db.AddJobRunForCustomer(seeded.Customer.Id, seeded.User.Id);
        using var client = factory.CreateClient();

        using var response = await client.PostAsJsonAsync(
            $"/api/v1/customers/{seeded.Customer.Id}/job-runs/{jobRun.Id}/cancel",
            new CancelJobRunRequest("stop"),
            JsonOptions);
        var dto = await response.Content.ReadFromJsonAsync<JobRunDto>(JsonOptions);

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        Assert.Equal(JobRunStatus.Cancelled, dto!.Status);
        Assert.Equal("stop", dto.CancellationReason);
    }

    [Fact]
    public async Task Post_retry_job_run_requires_retry_job_runs()
    {
        await using var factory = DefinitionApiFactory.Create(CustomerRole.Viewer);
        var seeded = factory.SeedCurrentUserAndCustomer();
        var jobRun = factory.Db.AddTerminalJobRunForCustomer(seeded.Customer.Id, seeded.User.Id, JobRunStatus.Failed);
        using var client = factory.CreateClient();

        using var response = await client.PostAsync(
            $"/api/v1/customers/{seeded.Customer.Id}/job-runs/{jobRun.Id}/retry",
            JsonContent.Create(new { }));

        Assert.Equal(HttpStatusCode.Forbidden, response.StatusCode);
    }

    [Fact]
    public async Task Post_retry_job_run_returns_new_queued_retry_job_run()
    {
        await using var factory = DefinitionApiFactory.Create(CustomerRole.Operator);
        var seeded = factory.SeedCurrentUserAndCustomer();
        var original = factory.Db.AddTerminalJobRunForCustomer(seeded.Customer.Id, seeded.User.Id, JobRunStatus.Failed);
        using var client = factory.CreateClient();

        using var response = await client.PostAsync(
            $"/api/v1/customers/{seeded.Customer.Id}/job-runs/{original.Id}/retry",
            JsonContent.Create(new { }));
        var dto = await response.Content.ReadFromJsonAsync<JobRunDto>(JsonOptions);

        Assert.Equal(HttpStatusCode.Created, response.StatusCode);
        Assert.Equal(JobRunStatus.Queued, dto!.Status);
        Assert.Equal(JobRunTriggerType.Retry, dto.TriggerType);
        Assert.Equal(original.Id, dto.RetriedFromJobRunId);
        Assert.Equal(1, dto.RetryAttempt);
    }

    [Fact]
    public async Task Job_run_operational_endpoints_reject_cross_tenant_access()
    {
        await using var factory = DefinitionApiFactory.Create(CustomerRole.Owner);
        var seeded = factory.SeedCurrentUserAndCustomer();
        var otherCustomer = Customer.Create("Other Customer", "other-customer", null, TestTime);
        factory.Db.AddCustomer(otherCustomer);
        var otherRun = factory.Db.AddTerminalJobRunForCustomer(otherCustomer.Id, seeded.User.Id, JobRunStatus.Failed, "other");
        using var client = factory.CreateClient();

        using var cancelResponse = await client.PostAsJsonAsync(
            $"/api/v1/customers/{seeded.Customer.Id}/job-runs/{otherRun.Id}/cancel",
            new CancelJobRunRequest(null),
            JsonOptions);
        using var retryResponse = await client.PostAsync(
            $"/api/v1/customers/{seeded.Customer.Id}/job-runs/{otherRun.Id}/retry",
            JsonContent.Create(new { }));

        Assert.Equal(HttpStatusCode.NotFound, cancelResponse.StatusCode);
        Assert.Equal(HttpStatusCode.NotFound, retryResponse.StatusCode);
    }

    [Fact]
    public async Task Get_job_runs_requires_customer_membership()
    {
        await using var factory = DefinitionApiFactory.CreateWithoutMembership();
        var seeded = factory.SeedCurrentUserAndCustomer();
        using var client = factory.CreateClient();

        using var response = await client.GetAsync($"/api/v1/customers/{seeded.Customer.Id}/job-runs");

        Assert.Equal(HttpStatusCode.Forbidden, response.StatusCode);
    }

    [Fact]
    public async Task Get_job_run_logs_allows_authorized_customer_member()
    {
        await using var factory = DefinitionApiFactory.Create(CustomerRole.Viewer);
        var seeded = factory.SeedCurrentUserAndCustomer();
        var jobRun = factory.Db.AddJobRunForCustomer(seeded.Customer.Id, seeded.User.Id);
        factory.Db.AddJobRunLogEntry(JobRunLogEntry.Create(
            jobRun,
            1,
            TestTime,
            JobRunLogStream.System,
            JobRunLogLevel.Info,
            "JobRun processing started."));
        using var client = factory.CreateClient();

        using var response = await client.GetAsync(
            $"/api/v1/customers/{seeded.Customer.Id}/job-runs/{jobRun.Id}/logs");
        var logs = await response.Content.ReadFromJsonAsync<JobRunLogsResponse>(JsonOptions);

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        Assert.Single(logs!.Items);
        Assert.Equal("System", logs.Items[0].Stream);
        Assert.Equal("Info", logs.Items[0].Level);
    }

    [Fact]
    public async Task Get_job_run_logs_rejects_cross_tenant_access()
    {
        await using var factory = DefinitionApiFactory.Create(CustomerRole.Owner);
        var seeded = factory.SeedCurrentUserAndCustomer();
        var otherCustomer = Customer.Create("Other Customer", "other-customer", null, TestTime);
        factory.Db.AddCustomer(otherCustomer);
        var otherRun = factory.Db.AddJobRunForCustomer(otherCustomer.Id, seeded.User.Id, suffix: "other");
        factory.Db.AddJobRunLogEntry(JobRunLogEntry.Create(
            otherRun,
            1,
            TestTime,
            JobRunLogStream.System,
            JobRunLogLevel.Info,
            "other customer log"));
        using var client = factory.CreateClient();

        using var response = await client.GetAsync(
            $"/api/v1/customers/{seeded.Customer.Id}/job-runs/{otherRun.Id}/logs");

        Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
    }

    [Fact]
    public async Task Get_job_run_logs_returns_logs_ordered_by_sequence()
    {
        await using var factory = DefinitionApiFactory.Create(CustomerRole.Viewer);
        var seeded = factory.SeedCurrentUserAndCustomer();
        var jobRun = factory.Db.AddJobRunForCustomer(seeded.Customer.Id, seeded.User.Id);
        factory.Db.AddJobRunLogEntry(JobRunLogEntry.Create(jobRun, 3, TestTime, JobRunLogStream.StdOut, JobRunLogLevel.Info, "third"));
        factory.Db.AddJobRunLogEntry(JobRunLogEntry.Create(jobRun, 1, TestTime, JobRunLogStream.System, JobRunLogLevel.Info, "first"));
        factory.Db.AddJobRunLogEntry(JobRunLogEntry.Create(jobRun, 2, TestTime, JobRunLogStream.StdErr, JobRunLogLevel.Error, "second"));
        using var client = factory.CreateClient();

        using var response = await client.GetAsync(
            $"/api/v1/customers/{seeded.Customer.Id}/job-runs/{jobRun.Id}/logs");
        var logs = await response.Content.ReadFromJsonAsync<JobRunLogsResponse>(JsonOptions);

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        Assert.Equal([1, 2, 3], logs!.Items.Select(item => item.Sequence).ToArray());
    }

    [Fact]
    public async Task Get_job_run_logs_filters_after_sequence()
    {
        await using var factory = DefinitionApiFactory.Create(CustomerRole.Viewer);
        var seeded = factory.SeedCurrentUserAndCustomer();
        var jobRun = factory.Db.AddJobRunForCustomer(seeded.Customer.Id, seeded.User.Id);
        factory.Db.AddJobRunLogEntry(JobRunLogEntry.Create(jobRun, 1, TestTime, JobRunLogStream.System, JobRunLogLevel.Info, "first"));
        factory.Db.AddJobRunLogEntry(JobRunLogEntry.Create(jobRun, 2, TestTime, JobRunLogStream.StdOut, JobRunLogLevel.Info, "second"));
        factory.Db.AddJobRunLogEntry(JobRunLogEntry.Create(jobRun, 3, TestTime, JobRunLogStream.StdErr, JobRunLogLevel.Error, "third"));
        using var client = factory.CreateClient();

        using var response = await client.GetAsync(
            $"/api/v1/customers/{seeded.Customer.Id}/job-runs/{jobRun.Id}/logs?afterSequence=1");
        var logs = await response.Content.ReadFromJsonAsync<JobRunLogsResponse>(JsonOptions);

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        Assert.Equal([2, 3], logs!.Items.Select(item => item.Sequence).ToArray());
    }

    [Fact]
    public async Task Get_job_run_logs_respects_limit()
    {
        await using var factory = DefinitionApiFactory.Create(CustomerRole.Viewer);
        var seeded = factory.SeedCurrentUserAndCustomer();
        var jobRun = factory.Db.AddJobRunForCustomer(seeded.Customer.Id, seeded.User.Id);
        factory.Db.AddJobRunLogEntry(JobRunLogEntry.Create(jobRun, 1, TestTime, JobRunLogStream.System, JobRunLogLevel.Info, "first"));
        factory.Db.AddJobRunLogEntry(JobRunLogEntry.Create(jobRun, 2, TestTime, JobRunLogStream.StdOut, JobRunLogLevel.Info, "second"));
        factory.Db.AddJobRunLogEntry(JobRunLogEntry.Create(jobRun, 3, TestTime, JobRunLogStream.StdErr, JobRunLogLevel.Error, "third"));
        using var client = factory.CreateClient();

        using var response = await client.GetAsync(
            $"/api/v1/customers/{seeded.Customer.Id}/job-runs/{jobRun.Id}/logs?limit=2");
        var logs = await response.Content.ReadFromJsonAsync<JobRunLogsResponse>(JsonOptions);

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        Assert.Equal([1, 2], logs!.Items.Select(item => item.Sequence).ToArray());
    }

    [Fact]
    public async Task Get_job_run_logs_does_not_trigger_worker_execution()
    {
        await using var factory = DefinitionApiFactory.Create(CustomerRole.Viewer);
        var seeded = factory.SeedCurrentUserAndCustomer();
        var jobRun = factory.Db.AddJobRunForCustomer(seeded.Customer.Id, seeded.User.Id);
        factory.Db.AddJobRunLogEntry(JobRunLogEntry.Create(jobRun, 1, TestTime, JobRunLogStream.System, JobRunLogLevel.Info, "queued"));
        using var client = factory.CreateClient();

        using var response = await client.GetAsync(
            $"/api/v1/customers/{seeded.Customer.Id}/job-runs/{jobRun.Id}/logs");

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        Assert.Equal(JobRunStatus.Queued, jobRun.Status);
        Assert.Null(jobRun.StartedAt);
    }

    [Fact]
    public async Task Get_audit_logs_requires_view_audit_logs()
    {
        await using var factory = DefinitionApiFactory.Create(CustomerRole.Viewer);
        var seeded = factory.SeedCurrentUserAndCustomer();
        using var client = factory.CreateClient();

        using var response = await client.GetAsync($"/api/v1/customers/{seeded.Customer.Id}/audit-logs");

        Assert.Equal(HttpStatusCode.Forbidden, response.StatusCode);
    }

    [Fact]
    public async Task Get_audit_logs_allows_auditor_and_filters_by_action()
    {
        await using var factory = DefinitionApiFactory.Create(CustomerRole.Auditor);
        var seeded = factory.SeedCurrentUserAndCustomer();
        var matching = factory.Db.AddAuditLogEntryForCustomer(seeded.Customer.Id, "job.created", "Job", TestTime);
        factory.Db.AddAuditLogEntryForCustomer(seeded.Customer.Id, "schedule.created", "Schedule", TestTime.AddMinutes(-1));
        using var client = factory.CreateClient();

        using var response = await client.GetAsync(
            $"/api/v1/customers/{seeded.Customer.Id}/audit-logs?action=job.created");
        var payload = await response.Content.ReadFromJsonAsync<AuditLogListResponse>(JsonOptions);

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        var item = Assert.Single(payload!.Items);
        Assert.Equal(matching.Id, item.Id);
    }

    [Fact]
    public async Task Audit_log_endpoints_reject_cross_tenant_access()
    {
        await using var factory = DefinitionApiFactory.Create(CustomerRole.Auditor);
        var seeded = factory.SeedCurrentUserAndCustomer();
        var otherCustomer = Customer.Create("Other Customer", "other-customer", null, TestTime);
        factory.Db.AddCustomer(otherCustomer);
        var own = factory.Db.AddAuditLogEntryForCustomer(seeded.Customer.Id, "job.created", "Job", TestTime);
        var other = factory.Db.AddAuditLogEntryForCustomer(otherCustomer.Id, "job.created", "Job", TestTime);
        using var client = factory.CreateClient();

        using var listResponse = await client.GetAsync($"/api/v1/customers/{seeded.Customer.Id}/audit-logs");
        var payload = await listResponse.Content.ReadFromJsonAsync<AuditLogListResponse>(JsonOptions);
        using var detailResponse = await client.GetAsync(
            $"/api/v1/customers/{seeded.Customer.Id}/audit-logs/{other.Id}");

        Assert.Equal(HttpStatusCode.OK, listResponse.StatusCode);
        var item = Assert.Single(payload!.Items);
        Assert.Equal(own.Id, item.Id);
        Assert.Equal(HttpStatusCode.NotFound, detailResponse.StatusCode);
    }

    private static CreateJobRequest ValidJobRequest(string slug, DefinitionResources resources)
    {
        return new CreateJobRequest(
            "Deploy App",
            slug,
            null,
            resources.ControlNode.Id,
            resources.InventoryGroup.Id,
            resources.Playbook.Id,
            resources.VariableSet.Id);
    }

    private static readonly JsonSerializerOptions JsonOptions = new(JsonSerializerDefaults.Web)
    {
        Converters = { new JsonStringEnumConverter(allowIntegerValues: false) }
    };

    private static DateTimeOffset TestTime => new(2026, 4, 27, 10, 0, 0, TimeSpan.Zero);

    private sealed record Seeded(User User, Customer Customer);

    private sealed record DefinitionResources(
        ControlNode ControlNode,
        InventoryGroup InventoryGroup,
        Playbook Playbook,
        VariableSet VariableSet);

    private sealed class DefinitionApiFactory(
        CustomerRole role,
        bool addMembership = true) : WebApplicationFactory<Program>
    {
        private const string Provider = "fake";
        private const string Subject = "job-user";
        private const string Email = "job-user@nodecontrol.local";
        private const string DisplayName = "Job User";

        public DefinitionApiDbContext Db { get; } = new();

        public static DefinitionApiFactory Create(CustomerRole role)
        {
            return new DefinitionApiFactory(role);
        }

        public static DefinitionApiFactory CreateWithoutMembership()
        {
            return new DefinitionApiFactory(CustomerRole.Viewer, addMembership: false);
        }

        public Seeded SeedCurrentUserAndCustomer()
        {
            var user = User.Create(DisplayName, Email, false, TestTime);
            var identity = ExternalIdentity.Create(user, Provider, Subject, Email, DisplayName, TestTime);
            var customer = Customer.Create("Customer A", "customer-a", null, TestTime);
            Db.AddUser(user);
            Db.AddExternalIdentity(identity);
            Db.AddCustomer(customer);
            if (addMembership)
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
                    ["Auth:Fake:IsPlatformAdmin"] = "False"
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

        public List<ControlNode> ControlNodes { get; } = [];

        public List<InventoryGroup> InventoryGroups { get; } = [];

        public List<Playbook> Playbooks { get; } = [];

        public List<VariableSet> VariableSets { get; } = [];

        public List<Job> Jobs { get; } = [];

        public List<JobSchedule> JobSchedules { get; } = [];

        public List<JobRun> JobRuns { get; } = [];

        public List<JobRunLogEntry> JobRunLogEntries { get; } = [];

        public List<AuditLogEntry> AuditLogEntries { get; } = [];

        public DefinitionResources AddDefinitionResources(Guid customerId, string suffix)
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

            AddControlNode(controlNode);
            AddInventoryGroup(inventoryGroup);
            AddPlaybook(playbook);
            AddVariableSet(variableSet);

            return new DefinitionResources(controlNode, inventoryGroup, playbook, variableSet);
        }

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

        public Task<ControlNode?> FindControlNodeAsync(Guid customerId, Guid controlNodeId, CancellationToken cancellationToken)
        {
            lock (syncRoot)
            {
                return Task.FromResult(ControlNodes.FirstOrDefault(controlNode =>
                    controlNode.CustomerId == customerId && controlNode.Id == controlNodeId));
            }
        }

        public Task<InventoryGroup?> FindInventoryGroupAsync(Guid customerId, Guid inventoryGroupId, CancellationToken cancellationToken)
        {
            lock (syncRoot)
            {
                return Task.FromResult(InventoryGroups.FirstOrDefault(group =>
                    group.CustomerId == customerId && group.Id == inventoryGroupId));
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

        public Task<VariableSet?> FindVariableSetAsync(Guid customerId, Guid variableSetId, CancellationToken cancellationToken)
        {
            lock (syncRoot)
            {
                return Task.FromResult(VariableSets.FirstOrDefault(variableSet =>
                    variableSet.CustomerId == customerId && variableSet.Id == variableSetId));
            }
        }

        public Task<IReadOnlyList<Job>> ListActiveJobsAsync(Guid customerId, CancellationToken cancellationToken)
        {
            lock (syncRoot)
            {
                return Task.FromResult<IReadOnlyList<Job>>(
                    Jobs.Where(job => job.CustomerId == customerId && job.Status == JobStatus.Active).ToArray());
            }
        }

        public Task<Job?> FindJobAsync(Guid customerId, Guid jobId, CancellationToken cancellationToken)
        {
            lock (syncRoot)
            {
                return Task.FromResult(Jobs.FirstOrDefault(job =>
                    job.CustomerId == customerId && job.Id == jobId));
            }
        }

        public Task<Job?> FindJobBySlugAsync(Guid customerId, string slug, CancellationToken cancellationToken)
        {
            lock (syncRoot)
            {
                return Task.FromResult(Jobs.FirstOrDefault(job =>
                    job.CustomerId == customerId && job.Slug == slug));
            }
        }

        public Task<IReadOnlyList<JobSchedule>> ListJobSchedulesAsync(Guid customerId, CancellationToken cancellationToken)
        {
            lock (syncRoot)
            {
                return Task.FromResult<IReadOnlyList<JobSchedule>>(
                    JobSchedules
                        .Where(schedule => schedule.CustomerId == customerId
                            && schedule.Status != JobScheduleStatus.Archived)
                        .OrderBy(schedule => schedule.Name)
                        .ToArray());
            }
        }

        public Task<JobSchedule?> FindJobScheduleAsync(Guid customerId, Guid jobScheduleId, CancellationToken cancellationToken)
        {
            lock (syncRoot)
            {
                return Task.FromResult(JobSchedules.FirstOrDefault(schedule =>
                    schedule.CustomerId == customerId && schedule.Id == jobScheduleId));
            }
        }

        public Task<JobSchedule?> FindJobScheduleBySlugAsync(Guid customerId, string slug, CancellationToken cancellationToken)
        {
            lock (syncRoot)
            {
                return Task.FromResult(JobSchedules.FirstOrDefault(schedule =>
                    schedule.CustomerId == customerId
                    && schedule.Slug == slug));
            }
        }

        public Task<IReadOnlyList<JobSchedule>> ListDueActiveJobSchedulesAsync(
            DateTimeOffset nowUtc,
            int limit,
            CancellationToken cancellationToken)
        {
            lock (syncRoot)
            {
                return Task.FromResult<IReadOnlyList<JobSchedule>>(
                    JobSchedules
                        .Where(schedule => schedule.Status == JobScheduleStatus.Active
                            && schedule.NextRunAtUtc is not null
                            && schedule.NextRunAtUtc <= nowUtc)
                        .OrderBy(schedule => schedule.NextRunAtUtc)
                        .Take(limit)
                        .ToArray());
            }
        }

        public Task<IReadOnlyList<JobRun>> ListJobRunsAsync(Guid customerId, CancellationToken cancellationToken)
        {
            lock (syncRoot)
            {
                return Task.FromResult<IReadOnlyList<JobRun>>(
                    JobRuns.Where(jobRun => jobRun.CustomerId == customerId).ToArray());
            }
        }

        public Task<JobRun?> FindJobRunAsync(Guid customerId, Guid jobRunId, CancellationToken cancellationToken)
        {
            lock (syncRoot)
            {
                return Task.FromResult(JobRuns.FirstOrDefault(jobRun =>
                    jobRun.CustomerId == customerId && jobRun.Id == jobRunId));
            }
        }

        public Task<long> GetNextJobRunLogSequenceAsync(Guid jobRunId, CancellationToken cancellationToken)
        {
            lock (syncRoot)
            {
                var currentMax = JobRunLogEntries
                    .Where(entry => entry.JobRunId == jobRunId)
                    .Select(entry => (long?)entry.Sequence)
                    .Max();
                return Task.FromResult((currentMax ?? 0) + 1);
            }
        }

        public Task<IReadOnlyList<JobRunLogEntry>> ListJobRunLogEntriesAsync(
            Guid jobRunId,
            long? afterSequence,
            int limit,
            CancellationToken cancellationToken)
        {
            lock (syncRoot)
            {
                return Task.FromResult<IReadOnlyList<JobRunLogEntry>>(
                    JobRunLogEntries
                        .Where(entry => entry.JobRunId == jobRunId
                            && (afterSequence == null || entry.Sequence > afterSequence))
                        .OrderBy(entry => entry.Sequence)
                        .Take(limit)
                        .ToArray());
            }
        }

        public Task<IReadOnlyList<AuditLogEntry>> ListAuditLogEntriesAsync(
            Guid customerId,
            AuditLogQuery query,
            int limit,
            CancellationToken cancellationToken)
        {
            lock (syncRoot)
            {
                var entries = AuditLogEntries.Where(entry => entry.CustomerId == customerId);

                if (!string.IsNullOrWhiteSpace(query.Action))
                {
                    entries = entries.Where(entry => entry.Action == query.Action);
                }

                if (!string.IsNullOrWhiteSpace(query.EntityType))
                {
                    entries = entries.Where(entry => entry.EntityType == query.EntityType);
                }

                if (query.EntityId is not null)
                {
                    entries = entries.Where(entry => entry.EntityId == query.EntityId);
                }

                if (query.ActorUserId is not null)
                {
                    entries = entries.Where(entry => entry.ActorUserId == query.ActorUserId);
                }

                if (!string.IsNullOrWhiteSpace(query.Outcome)
                    && Enum.TryParse<AuditOutcome>(query.Outcome, ignoreCase: true, out var outcome))
                {
                    entries = entries.Where(entry => entry.Outcome == outcome);
                }

                if (query.FromUtc is not null)
                {
                    entries = entries.Where(entry => entry.CreatedAtUtc >= query.FromUtc);
                }

                if (query.ToUtc is not null)
                {
                    entries = entries.Where(entry => entry.CreatedAtUtc <= query.ToUtc);
                }

                return Task.FromResult<IReadOnlyList<AuditLogEntry>>(
                    entries
                        .OrderByDescending(entry => entry.CreatedAtUtc)
                        .Take(limit)
                        .ToArray());
            }
        }

        public Task<AuditLogEntry?> FindAuditLogEntryAsync(
            Guid customerId,
            Guid auditLogEntryId,
            CancellationToken cancellationToken)
        {
            lock (syncRoot)
            {
                return Task.FromResult(AuditLogEntries.FirstOrDefault(entry =>
                    entry.CustomerId == customerId && entry.Id == auditLogEntryId));
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

        public void AddInventoryGroup(InventoryGroup inventoryGroup)
        {
            lock (syncRoot)
            {
                InventoryGroups.Add(inventoryGroup);
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

        public Job AddJob(Job job)
        {
            lock (syncRoot)
            {
                Jobs.Add(job);
                return job;
            }
        }

        void INodeControlDbContext.AddJob(Job job)
        {
            AddJob(job);
        }

        public JobSchedule AddScheduleForCustomer(Guid customerId, string suffix = "a")
        {
            lock (syncRoot)
            {
                var resources = AddDefinitionResources(customerId, suffix);
                var job = AddJob(Job.Create(
                    customerId,
                    $"Deploy {suffix}",
                    $"deploy-schedule-{suffix}",
                    null,
                    resources.ControlNode.Id,
                    resources.InventoryGroup.Id,
                    resources.Playbook.Id,
                    resources.VariableSet.Id,
                    1800,
                    TestTime));
                var schedule = JobSchedule.Create(
                    job,
                    $"Nightly {suffix}",
                    $"nightly-{suffix}",
                    null,
                    "0 * * * *",
                    "UTC",
                    TestTime.AddHours(1),
                    TestTime);
                JobSchedules.Add(schedule);
                return schedule;
            }
        }

        public void AddJobSchedule(JobSchedule jobSchedule)
        {
            lock (syncRoot)
            {
                JobSchedules.Add(jobSchedule);
            }
        }

        public void AddJobRun(JobRun jobRun)
        {
            lock (syncRoot)
            {
                JobRuns.Add(jobRun);
            }
        }

        public JobRun AddJobRunForCustomer(Guid customerId, Guid userId, string suffix = "a")
        {
            lock (syncRoot)
            {
                var resources = AddDefinitionResources(customerId, suffix);
                var job = AddJob(Job.Create(
                    customerId,
                    $"Deploy {suffix}",
                    $"deploy-{suffix}",
                    null,
                    resources.ControlNode.Id,
                    resources.InventoryGroup.Id,
                    resources.Playbook.Id,
                    resources.VariableSet.Id,
                    1800,
                    TestTime));
                var jobRun = JobRun.CreateManual(job, userId, TestTime);
                JobRuns.Add(jobRun);
                return jobRun;
            }
        }

        public JobRun AddTerminalJobRunForCustomer(
            Guid customerId,
            Guid userId,
            JobRunStatus status,
            string suffix = "a")
        {
            lock (syncRoot)
            {
                var jobRun = AddJobRunForCustomer(customerId, userId, suffix);
                jobRun.MarkRunning(TestTime);
                switch (status)
                {
                    case JobRunStatus.Succeeded:
                        jobRun.MarkSucceeded(0, TestTime);
                        break;
                    case JobRunStatus.Failed:
                        jobRun.MarkFailed(1, "failed", TestTime);
                        break;
                    case JobRunStatus.Cancelled:
                        jobRun.MarkCancelled(130, "cancelled", TestTime);
                        break;
                    case JobRunStatus.TimedOut:
                        jobRun.MarkTimedOut(null, "timeout", TestTime);
                        break;
                    default:
                        throw new ArgumentOutOfRangeException(nameof(status), status, "Status must be terminal.");
                }

                return jobRun;
            }
        }

        public void AddJobRunLogEntry(JobRunLogEntry jobRunLogEntry)
        {
            lock (syncRoot)
            {
                JobRunLogEntries.Add(jobRunLogEntry);
            }
        }

        public AuditLogEntry AddAuditLogEntryForCustomer(
            Guid customerId,
            string action,
            string entityType,
            DateTimeOffset createdAtUtc)
        {
            var entry = AuditLogEntry.Create(
                customerId,
                null,
                null,
                AuditActorType.System,
                action,
                entityType,
                Guid.NewGuid(),
                entityType,
                AuditOutcome.Succeeded,
                $"{action} message",
                null,
                null,
                null,
                createdAtUtc);
            AddAuditLogEntry(entry);
            return entry;
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
