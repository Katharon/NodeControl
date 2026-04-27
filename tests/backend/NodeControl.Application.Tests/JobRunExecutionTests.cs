using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging.Abstractions;
using NodeControl.Application.Abstractions.Execution;
using NodeControl.Application.Abstractions.Persistence;
using NodeControl.Application.Abstractions.Time;
using NodeControl.Application.JobRuns;
using NodeControl.Domain.Customers;
using NodeControl.Domain.Inventories;
using NodeControl.Domain.Jobs;
using NodeControl.Domain.Nodes;
using NodeControl.Domain.Playbooks;
using NodeControl.Domain.Users;
using NodeControl.Domain.VariableSets;
using NodeControl.Worker.JobRuns;

namespace NodeControl.Application.Tests;

public sealed class JobRunExecutionTests
{
    [Fact]
    public async Task JobRunWorkspaceBuilder_creates_workspace_for_job_run()
    {
        using var fixture = ExecutionFixture.Create();
        var result = await fixture.CreateWorkspaceBuilder().BuildAsync(
            fixture.JobRun,
            fixture.Job,
            fixture.ControlNode,
            fixture.InventoryGroup,
            [fixture.ManagedNode],
            fixture.Playbook,
            fixture.VariableSet);

        Assert.True(result.Succeeded);
        Assert.True(Directory.Exists(result.Workspace!.WorkspacePath));
        Assert.Equal(fixture.JobRun.Id.ToString("D"), Path.GetFileName(result.Workspace.WorkspacePath));
    }

    [Fact]
    public async Task JobRunWorkspaceBuilder_writes_inventory_yml()
    {
        using var fixture = ExecutionFixture.Create();

        var result = await fixture.CreateWorkspaceBuilder().BuildAsync(
            fixture.JobRun,
            fixture.Job,
            fixture.ControlNode,
            fixture.InventoryGroup,
            [fixture.ManagedNode],
            fixture.Playbook,
            fixture.VariableSet);

        var inventory = await File.ReadAllTextAsync(result.Workspace!.InventoryPath);
        Assert.Contains("web:", inventory);
        Assert.Contains("web-01:", inventory);
        Assert.Contains("ansible_host: 10.0.0.10", inventory);
        Assert.Contains("ansible_port: 22", inventory);
    }

    [Fact]
    public async Task JobRunWorkspaceBuilder_writes_vars_yml_for_yaml_variable_set()
    {
        using var fixture = ExecutionFixture.Create(variableFormat: VariableSetFormat.Yaml, variableContent: "app_name: nodecontrol\n");

        var result = await fixture.CreateWorkspaceBuilder().BuildAsync(
            fixture.JobRun,
            fixture.Job,
            fixture.ControlNode,
            fixture.InventoryGroup,
            [fixture.ManagedNode],
            fixture.Playbook,
            fixture.VariableSet);

        Assert.Equal("vars.yml", result.Workspace!.VariableFileName);
        Assert.Equal("app_name: nodecontrol\n", await File.ReadAllTextAsync(result.Workspace.VariablePath));
    }

    [Fact]
    public async Task JobRunWorkspaceBuilder_writes_vars_json_for_json_variable_set()
    {
        using var fixture = ExecutionFixture.Create(variableFormat: VariableSetFormat.Json, variableContent: "{\"appName\":\"nodecontrol\"}");

        var result = await fixture.CreateWorkspaceBuilder().BuildAsync(
            fixture.JobRun,
            fixture.Job,
            fixture.ControlNode,
            fixture.InventoryGroup,
            [fixture.ManagedNode],
            fixture.Playbook,
            fixture.VariableSet);

        Assert.Equal("vars.json", result.Workspace!.VariableFileName);
        Assert.Equal("{\"appName\":\"nodecontrol\"}", await File.ReadAllTextAsync(result.Workspace.VariablePath));
    }

    [Fact]
    public async Task JobRunWorkspaceBuilder_writes_empty_vars_yml_when_job_has_no_variable_set()
    {
        using var fixture = ExecutionFixture.Create(includeVariableSet: false);

        var result = await fixture.CreateWorkspaceBuilder().BuildAsync(
            fixture.JobRun,
            fixture.Job,
            fixture.ControlNode,
            fixture.InventoryGroup,
            [fixture.ManagedNode],
            fixture.Playbook,
            null);

        Assert.Equal("vars.yml", result.Workspace!.VariableFileName);
        Assert.Equal("{}", await File.ReadAllTextAsync(result.Workspace.VariablePath));
    }

