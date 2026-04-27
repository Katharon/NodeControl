using YamlDotNet.Serialization;
using NodeControl.Application.Abstractions.Execution;
using NodeControl.Domain.Inventories;
using NodeControl.Domain.Jobs;
using NodeControl.Domain.Nodes;
using NodeControl.Domain.Playbooks;
using NodeControl.Domain.VariableSets;

namespace NodeControl.Application.JobRuns;

public sealed class JobRunWorkspaceBuilder(string runWorkspaceRoot) : IJobRunWorkspaceBuilder
{
    private readonly ISerializer yamlSerializer = new SerializerBuilder().Build();
    private readonly string runWorkspaceRoot = string.IsNullOrWhiteSpace(runWorkspaceRoot)
        ? "/var/lib/nodecontrol/runs"
        : runWorkspaceRoot;

    public async Task<JobRunWorkspaceBuildResult> BuildAsync(
        JobRun jobRun,
        Job job,
        ControlNode controlNode,
        InventoryGroup inventoryGroup,
        IReadOnlyList<ManagedNode> managedNodes,
        Playbook playbook,
        VariableSet? variableSet,
        CancellationToken cancellationToken = default)
    {
        if (job.CustomerId != jobRun.CustomerId
            || controlNode.CustomerId != jobRun.CustomerId
            || inventoryGroup.CustomerId != jobRun.CustomerId
            || playbook.CustomerId != jobRun.CustomerId
            || variableSet is not null && variableSet.CustomerId != jobRun.CustomerId)
        {
            return JobRunWorkspaceBuildResult.Failed("JobRun references resources outside its customer.");
        }

        if (managedNodes.Any(managedNode => managedNode.CustomerId != jobRun.CustomerId))
        {
            return JobRunWorkspaceBuildResult.Failed("Inventory group contains managed nodes outside the JobRun customer.");
        }

        if (managedNodes.Count == 0)
        {
            return JobRunWorkspaceBuildResult.Failed("Inventory group has no active managed nodes.");
        }

        if (playbook.SourceType != PlaybookSourceType.InlineYaml)
        {
            return JobRunWorkspaceBuildResult.Failed("Only inline YAML playbooks are supported for execution.");
        }

        if (string.IsNullOrWhiteSpace(playbook.InlineContent))
        {
            return JobRunWorkspaceBuildResult.Failed("Inline playbook content is required for execution.");
        }

        var rootPath = Path.GetFullPath(runWorkspaceRoot);
        var workspacePath = Path.GetFullPath(Path.Combine(rootPath, jobRun.Id.ToString("D")));
        if (!workspacePath.StartsWith(rootPath, StringComparison.Ordinal))
        {
            return JobRunWorkspaceBuildResult.Failed("Execution workspace path is invalid.");
        }

        var playbookDirectory = Path.Combine(workspacePath, "playbook");
        var inventoryPath = Path.Combine(workspacePath, "inventory.yml");
        var variableFileName = variableSet?.Format == VariableSetFormat.Json ? "vars.json" : "vars.yml";
        var variablePath = Path.Combine(workspacePath, variableFileName);
        var playbookPath = Path.Combine(playbookDirectory, "site.yml");
        var stdoutLogPath = Path.Combine(workspacePath, "stdout.log");
        var stderrLogPath = Path.Combine(workspacePath, "stderr.log");

        Directory.CreateDirectory(playbookDirectory);

        await File.WriteAllTextAsync(inventoryPath, BuildInventoryYaml(inventoryGroup, managedNodes), cancellationToken);
        await File.WriteAllTextAsync(variablePath, variableSet is null ? "{}" : variableSet.Content, cancellationToken);
        await File.WriteAllTextAsync(playbookPath, playbook.InlineContent, cancellationToken);
        await File.WriteAllTextAsync(stdoutLogPath, string.Empty, cancellationToken);
        await File.WriteAllTextAsync(stderrLogPath, string.Empty, cancellationToken);

        return JobRunWorkspaceBuildResult.Ok(new JobRunWorkspace(
            workspacePath,
            inventoryPath,
            variablePath,
            variableFileName,
            playbookPath,
            stdoutLogPath,
            stderrLogPath));
    }

    private string BuildInventoryYaml(InventoryGroup inventoryGroup, IReadOnlyList<ManagedNode> managedNodes)
    {
        var hosts = managedNodes
            .OrderBy(managedNode => managedNode.Name, StringComparer.Ordinal)
            .ToDictionary(
                managedNode => managedNode.Name,
                managedNode => (object)new Dictionary<string, object>
                {
                    ["ansible_host"] = managedNode.Hostname,
                    ["ansible_port"] = managedNode.SshPort
                },
                StringComparer.Ordinal);

        var inventory = new Dictionary<string, object>
        {
            ["all"] = new Dictionary<string, object>
            {
                ["children"] = new Dictionary<string, object>
                {
                    [inventoryGroup.Name] = new Dictionary<string, object>
                    {
                        ["hosts"] = hosts
                    }
                }
            }
        };

        return yamlSerializer.Serialize(inventory);
    }
}
