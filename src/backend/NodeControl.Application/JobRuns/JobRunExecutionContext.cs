using NodeControl.Domain.Inventories;
using NodeControl.Domain.Jobs;
using NodeControl.Domain.Nodes;
using NodeControl.Domain.Playbooks;
using NodeControl.Domain.VariableSets;

namespace NodeControl.Application.JobRuns;

internal sealed record JobRunExecutionContext(
    bool Succeeded,
    Job? Job,
    ControlNode? ControlNode,
    InventoryGroup? InventoryGroup,
    IReadOnlyList<ManagedNode>? ManagedNodes,
    Playbook? Playbook,
    VariableSet? VariableSet,
    string? ErrorMessage)
{
    public static JobRunExecutionContext Ok(
        Job job,
        ControlNode controlNode,
        InventoryGroup inventoryGroup,
        IReadOnlyList<ManagedNode> managedNodes,
        Playbook playbook,
        VariableSet? variableSet)
    {
        return new JobRunExecutionContext(true, job, controlNode, inventoryGroup, managedNodes, playbook, variableSet, null);
    }

    public static JobRunExecutionContext Failed(string errorMessage)
    {
        return new JobRunExecutionContext(false, null, null, null, null, null, null, errorMessage);
    }
}