    [Fact]
    public async Task JobRunWorkspaceBuilder_writes_playbook_site_yml_for_inline_yaml()
    {
        using var fixture = ExecutionFixture.Create();

        var result = await fixture.CreateWorkspaceBuilder().BuildAsync(
            fixture.JobRun,
            fixture.Job,
            fixture.ControlNode,
            fixture.InventoryGroup,
            [fixture.ManagedNode],
            fixture.Playbook,
            fixture.VariableSet);

        Assert.Equal(fixture.Playbook.InlineContent, await File.ReadAllTextAsync(result.Workspace!.PlaybookPath));
    }

    [Fact]
    public async Task JobRunWorkspaceBuilder_fails_if_inventory_group_has_no_active_managed_nodes()
    {
        using var fixture = ExecutionFixture.Create();

        var result = await fixture.CreateWorkspaceBuilder().BuildAsync(
            fixture.JobRun,
            fixture.Job,
            fixture.ControlNode,
            fixture.InventoryGroup,
            [],
            fixture.Playbook,
            fixture.VariableSet);

        Assert.False(result.Succeeded);
        Assert.Contains("no active managed nodes", result.ErrorMessage);
    }

    [Fact]
    public async Task JobRunExecutionService_sets_running_then_succeeded_when_runner_returns_exit_code_zero()
    {
        using var fixture = ExecutionFixture.Create();
        var service = fixture.CreateExecutionService(new FakeAnsibleRunner(_ => new AnsiblePlaybookRunResult(0, false, null)));

        await service.ExecuteAsync(fixture.JobRun);

        Assert.Equal(JobRunStatus.Succeeded, fixture.JobRun.Status);
        Assert.Equal(0, fixture.JobRun.ExitCode);
        Assert.Equal(JobRunStatus.Running, fixture.Db.SavedJobRunStatuses[0][0]);
        Assert.Equal(JobRunStatus.Succeeded, fixture.Db.SavedJobRunStatuses[^1][0]);
    }

    [Fact]
    public async Task JobRunExecutionService_sets_failed_when_runner_returns_non_zero_exit_code()
    {
        using var fixture = ExecutionFixture.Create();
        var service = fixture.CreateExecutionService(new FakeAnsibleRunner(_ => new AnsiblePlaybookRunResult(2, false, "playbook failed")));

        await service.ExecuteAsync(fixture.JobRun);

        Assert.Equal(JobRunStatus.Failed, fixture.JobRun.Status);
        Assert.Equal(2, fixture.JobRun.ExitCode);
        Assert.Equal("playbook failed", fixture.JobRun.ErrorMessage);
    }

    [Fact]
    public async Task JobRunExecutionService_sets_timed_out_when_runner_reports_timeout()
    {
        using var fixture = ExecutionFixture.Create();
        var service = fixture.CreateExecutionService(new FakeAnsibleRunner(_ => new AnsiblePlaybookRunResult(null, true, "timeout")));

        await service.ExecuteAsync(fixture.JobRun);

        Assert.Equal(JobRunStatus.TimedOut, fixture.JobRun.Status);
        Assert.Equal("timeout", fixture.JobRun.ErrorMessage);
    }

    [Fact]
    public async Task JobRunExecutionService_sets_failed_when_workspace_creation_fails()
    {
        using var fixture = ExecutionFixture.Create();
        var service = fixture.CreateExecutionService(
            new FakeAnsibleRunner(_ => new AnsiblePlaybookRunResult(0, false, null)),
            new FailingWorkspaceBuilder("setup failed"));

        await service.ExecuteAsync(fixture.JobRun);

        Assert.Equal(JobRunStatus.Failed, fixture.JobRun.Status);
        Assert.Equal("setup failed", fixture.JobRun.ErrorMessage);
    }

    [Fact]
    public async Task JobRunExecutionService_stores_stdout_and_stderr_log_paths()
    {
        using var fixture = ExecutionFixture.Create();
        var service = fixture.CreateExecutionService(new FakeAnsibleRunner(_ => new AnsiblePlaybookRunResult(0, false, null)));

        await service.ExecuteAsync(fixture.JobRun);

        Assert.EndsWith("stdout.log", fixture.JobRun.StdoutLogPath);
        Assert.EndsWith("stderr.log", fixture.JobRun.StderrLogPath);
        Assert.Equal(fixture.JobRun.Id.ToString("D"), Path.GetFileName(fixture.JobRun.WorkspacePath));
    }

