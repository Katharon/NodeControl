using NodeControl.Domain.Inventories;
using NodeControl.Domain.Jobs;
using NodeControl.Domain.Nodes;
using NodeControl.Domain.Playbooks;
using NodeControl.Domain.Templates;
using NodeControl.Domain.VariableSets;

namespace NodeControl.Application.Abstractions.Execution;

public interface IJobRunWorkspaceBuilder
{
    Task<JobRunWorkspaceBuildResult> BuildAsync(
        JobRun jobRun,
        Job job,
        ControlNode controlNode,
        InventoryGroup inventoryGroup,
        IReadOnlyList<ManagedNode> managedNodes,
        IReadOnlyDictionary<Guid, ManagedNode> jumpHostsByNodeId,
        Playbook playbook,
        VariableSet? variableSet,
        IReadOnlyList<JobRunTemplateArtifact> templateArtifacts,
        IReadOnlyDictionary<string, string> secretValuesBySlug,
        CancellationToken cancellationToken = default);
}

public sealed record JobRunTemplateArtifact(
    Template Template,
    string Path);

public sealed record JobRunWorkspace(
    string WorkspacePath,
    string ControlHostWorkspacePath,
    string InventoryPath,
    string VariablePath,
    string VariableFileName,
    string PlaybookPath,
    string PlaybookFileName,
    string DispatchManifestPath,
    string StdoutLogPath,
    string StderrLogPath);

public sealed record JobRunWorkspaceBuildResult(
    bool Succeeded,
    JobRunWorkspace? Workspace,
    string? ErrorMessage)
{
    public static JobRunWorkspaceBuildResult Ok(JobRunWorkspace workspace)
    {
        return new JobRunWorkspaceBuildResult(true, workspace, null);
    }

    public static JobRunWorkspaceBuildResult Failed(string errorMessage)
    {
        return new JobRunWorkspaceBuildResult(false, null, errorMessage);
    }
}
