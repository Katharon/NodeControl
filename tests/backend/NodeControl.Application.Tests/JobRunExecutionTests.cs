using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.Options;
using NodeControl.Application.Abstractions.Execution;
using NodeControl.Application.Abstractions.Persistence;
using NodeControl.Application.Abstractions.Security;
using NodeControl.Application.Abstractions.Time;
using NodeControl.Application.JobRuns;
using NodeControl.Application.Secrets;
using NodeControl.Domain.Customers;
using NodeControl.Domain.Inventories;
using NodeControl.Domain.Jobs;
using NodeControl.Domain.Nodes;
using NodeControl.Domain.Playbooks;
using NodeControl.Domain.Secrets;
using NodeControl.Domain.Templates;
using NodeControl.Domain.Users;
using NodeControl.Domain.VariableSets;
using NodeControl.Infrastructure.Execution;
using NodeControl.Worker.JobRuns;

namespace NodeControl.Application.Tests;

public sealed class JobRunExecutionTests
{
    private static readonly IReadOnlyDictionary<string, string> EmptySecrets =
        new Dictionary<string, string>(StringComparer.Ordinal);

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
            fixture.VariableSet,
            [],
            EmptySecrets);

        Assert.True(result.Succeeded);
        Assert.True(Directory.Exists(result.Workspace!.WorkspacePath));
        Assert.Equal(fixture.JobRun.Id.ToString("D"), Path.GetFileName(result.Workspace.WorkspacePath));
        Assert.Contains(fixture.ControlNode.Id.ToString("D"), result.Workspace.WorkspacePath);
    }

    [Fact]
    public async Task JobRunWorkspaceBuilder_resets_existing_workspace_for_same_run()
    {
        using var fixture = ExecutionFixture.Create();
        var first = await BuildWorkspaceAsync(fixture);
        var staleFile = Path.Combine(first.WorkspacePath, "playbook", "stale.yml");
        await File.WriteAllTextAsync(staleFile, "stale: true\n");

        var second = await BuildWorkspaceAsync(fixture);

        Assert.Equal(first.WorkspacePath, second.WorkspacePath);
        Assert.False(File.Exists(staleFile));
        Assert.True(File.Exists(second.PlaybookPath));
    }

    [Fact]
    public async Task JobRunWorkspaceBuilder_uses_isolated_paths_for_retry_runs()
    {
        using var fixture = ExecutionFixture.Create();
        fixture.JobRun.MarkRunning(TestTime.AddMinutes(1));
        fixture.JobRun.MarkFailed(1, "failed", TestTime.AddMinutes(2));
        var retry = JobRun.CreateRetry(fixture.JobRun, fixture.Job, fixture.User.Id, TestTime.AddMinutes(3));

        var originalWorkspace = await BuildWorkspaceAsync(fixture);
        var retryResult = await fixture.CreateWorkspaceBuilder().BuildAsync(
            retry,
            fixture.Job,
            fixture.ControlNode,
            fixture.InventoryGroup,
            [fixture.ManagedNode],
            fixture.Playbook,
            fixture.VariableSet,
            [],
            EmptySecrets);

        Assert.True(retryResult.Succeeded);
        Assert.NotEqual(originalWorkspace.WorkspacePath, retryResult.Workspace!.WorkspacePath);
        Assert.Equal(retry.Id.ToString("D"), Path.GetFileName(retryResult.Workspace.WorkspacePath));
        Assert.Equal(fixture.JobRun.Id.ToString("D"), Path.GetFileName(originalWorkspace.WorkspacePath));
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
            fixture.VariableSet,
            [],
            EmptySecrets);

        var inventory = await File.ReadAllTextAsync(result.Workspace!.InventoryPath);
        Assert.Contains("web:", inventory);
        Assert.Contains("web-01:", inventory);
        Assert.Contains("ansible_host: 10.0.0.10", inventory);
        Assert.Contains("ansible_port: 22", inventory);
    }

    [Fact]
    public async Task JobRunWorkspaceBuilder_marks_simple_localhost_managed_nodes_as_local_connection()
    {
        using var fixture = ExecutionFixture.Create();
        fixture.ManagedNode.Update(
            fixture.ManagedNode.Name,
            "localhost",
            fixture.ManagedNode.SshPort,
            null,
            null,
            fixture.ManagedNode.OperatingSystem,
            fixture.ManagedNode.Environment,
            fixture.ManagedNode.Description,
            TestTime.AddMinutes(1));

        var result = await fixture.CreateWorkspaceBuilder().BuildAsync(
            fixture.JobRun,
            fixture.Job,
            fixture.ControlNode,
            fixture.InventoryGroup,
            [fixture.ManagedNode],
            fixture.Playbook,
            fixture.VariableSet,
            [],
            EmptySecrets);

        var inventory = await File.ReadAllTextAsync(result.Workspace!.InventoryPath);
        Assert.Contains("ansible_connection: local", inventory);
    }

    [Fact]
    public async Task JobRunWorkspaceBuilder_writes_managed_node_ssh_settings_to_inventory()
    {
        using var fixture = ExecutionFixture.Create();
        var secretId = Guid.NewGuid();
        fixture.ManagedNode.Update(
            fixture.ManagedNode.Name,
            fixture.ManagedNode.Hostname,
            2222,
            "deploy",
            secretId,
            fixture.ManagedNode.OperatingSystem,
            fixture.ManagedNode.Environment,
            fixture.ManagedNode.Description,
            TestTime.AddMinutes(1));

        var result = await fixture.CreateWorkspaceBuilder().BuildAsync(
            fixture.JobRun,
            fixture.Job,
            fixture.ControlNode,
            fixture.InventoryGroup,
            [fixture.ManagedNode],
            fixture.Playbook,
            fixture.VariableSet,
            [],
            EmptySecrets);

        var inventory = await File.ReadAllTextAsync(result.Workspace!.InventoryPath);
        Assert.Contains("ansible_port: 2222", inventory);
        Assert.Contains("ansible_user: deploy", inventory);
        Assert.Contains($".nodecontrol/managed-host-keys/{fixture.ManagedNode.Id:D}.key", inventory);
        Assert.Contains("ansible_ssh_common_args: -o IdentitiesOnly=yes", inventory);
        Assert.DoesNotContain("ansible_connection: local", inventory);
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
            fixture.VariableSet,
            [],
            EmptySecrets);

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
            fixture.VariableSet,
            [],
            EmptySecrets);

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
            null,
            [],
            EmptySecrets);

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
            fixture.VariableSet,
            [],
            EmptySecrets);

        Assert.Equal(fixture.Playbook.InlineContent, await File.ReadAllTextAsync(result.Workspace!.PlaybookPath));
        Assert.Equal("playbook/site.yml", result.Workspace.PlaybookFileName);
    }

    [Fact]
    public async Task JobRunWorkspaceBuilder_writes_control_node_dispatch_manifest()
    {
        using var fixture = ExecutionFixture.Create();

        var result = await fixture.CreateWorkspaceBuilder().BuildAsync(
            fixture.JobRun,
            fixture.Job,
            fixture.ControlNode,
            fixture.InventoryGroup,
            [fixture.ManagedNode],
            fixture.Playbook,
            fixture.VariableSet,
            [],
            EmptySecrets);

        Assert.True(result.Succeeded);
        var manifest = await File.ReadAllTextAsync(result.Workspace!.DispatchManifestPath);
        Assert.Contains(fixture.JobRun.Id.ToString("D"), manifest);
        Assert.Contains(fixture.ControlNode.Id.ToString("D"), manifest);
        Assert.Contains(fixture.ControlNode.Hostname, manifest);
    }

    [Fact]
    public async Task JobRunWorkspaceBuilder_materializes_artifact_directory_playbook()
    {
        using var fixture = ExecutionFixture.Create(artifactPlaybook: true);

        var result = await fixture.CreateWorkspaceBuilder().BuildAsync(
            fixture.JobRun,
            fixture.Job,
            fixture.ControlNode,
            fixture.InventoryGroup,
            [fixture.ManagedNode],
            fixture.Playbook,
            fixture.VariableSet,
            [],
            EmptySecrets);

        Assert.True(result.Succeeded);
        Assert.Equal("playbook/site.yml", result.Workspace!.PlaybookFileName);
        Assert.Equal(
            "- hosts: all\n  roles:\n    - app\n",
            await File.ReadAllTextAsync(result.Workspace.PlaybookPath));
        Assert.True(File.Exists(Path.Combine(result.Workspace.WorkspacePath, "playbook", "roles", "app", "tasks", "main.yml")));
    }

    [Fact]
    public async Task JobRunWorkspaceBuilder_materializes_template_artifacts_and_secret_references()
    {
        using var fixture = ExecutionFixture.Create();
        var template = Template.Create(
            fixture.Customer.Id,
            "App config",
            "app-config",
            null,
            TemplateType.ConfigFile,
            "token=secret://api-token\n",
            "ini",
            TestTime);

        var result = await fixture.CreateWorkspaceBuilder().BuildAsync(
            fixture.JobRun,
            fixture.Job,
            fixture.ControlNode,
            fixture.InventoryGroup,
            [fixture.ManagedNode],
            fixture.Playbook,
            fixture.VariableSet,
            [new JobRunTemplateArtifact(template, "templates/app.conf")],
            new Dictionary<string, string>(StringComparer.Ordinal)
            {
                ["api-token"] = "resolved-secret-value"
            });

        Assert.True(result.Succeeded);
        Assert.Equal(
            "token=resolved-secret-value\n",
            await File.ReadAllTextAsync(Path.Combine(result.Workspace!.WorkspacePath, "playbook", "templates", "app.conf")));
    }

    [Fact]
    public async Task JobRunExecutionService_resolves_secret_references_only_during_worker_materialization_and_redacts_logs()
    {
        using var fixture = ExecutionFixture.Create(
            variableContent: "api_token: secret://api-token\n",
            templateArtifactPath: "templates/app.conf",
            templateContent: "token=secret://api-token\n",
            protectedSecretValue: "protected:resolved-secret-value");
        var service = fixture.CreateExecutionService(new FakeAnsibleRunner(
            request =>
            {
                Assert.Equal(
                    "api_token: resolved-secret-value\n",
                    File.ReadAllText(Path.Combine(request.WorkspacePath, request.VariableFileName)));
                Assert.Equal(
                    "token=resolved-secret-value\n",
                    File.ReadAllText(Path.Combine(request.WorkspacePath, "playbook", "templates", "app.conf")));
                return new AnsiblePlaybookRunResult(1, false, false, "failed with resolved-secret-value");
            },
            StdoutLines: ["stdout resolved-secret-value"],
            StderrLines: ["stderr resolved-secret-value"]));

        await service.ExecuteAsync(fixture.JobRun);

        Assert.Equal(JobRunStatus.Failed, fixture.JobRun.Status);
        Assert.DoesNotContain("resolved-secret-value", fixture.JobRun.ErrorMessage);
        Assert.DoesNotContain(fixture.Db.JobRunLogEntries, entry => entry.Message.Contains("resolved-secret-value", StringComparison.Ordinal));
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
            fixture.VariableSet,
            [],
            EmptySecrets);

        Assert.False(result.Succeeded);
        Assert.Contains("no active managed nodes", result.ErrorMessage);
    }

    [Fact]
    public async Task JobRunExecutionService_sets_running_then_succeeded_when_runner_returns_exit_code_zero()
    {
        using var fixture = ExecutionFixture.Create();
        var service = fixture.CreateExecutionService(new FakeAnsibleRunner(_ => new AnsiblePlaybookRunResult(0, false, false, null)));

        await service.ExecuteAsync(fixture.JobRun);

        Assert.Equal(JobRunStatus.Succeeded, fixture.JobRun.Status);
        Assert.Equal(0, fixture.JobRun.ExitCode);
        Assert.Equal(JobRunStatus.Running, fixture.Db.SavedJobRunStatuses[0][0]);
        Assert.Equal(JobRunStatus.Succeeded, fixture.Db.SavedJobRunStatuses[^1][0]);
    }

    [Fact]
    public async Task JobRunExecutionService_dispatches_to_run_bound_control_node_snapshot()
    {
        using var fixture = ExecutionFixture.Create();
        var secondControlNode = ControlNode.Create(fixture.Customer.Id, "control-2", "localhost", 22, null, TestTime);
        fixture.Db.AddControlNode(secondControlNode);
        fixture.Job.Update(
            fixture.Job.Name,
            fixture.Job.Slug,
            fixture.Job.Description,
            secondControlNode.Id,
            fixture.Job.InventoryGroupId,
            fixture.Job.PlaybookId,
            fixture.Job.VariableSetId,
            fixture.Job.DefaultTimeoutSeconds,
            TestTime.AddMinutes(1),
            fixture.Job.TemplateArtifactsJson);
        var dispatcher = new CapturingControlNodeDispatcher(new ControlNodeDispatchResult(0, false, false, null));
        var service = fixture.CreateExecutionService(dispatcher);

        await service.ExecuteAsync(fixture.JobRun);

        Assert.Equal(JobRunStatus.Succeeded, fixture.JobRun.Status);
        Assert.Equal(fixture.ControlNode.Id, dispatcher.Requests.Single().ControlNode.Id);
        Assert.Equal(fixture.ControlNode.Id, fixture.JobRun.ControlNodeId);
        Assert.NotEqual(fixture.Job.ControlNodeId, fixture.JobRun.ControlNodeId);
    }

    [Fact]
    public async Task JobRunExecutionService_resolves_control_node_ssh_key_only_for_dispatch_and_redacts_it()
    {
        using var fixture = ExecutionFixture.Create(controlNodeHostname: "control.example.test");
        var keySecret = Secret.Create(
            fixture.Customer.Id,
            "Control key",
            "control-key",
            null,
            SecretKind.SshPrivateKey,
            "protected:-----BEGIN PRIVATE KEY-----\nremote-key\n-----END PRIVATE KEY-----",
            TestTime);
        fixture.Db.AddSecret(keySecret);
        fixture.ControlNode.Update(
            fixture.ControlNode.Name,
            fixture.ControlNode.Hostname,
            fixture.ControlNode.SshPort,
            "ansible",
            keySecret.Id,
            "/var/lib/nodecontrol/remote-runs",
            fixture.ControlNode.Description,
            TestTime.AddMinutes(1));
        var dispatcher = new CapturingControlNodeDispatcher(new ControlNodeDispatchResult(
            255,
            false,
            false,
            "ssh failed with -----BEGIN PRIVATE KEY-----\nremote-key\n-----END PRIVATE KEY-----"));
        var service = fixture.CreateExecutionService(dispatcher);

        await service.ExecuteAsync(fixture.JobRun);

        Assert.Equal(JobRunStatus.Failed, fixture.JobRun.Status);
        Assert.Equal("-----BEGIN PRIVATE KEY-----\nremote-key\n-----END PRIVATE KEY-----", dispatcher.Requests.Single().CredentialMaterial!.SshPrivateKey);
        Assert.DoesNotContain("remote-key", fixture.JobRun.ErrorMessage);
        Assert.DoesNotContain(fixture.Db.JobRunLogEntries, entry => entry.Message.Contains("remote-key", StringComparison.Ordinal));
    }

    [Fact]
    public async Task JobRunExecutionService_resolves_managed_node_ssh_key_only_during_worker_materialization_and_redacts_it()
    {
        using var fixture = ExecutionFixture.Create();
        var keySecret = Secret.Create(
            fixture.Customer.Id,
            "Host key",
            "host-key",
            null,
            SecretKind.SshPrivateKey,
            "protected:managed-node-private-key",
            TestTime);
        fixture.Db.AddSecret(keySecret);
        fixture.ManagedNode.Update(
            fixture.ManagedNode.Name,
            fixture.ManagedNode.Hostname,
            fixture.ManagedNode.SshPort,
            "deploy",
            keySecret.Id,
            fixture.ManagedNode.OperatingSystem,
            fixture.ManagedNode.Environment,
            fixture.ManagedNode.Description,
            TestTime.AddMinutes(1));
        var dispatcher = new InspectingControlNodeDispatcher(request =>
        {
            var keyPath = Path.Combine(
                request.Workspace.WorkspacePath,
                ".nodecontrol",
                "managed-host-keys",
                $"{fixture.ManagedNode.Id:D}.key");
            Assert.True(File.Exists(keyPath));
            Assert.Equal("managed-node-private-key\n", File.ReadAllText(keyPath));
            Assert.Contains("ansible_user: deploy", File.ReadAllText(request.Workspace.InventoryPath));
            Assert.Contains($".nodecontrol/managed-host-keys/{fixture.ManagedNode.Id:D}.key", File.ReadAllText(request.Workspace.InventoryPath));
            Assert.Contains("ansible_ssh_common_args: -o IdentitiesOnly=yes", File.ReadAllText(request.Workspace.InventoryPath));

            return new ControlNodeDispatchResult(2, false, false, "failed with managed-node-private-key");
        });
        var service = fixture.CreateExecutionService(dispatcher);

        await service.ExecuteAsync(fixture.JobRun);

        Assert.Equal(JobRunStatus.Failed, fixture.JobRun.Status);
        Assert.DoesNotContain("managed-node-private-key", fixture.JobRun.ErrorMessage);
        Assert.DoesNotContain(fixture.Db.JobRunLogEntries, entry => entry.Message.Contains("managed-node-private-key", StringComparison.Ordinal));
    }

    [Fact]
    public async Task JobRunExecutionService_sets_failed_when_runner_returns_non_zero_exit_code()
    {
        using var fixture = ExecutionFixture.Create();
        var service = fixture.CreateExecutionService(new FakeAnsibleRunner(_ => new AnsiblePlaybookRunResult(2, false, false, "playbook failed")));

        await service.ExecuteAsync(fixture.JobRun);

        Assert.Equal(JobRunStatus.Failed, fixture.JobRun.Status);
        Assert.Equal(2, fixture.JobRun.ExitCode);
        Assert.Equal("playbook failed", fixture.JobRun.ErrorMessage);
    }

    [Fact]
    public async Task JobRunExecutionService_sets_timed_out_when_runner_reports_timeout()
    {
        using var fixture = ExecutionFixture.Create();
        var service = fixture.CreateExecutionService(new FakeAnsibleRunner(_ => new AnsiblePlaybookRunResult(null, true, false, "timeout")));

        await service.ExecuteAsync(fixture.JobRun);

        Assert.Equal(JobRunStatus.TimedOut, fixture.JobRun.Status);
        Assert.Equal("timeout", fixture.JobRun.ErrorMessage);
    }

    [Fact]
    public async Task JobRunExecutionService_sets_failed_when_workspace_creation_fails()
    {
        using var fixture = ExecutionFixture.Create();
        var service = fixture.CreateExecutionService(
            new FakeAnsibleRunner(_ => new AnsiblePlaybookRunResult(0, false, false, null)),
            new FailingWorkspaceBuilder("setup failed"));

        await service.ExecuteAsync(fixture.JobRun);

        Assert.Equal(JobRunStatus.Failed, fixture.JobRun.Status);
        Assert.Equal("setup failed", fixture.JobRun.ErrorMessage);
    }

    [Fact]
    public async Task JobRunExecutionService_stores_stdout_and_stderr_log_paths()
    {
        using var fixture = ExecutionFixture.Create();
        var service = fixture.CreateExecutionService(new FakeAnsibleRunner(_ => new AnsiblePlaybookRunResult(0, false, false, null)));

        await service.ExecuteAsync(fixture.JobRun);

        Assert.EndsWith("stdout.log", fixture.JobRun.StdoutLogPath);
        Assert.EndsWith("stderr.log", fixture.JobRun.StderrLogPath);
        Assert.Equal(fixture.JobRun.Id.ToString("D"), Path.GetFileName(fixture.JobRun.WorkspacePath));
    }

    [Fact]
    public async Task JobRunExecutionService_skips_job_run_cancelled_while_queued()
    {
        using var fixture = ExecutionFixture.Create();
        fixture.JobRun.RequestCancellation(fixture.User.Id, "no longer needed", TestTime);
        var runner = new FakeAnsibleRunner(_ => new AnsiblePlaybookRunResult(0, false, false, null));
        var service = fixture.CreateExecutionService(runner);

        await service.ExecuteAsync(fixture.JobRun);

        Assert.Empty(runner.Requests);
        Assert.Equal(JobRunStatus.Cancelled, fixture.JobRun.Status);
    }

    [Fact]
    public async Task JobRunExecutionService_detects_running_cancellation_and_marks_cancelled()
    {
        using var fixture = ExecutionFixture.Create();
        var runner = new CancellingAnsibleRunner(fixture.JobRun, fixture.User.Id);
        var service = fixture.CreateExecutionService(runner);

        await service.ExecuteAsync(fixture.JobRun);

        Assert.True(runner.ObservedCancellation);
        Assert.Equal(JobRunStatus.Cancelled, fixture.JobRun.Status);
        Assert.Contains(fixture.Db.JobRunLogEntries, entry =>
            entry.Stream == JobRunLogStream.System
            && entry.Message == "Cancellation observed by worker.");
        Assert.Contains(fixture.Db.JobRunLogEntries, entry =>
            entry.Stream == JobRunLogStream.System
            && entry.Message == "JobRun cancelled.");
    }

    [Fact]
    public async Task JobRunExecutionService_does_not_overwrite_cancelled_with_succeeded()
    {
        using var fixture = ExecutionFixture.Create();
        var service = fixture.CreateExecutionService(new CancellingAnsibleRunner(fixture.JobRun, fixture.User.Id));

        await service.ExecuteAsync(fixture.JobRun);

        Assert.Equal(JobRunStatus.Cancelled, fixture.JobRun.Status);
        Assert.NotEqual(JobRunStatus.Succeeded, fixture.JobRun.Status);
    }

    [Fact]
    public async Task JobRunExecutionService_persists_system_stdout_and_stderr_log_entries()
    {
        using var fixture = ExecutionFixture.Create();
        var service = fixture.CreateExecutionService(new FakeAnsibleRunner(
            _ => new AnsiblePlaybookRunResult(1, false, false, "playbook failed"),
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
    public async Task ControlNodeDispatcher_runs_local_control_node_through_ansible_runner()
    {
        using var fixture = ExecutionFixture.Create(controlNodeHostname: "localhost");
        var workspace = await BuildWorkspaceAsync(fixture);
        var runner = new FakeAnsibleRunner(_ => new AnsiblePlaybookRunResult(0, false, false, null));
        var dispatcher = new ControlNodeDispatcher(runner, Options.Create(new ExecutionOptions()));

        var result = await dispatcher.DispatchAsync(new ControlNodeDispatchRequest(
            fixture.JobRun,
            fixture.ControlNode,
            workspace,
            TimeSpan.FromSeconds(30)));

        Assert.Equal(0, result.ExitCode);
        Assert.Single(runner.Requests);
        Assert.Equal(workspace.WorkspacePath, runner.Requests.Single().WorkspacePath);
    }

    [Fact]
    public async Task ControlNodeDispatcher_removes_local_managed_host_key_files_after_local_execution()
    {
        using var fixture = ExecutionFixture.Create(controlNodeHostname: "localhost");
        var workspace = await BuildWorkspaceAsync(fixture);
        var keyDirectory = Path.Combine(workspace.WorkspacePath, ".nodecontrol", "managed-host-keys");
        Directory.CreateDirectory(keyDirectory);
        await File.WriteAllTextAsync(Path.Combine(keyDirectory, "host.key"), "private-key");
        var runner = new FakeAnsibleRunner(_ => new AnsiblePlaybookRunResult(0, false, false, null));
        var dispatcher = new ControlNodeDispatcher(runner, Options.Create(new ExecutionOptions()));

        var result = await dispatcher.DispatchAsync(new ControlNodeDispatchRequest(
            fixture.JobRun,
            fixture.ControlNode,
            workspace,
            TimeSpan.FromSeconds(30)));

        Assert.Equal(0, result.ExitCode);
        Assert.False(Directory.Exists(keyDirectory));
    }

    [Fact]
    public async Task ControlNodeDispatcher_rejects_non_local_control_node_without_remote_settings()
    {
        using var fixture = ExecutionFixture.Create(controlNodeHostname: "control.example.test");
        var workspace = await BuildWorkspaceAsync(fixture);
        var runner = new FakeAnsibleRunner(_ => new AnsiblePlaybookRunResult(0, false, false, null));
        var dispatcher = new ControlNodeDispatcher(runner, Options.Create(new ExecutionOptions()));

        var result = await dispatcher.DispatchAsync(new ControlNodeDispatchRequest(
            fixture.JobRun,
            fixture.ControlNode,
            workspace,
            TimeSpan.FromSeconds(30)));

        Assert.Null(result.ExitCode);
        Assert.Contains("requires SSH remote dispatch settings", result.ErrorMessage);
        Assert.Empty(runner.Requests);
    }

    [Fact]
    public async Task ControlNodeDispatcher_rejects_non_local_control_node_without_private_key_material()
    {
        using var fixture = ExecutionFixture.Create(controlNodeHostname: "control.example.test");
        fixture.ControlNode.Update(
            fixture.ControlNode.Name,
            fixture.ControlNode.Hostname,
            fixture.ControlNode.SshPort,
            "ansible",
            Guid.NewGuid(),
            "/var/lib/nodecontrol/remote-runs",
            fixture.ControlNode.Description,
            TestTime.AddMinutes(1));
        var workspace = await BuildWorkspaceAsync(fixture);
        var runner = new FakeAnsibleRunner(_ => new AnsiblePlaybookRunResult(0, false, false, null));
        var dispatcher = new ControlNodeDispatcher(runner, Options.Create(new ExecutionOptions()));

        var result = await dispatcher.DispatchAsync(new ControlNodeDispatchRequest(
            fixture.JobRun,
            fixture.ControlNode,
            workspace,
            TimeSpan.FromSeconds(30)));

        Assert.Null(result.ExitCode);
        Assert.Contains("private key material is unavailable", result.ErrorMessage);
        Assert.Empty(runner.Requests);
    }

    [Fact]
    public async Task ControlNodeDispatcher_stages_remote_workspace_to_temporary_path_then_promotes()
    {
        using var fixture = ExecutionFixture.Create(controlNodeHostname: "control.example.test");
        ConfigureRemoteControlNode(fixture);
        var workspace = await BuildWorkspaceAsync(fixture);
        var runner = new FakeAnsibleRunner(_ => new AnsiblePlaybookRunResult(0, false, false, null));
        var remoteRunner = new FakeRemoteCommandRunner();
        var dispatcher = new ControlNodeDispatcher(runner, Options.Create(new ExecutionOptions()), remoteRunner);

        var result = await dispatcher.DispatchAsync(new ControlNodeDispatchRequest(
            fixture.JobRun,
            fixture.ControlNode,
            workspace,
            TimeSpan.FromSeconds(30),
            new ControlNodeCredentialMaterial("-----BEGIN PRIVATE KEY-----\ntest\n-----END PRIVATE KEY-----")));

        var remoteRunPath = BuildExpectedRemoteRunPath(fixture);
        Assert.Equal(0, result.ExitCode);
        Assert.Empty(runner.Requests);
        Assert.Equal(new[] { "ssh", "scp", "ssh", "ssh", "ssh" }, remoteRunner.Invocations.Select(invocation => invocation.FileName).ToArray());
        Assert.Contains(".staging-", remoteRunner.Invocations[0].Arguments[^1]);
        Assert.Contains("mkdir -p", remoteRunner.Invocations[0].Arguments[^1]);
        Assert.Contains(".staging-", remoteRunner.Invocations[1].Arguments[^1]);
        Assert.Contains($"rm -rf -- '{remoteRunPath}'", remoteRunner.Invocations[2].Arguments[^1]);
        Assert.Contains("mv --", remoteRunner.Invocations[2].Arguments[^1]);
        Assert.Contains($"'{remoteRunPath}'", remoteRunner.Invocations[2].Arguments[^1]);
        Assert.Contains($"cd '{remoteRunPath}'", remoteRunner.Invocations[3].Arguments[^1]);
        Assert.Contains("exec 'ansible-playbook'", remoteRunner.Invocations[3].Arguments[^1]);
        Assert.Contains($"{remoteRunPath}/.nodecontrol/managed-host-keys", remoteRunner.Invocations[4].Arguments[^1]);
        AssertNoTemporaryKeyDirectory(fixture.JobRun.Id);
    }

    [Fact]
    public async Task ControlNodeDispatcher_cleans_remote_staging_directory_when_staging_fails()
    {
        using var fixture = ExecutionFixture.Create(controlNodeHostname: "control.example.test");
        ConfigureRemoteControlNode(fixture);
        var workspace = await BuildWorkspaceAsync(fixture);
        var runner = new FakeAnsibleRunner(_ => new AnsiblePlaybookRunResult(0, false, false, null));
        var remoteRunner = new FakeRemoteCommandRunner(
            new RemoteCommandResult(0, false, false, null),
            new RemoteCommandResult(1, false, false, "scp failed"));
        var dispatcher = new ControlNodeDispatcher(runner, Options.Create(new ExecutionOptions()), remoteRunner);

        var result = await dispatcher.DispatchAsync(new ControlNodeDispatchRequest(
            fixture.JobRun,
            fixture.ControlNode,
            workspace,
            TimeSpan.FromSeconds(30),
            new ControlNodeCredentialMaterial("-----BEGIN PRIVATE KEY-----\ntest\n-----END PRIVATE KEY-----")));

        Assert.Equal(1, result.ExitCode);
        Assert.Equal("scp failed", result.ErrorMessage);
        Assert.Equal(new[] { "ssh", "scp", "ssh" }, remoteRunner.Invocations.Select(invocation => invocation.FileName).ToArray());
        Assert.Contains("rm -rf --", remoteRunner.Invocations[^1].Arguments[^1]);
        Assert.Contains(".staging-", remoteRunner.Invocations[^1].Arguments[^1]);
        Assert.DoesNotContain(remoteRunner.Invocations, invocation =>
            invocation.FileName == "ssh"
            && invocation.Arguments[^1].Contains("exec 'ansible-playbook'", StringComparison.Ordinal));
        AssertNoTemporaryKeyDirectory(fixture.JobRun.Id);
    }

    [Fact]
    public async Task QueuedJobRunWorker_ignores_when_no_queued_job_runs_exist()
    {
        using var fixture = ExecutionFixture.Create(addJobRun: false);
        var runner = new FakeAnsibleRunner(_ => new AnsiblePlaybookRunResult(0, false, false, null));
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
        var runner = new FakeAnsibleRunner(_ => new AnsiblePlaybookRunResult(0, false, false, null));
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

    private static async Task<JobRunWorkspace> BuildWorkspaceAsync(ExecutionFixture fixture)
    {
        var result = await fixture.CreateWorkspaceBuilder().BuildAsync(
            fixture.JobRun,
            fixture.Job,
            fixture.ControlNode,
            fixture.InventoryGroup,
            [fixture.ManagedNode],
            fixture.Playbook,
            fixture.VariableSet,
            [],
            EmptySecrets);

        Assert.True(result.Succeeded);
        return result.Workspace!;
    }

    private static void ConfigureRemoteControlNode(ExecutionFixture fixture)
    {
        fixture.ControlNode.Update(
            fixture.ControlNode.Name,
            fixture.ControlNode.Hostname,
            fixture.ControlNode.SshPort,
            "ansible",
            Guid.NewGuid(),
            "/var/lib/nodecontrol/remote-runs",
            fixture.ControlNode.Description,
            TestTime.AddMinutes(1));
    }

    private static string BuildExpectedRemoteRunPath(ExecutionFixture fixture)
    {
        return string.Join(
            '/',
            "/var/lib/nodecontrol/remote-runs",
            fixture.JobRun.CustomerId.ToString("D"),
            "control-nodes",
            fixture.ControlNode.Id.ToString("D"),
            "runs",
            fixture.JobRun.Id.ToString("D"));
    }

    private static void AssertNoTemporaryKeyDirectory(Guid jobRunId)
    {
        var tempRoot = Path.Combine(Path.GetTempPath(), "nodecontrol-ssh");
        if (!Directory.Exists(tempRoot))
        {
            return;
        }

        Assert.Empty(Directory.GetDirectories(tempRoot, $"{jobRunId:D}-*"));
    }

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

    private sealed class FakeRemoteCommandRunner(params RemoteCommandResult[] results) : IRemoteCommandRunner
    {
        private readonly Queue<RemoteCommandResult> resultQueue = new(results);

        public List<RemoteCommandInvocation> Invocations { get; } = [];

        public Task<RemoteCommandResult> RunAsync(
            string fileName,
            IReadOnlyList<string> arguments,
            TimeSpan timeout,
            Func<CancellationToken, Task<bool>>? isCancellationRequested,
            Func<string, CancellationToken, Task>? onStdoutLine,
            Func<string, CancellationToken, Task>? onStderrLine,
            CancellationToken cancellationToken = default)
        {
            Invocations.Add(new RemoteCommandInvocation(fileName, arguments.ToArray()));
            return Task.FromResult(resultQueue.Count == 0
                ? new RemoteCommandResult(0, false, false, null)
                : resultQueue.Dequeue());
        }
    }

    private sealed record RemoteCommandInvocation(
        string FileName,
        string[] Arguments);

    private sealed class CancellingAnsibleRunner(JobRun jobRun, Guid userId) : IAnsiblePlaybookRunner
    {
        public bool ObservedCancellation { get; private set; }

        public async Task<AnsiblePlaybookRunResult> RunAsync(
            AnsiblePlaybookRunRequest request,
            CancellationToken cancellationToken = default)
        {
            jobRun.RequestCancellation(userId, "stop", TestTime);
            ObservedCancellation = request.IsCancellationRequested is not null
                && await request.IsCancellationRequested(cancellationToken);

            return new AnsiblePlaybookRunResult(
                130,
                false,
                ObservedCancellation,
                ObservedCancellation ? "ansible-playbook was cancelled." : "cancellation was not observed.");
        }
    }

    private sealed class PassthroughControlNodeDispatcher(IAnsiblePlaybookRunner runner) : IControlNodeDispatcher
    {
        public async Task<ControlNodeDispatchResult> DispatchAsync(
            ControlNodeDispatchRequest request,
            CancellationToken cancellationToken = default)
        {
            var result = await runner.RunAsync(
                new AnsiblePlaybookRunRequest(
                    request.Workspace.WorkspacePath,
                    request.Workspace.PlaybookFileName,
                    request.Workspace.VariableFileName,
                    request.Workspace.StdoutLogPath,
                    request.Workspace.StderrLogPath,
                    request.Timeout,
                    request.OnStdoutLine,
                    request.OnStderrLine,
                    request.IsCancellationRequested),
                cancellationToken);

            return new ControlNodeDispatchResult(result.ExitCode, result.TimedOut, result.Cancelled, result.ErrorMessage);
        }
    }

    private sealed class CapturingControlNodeDispatcher(ControlNodeDispatchResult result) : IControlNodeDispatcher
    {
        public List<ControlNodeDispatchRequest> Requests { get; } = [];

        public Task<ControlNodeDispatchResult> DispatchAsync(
            ControlNodeDispatchRequest request,
            CancellationToken cancellationToken = default)
        {
            Requests.Add(request);
            return Task.FromResult(result);
        }
    }

    private sealed class InspectingControlNodeDispatcher(Func<ControlNodeDispatchRequest, ControlNodeDispatchResult> handler) : IControlNodeDispatcher
    {
        public Task<ControlNodeDispatchResult> DispatchAsync(
            ControlNodeDispatchRequest request,
            CancellationToken cancellationToken = default)
        {
            return Task.FromResult(handler(request));
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
            IReadOnlyList<JobRunTemplateArtifact> templateArtifacts,
            IReadOnlyDictionary<string, string> secretValuesBySlug,
            CancellationToken cancellationToken = default)
        {
            return Task.FromResult(JobRunWorkspaceBuildResult.Failed(errorMessage));
        }
    }

    private sealed class FakeSecretProtector : ISecretProtector
    {
        public string Protect(string plaintext)
        {
            return $"protected:{plaintext}";
        }

        public string Unprotect(string protectedValue)
        {
            return protectedValue.StartsWith("protected:", StringComparison.Ordinal)
                ? protectedValue["protected:".Length..]
                : protectedValue;
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
            Template? template,
            Secret? secret,
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
            Template = template;
            Secret = secret;
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

        public Template? Template { get; }

        public Secret? Secret { get; }

        public Job Job { get; }

        public JobRun JobRun { get; }

        public static ExecutionFixture Create(
            bool includeVariableSet = true,
            VariableSetFormat variableFormat = VariableSetFormat.Yaml,
            string variableContent = "app_name: nodecontrol\n",
            bool addJobRun = true,
            bool artifactPlaybook = false,
            string? templateArtifactPath = null,
            string? templateContent = null,
            string? protectedSecretValue = null,
            string controlNodeHostname = "control.local")
        {
            var workspaceRoot = Path.Combine(Path.GetTempPath(), "nodecontrol-tests", Guid.NewGuid().ToString("N"));
            var db = new NodeControlTestDbContext();
            var user = User.Create("Operator", "operator@nodecontrol.local", false, TestTime);
            var customer = Customer.Create("Customer A", "customer-a", null, TestTime);
            var controlNode = ControlNode.Create(customer.Id, "control", controlNodeHostname, 22, null, TestTime);
            var inventoryGroup = InventoryGroup.Create(customer.Id, "web", null, TestTime);
            var managedNode = ManagedNode.Create(customer.Id, "web-01", "10.0.0.10", 22, "Linux", "prod", null, TestTime);
            var playbook = artifactPlaybook
                ? Playbook.Create(
                    customer.Id,
                    "Deploy",
                    "deploy",
                    null,
                    PlaybookSourceType.ArtifactDirectory,
                    null,
                    "site.yml",
                    TestTime,
                    "[{\"path\":\"site.yml\",\"content\":\"- hosts: all\\n  roles:\\n    - app\\n\"},{\"path\":\"roles/app/tasks/main.yml\",\"content\":\"- debug:\\n    msg: hello\\n\"}]")
                : Playbook.Create(
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
            var template = templateArtifactPath is not null
                ? Template.Create(
                    customer.Id,
                    "App config",
                    "app-config",
                    null,
                    TemplateType.ConfigFile,
                    templateContent ?? "enabled=true\n",
                    "ini",
                    TestTime)
                : null;
            var secret = protectedSecretValue is not null
                ? Secret.Create(
                    customer.Id,
                    "API Token",
                    "api-token",
                    null,
                    SecretKind.ApiToken,
                    protectedSecretValue,
                    TestTime)
                : null;
            var templateArtifactsJson = template is null
                ? null
                : $"[{{\"TemplateId\":\"{template.Id:D}\",\"Path\":\"{templateArtifactPath}\"}}]";
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
                TestTime,
                templateArtifactsJson);
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
            if (template is not null)
            {
                db.AddTemplate(template);
            }
            if (secret is not null)
            {
                db.AddSecret(secret);
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
                template,
                secret,
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
            return CreateExecutionService(new PassthroughControlNodeDispatcher(runner), workspaceBuilder);
        }

        public JobRunExecutionService CreateExecutionService(
            IControlNodeDispatcher dispatcher,
            IJobRunWorkspaceBuilder? workspaceBuilder = null)
        {
            return new JobRunExecutionService(
                Db,
                workspaceBuilder ?? CreateWorkspaceBuilder(),
                dispatcher,
                new SecretReferenceParser(),
                new FakeSecretProtector(),
                new TestClock());
        }

        public ServiceProvider CreateWorkerServiceProvider(IAnsiblePlaybookRunner runner)
        {
            var services = new ServiceCollection();
            services.AddSingleton<INodeControlDbContext>(Db);
            services.AddSingleton<IClock>(new TestClock());
            services.AddSingleton<IJobRunWorkspaceBuilder>(CreateWorkspaceBuilder());
            services.AddSingleton(new SecretReferenceParser());
            services.AddSingleton<ISecretProtector>(new FakeSecretProtector());
            services.AddSingleton(runner);
            services.AddSingleton<IControlNodeDispatcher>(new PassthroughControlNodeDispatcher(runner));
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