    [Fact]
    public async Task JobRunExecutionService_persists_system_stdout_and_stderr_log_entries()
    {
        using var fixture = ExecutionFixture.Create();
        var service = fixture.CreateExecutionService(new FakeAnsibleRunner(
            _ => new AnsiblePlaybookRunResult(1, false, "playbook failed"),
            StdoutLines: ["ok: [web-01]"],
            StderrLines: ["fatal: [web-01]"]));

        await service.ExecuteAsync(fixture.JobRun);

        Assert.Contains(fixture.Db.JobRunLogEntries, entry =>
            entry.Stream == JobRunLogStream.System
            && entry.Level == JobRunLogLevel.Info
            && entry.Message == "JobRun processing started.");
        Assert.Contains(fixture.Db.JobRunLogEntries, entry =>
            entry.Stream == JobRunLogStream.StdOut
            && entry.Level == JobRunLogLevel.Info
            && entry.Message == "ok: [web-01]");
        Assert.Contains(fixture.Db.JobRunLogEntries, entry =>
            entry.Stream == JobRunLogStream.StdErr
            && entry.Level == JobRunLogLevel.Error
            && entry.Message == "fatal: [web-01]");
        Assert.Equal(
            fixture.Db.JobRunLogEntries.Select(entry => entry.Sequence).Order().ToArray(),
            fixture.Db.JobRunLogEntries.Select(entry => entry.Sequence).ToArray());
    }

    [Fact]
    public async Task QueuedJobRunWorker_ignores_when_no_queued_job_runs_exist()
    {
        using var fixture = ExecutionFixture.Create(addJobRun: false);
        var runner = new FakeAnsibleRunner(_ => new AnsiblePlaybookRunResult(0, false, null));
        using var provider = fixture.CreateWorkerServiceProvider(runner);
        var worker = new QueuedJobRunWorker(
            provider.GetRequiredService<IServiceScopeFactory>(),
            NullLogger<QueuedJobRunWorker>.Instance);

        var processed = await worker.ExecuteOnceAsync();

        Assert.False(processed);
        Assert.Empty(runner.Requests);
    }

    [Fact]
    public async Task QueuedJobRunWorker_processes_the_oldest_queued_job_run()
    {
        using var fixture = ExecutionFixture.Create(addJobRun: false);
        var newer = fixture.AddJobRun(TestTime.AddMinutes(2));
        var older = fixture.AddJobRun(TestTime.AddMinutes(1));
        var runner = new FakeAnsibleRunner(_ => new AnsiblePlaybookRunResult(0, false, null));
        using var provider = fixture.CreateWorkerServiceProvider(runner);
        var worker = new QueuedJobRunWorker(
            provider.GetRequiredService<IServiceScopeFactory>(),
            NullLogger<QueuedJobRunWorker>.Instance);

        var processed = await worker.ExecuteOnceAsync();

        Assert.True(processed);
        Assert.Equal(JobRunStatus.Succeeded, older.Status);
        Assert.Equal(JobRunStatus.Queued, newer.Status);
        Assert.Equal(older.Id.ToString("D"), Path.GetFileName(runner.Requests.Single().WorkspacePath));
    }

    private static DateTimeOffset TestTime => new(2026, 4, 27, 10, 0, 0, TimeSpan.Zero);

    private sealed class FakeAnsibleRunner(
        Func<AnsiblePlaybookRunRequest, AnsiblePlaybookRunResult> handler,
        string[]? StdoutLines = null,
        string[]? StderrLines = null)
        : IAnsiblePlaybookRunner
    {
        public List<AnsiblePlaybookRunRequest> Requests { get; } = [];

        public async Task<AnsiblePlaybookRunResult> RunAsync(
            AnsiblePlaybookRunRequest request,
            CancellationToken cancellationToken = default)
        {
            Requests.Add(request);
            foreach (var line in StdoutLines ?? [])
            {
                if (request.OnStdoutLine is not null)
                {
                    await request.OnStdoutLine(line, cancellationToken);
                }
            }

            foreach (var line in StderrLines ?? [])
            {
                if (request.OnStderrLine is not null)
                {
                    await request.OnStderrLine(line, cancellationToken);
                }
            }

            return handler(request);
        }
    }

    private sealed class FailingWorkspaceBuilder(string errorMessage) : IJobRunWorkspaceBuilder
    {
        public Task<JobRunWorkspaceBuildResult> BuildAsync(
            JobRun jobRun,
            Job job,
            ControlNode controlNode,
            InventoryGroup inventoryGroup,
            IReadOnlyList<ManagedNode> managedNodes,
            Playbook playbook,
            VariableSet? variableSet,
            CancellationToken cancellationToken = default)
        {
            return Task.FromResult(JobRunWorkspaceBuildResult.Failed(errorMessage));
        }
    }

    private sealed class TestClock : IClock
    {
        public DateTimeOffset UtcNow { get; set; } = TestTime;
    }

