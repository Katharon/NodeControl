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
using NodeControl.Application.JobRuns;
using NodeControl.Application.Jobs;
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
    public async Task Get_job_runs_requires_customer_membership()
    {
        await using var factory = DefinitionApiFactory.CreateWithoutMembership();
        var seeded = factory.SeedCurrentUserAndCustomer();
        using var client = factory.CreateClient();

        using var response = await client.GetAsync($"/api/v1/customers/{seeded.Customer.Id}/job-runs");

        Assert.Equal(HttpStatusCode.Forbidden, response.StatusCode);
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

        public List<JobRun> JobRuns { get; } = [];

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

        public void AddJobRun(JobRun jobRun)
        {
            lock (syncRoot)
            {
                JobRuns.Add(jobRun);
            }
        }

        public Task<int> SaveChangesAsync(CancellationToken cancellationToken)
        {
            return Task.FromResult(1);
        }
    }
}