    private sealed class ExecutionFixture : IDisposable
    {
        private ExecutionFixture(
            string workspaceRoot,
            NodeControlTestDbContext db,
            User user,
            Customer customer,
            ControlNode controlNode,
            InventoryGroup inventoryGroup,
            ManagedNode managedNode,
            Playbook playbook,
            VariableSet? variableSet,
            Job job,
            JobRun jobRun)
        {
            WorkspaceRoot = workspaceRoot;
            Db = db;
            User = user;
            Customer = customer;
            ControlNode = controlNode;
            InventoryGroup = inventoryGroup;
            ManagedNode = managedNode;
            Playbook = playbook;
            VariableSet = variableSet;
            Job = job;
            JobRun = jobRun;
        }

        public string WorkspaceRoot { get; }

        public NodeControlTestDbContext Db { get; }

        public User User { get; }

        public Customer Customer { get; }

        public ControlNode ControlNode { get; }

        public InventoryGroup InventoryGroup { get; }

        public ManagedNode ManagedNode { get; }

        public Playbook Playbook { get; }

        public VariableSet? VariableSet { get; }

        public Job Job { get; }

        public JobRun JobRun { get; }

        public static ExecutionFixture Create(
            bool includeVariableSet = true,
            VariableSetFormat variableFormat = VariableSetFormat.Yaml,
            string variableContent = "app_name: nodecontrol\n",
            bool addJobRun = true)
        {
            var workspaceRoot = Path.Combine(Path.GetTempPath(), "nodecontrol-tests", Guid.NewGuid().ToString("N"));
            var db = new NodeControlTestDbContext();
            var user = User.Create("Operator", "operator@nodecontrol.local", false, TestTime);
            var customer = Customer.Create("Customer A", "customer-a", null, TestTime);
            var controlNode = ControlNode.Create(customer.Id, "control", "control.local", 22, null, TestTime);
            var inventoryGroup = InventoryGroup.Create(customer.Id, "web", null, TestTime);
            var managedNode = ManagedNode.Create(customer.Id, "web-01", "10.0.0.10", 22, "Linux", "prod", null, TestTime);
            var playbook = Playbook.Create(
                customer.Id,
                "Deploy",
                "deploy",
                null,
                PlaybookSourceType.InlineYaml,
                "- hosts: all\n  tasks:\n    - debug:\n        msg: hello\n",
                null,
                TestTime);
            var variableSet = includeVariableSet
                ? VariableSet.Create(customer.Id, "Defaults", "defaults", null, variableFormat, variableContent, false, TestTime)
                : null;
            var job = Job.Create(
                customer.Id,
                "Deploy",
                "deploy",
                null,
                controlNode.Id,
                inventoryGroup.Id,
                playbook.Id,
                variableSet?.Id,
                1800,
                TestTime);
            var jobRun = JobRun.CreateManual(job, user.Id, TestTime);

            db.AddUser(user);
            db.AddCustomer(customer);
            db.AddControlNode(controlNode);
            db.AddInventoryGroup(inventoryGroup);
            db.AddManagedNode(managedNode);
            db.AddInventoryGroupNode(InventoryGroupNode.Create(inventoryGroup, managedNode, TestTime));
            db.AddPlaybook(playbook);
            if (variableSet is not null)
            {
                db.AddVariableSet(variableSet);
            }

            db.AddJob(job);
            if (addJobRun)
            {
                db.AddJobRun(jobRun);
            }

            return new ExecutionFixture(
                workspaceRoot,
                db,
                user,
                customer,
                controlNode,
                inventoryGroup,
                managedNode,
                playbook,
                variableSet,
                job,
                jobRun);
        }

        public JobRun AddJobRun(DateTimeOffset queuedAt)
        {
            var jobRun = JobRun.CreateManual(Job, User.Id, queuedAt);
            Db.AddJobRun(jobRun);
            return jobRun;
        }

        public JobRunWorkspaceBuilder CreateWorkspaceBuilder()
        {
            return new JobRunWorkspaceBuilder(WorkspaceRoot);
        }

        public JobRunExecutionService CreateExecutionService(
            IAnsiblePlaybookRunner runner,
            IJobRunWorkspaceBuilder? workspaceBuilder = null)
        {
            return new JobRunExecutionService(
                Db,
                workspaceBuilder ?? CreateWorkspaceBuilder(),
                runner,
                new TestClock());
        }

        public ServiceProvider CreateWorkerServiceProvider(IAnsiblePlaybookRunner runner)
        {
            var services = new ServiceCollection();
            services.AddSingleton<INodeControlDbContext>(Db);
            services.AddSingleton<IClock>(new TestClock());
            services.AddSingleton<IJobRunWorkspaceBuilder>(CreateWorkspaceBuilder());
            services.AddSingleton(runner);
            services.AddScoped<JobRunExecutionService>();
            return services.BuildServiceProvider();
        }

        public void Dispose()
        {
            if (Directory.Exists(WorkspaceRoot))
            {
                Directory.Delete(WorkspaceRoot, recursive: true);
            }
        }
    }
}
