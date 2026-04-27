using NodeControl.Application.Abstractions.Execution;
using NodeControl.Application.Abstractions.Persistence;
using NodeControl.Application.Abstractions.Time;
using NodeControl.Domain.Jobs;
using NodeControl.Domain.Nodes;
using NodeControl.Domain.Playbooks;
using NodeControl.Domain.VariableSets;

namespace NodeControl.Application.JobRuns;

public sealed class JobRunExecutionService(
    INodeControlDbContext dbContext,
    IJobRunWorkspaceBuilder workspaceBuilder,
    IAnsiblePlaybookRunner ansibleRunner,
    IClock clock)
{
    public async Task<bool> ProcessOldestQueuedAsync(CancellationToken cancellationToken = default)
    {
        var jobRun = await dbContext.FindOldestQueuedJobRunAsync(cancellationToken);
        if (jobRun is null)
        {
            return false;
        }

        await ExecuteAsync(jobRun, cancellationToken);
        return true;
    }

    public async Task ExecuteAsync(JobRun jobRun, CancellationToken cancellationToken = default)
    {
        if (jobRun.Status != JobRunStatus.Queued)
        {
            return;
        }

        jobRun.MarkRunning(clock.UtcNow);
        await dbContext.SaveChangesAsync(cancellationToken);

        try
        {
            var executionContext = await LoadExecutionContextAsync(jobRun, cancellationToken);
            if (!executionContext.Succeeded)
            {
                jobRun.MarkFailed(null, executionContext.ErrorMessage!, clock.UtcNow);
                await dbContext.SaveChangesAsync(cancellationToken);
                return;
            }

            var buildResult = await workspaceBuilder.BuildAsync(
                jobRun,
                executionContext.Job!,
                executionContext.ControlNode!,
                executionContext.InventoryGroup!,
                executionContext.ManagedNodes!,
                executionContext.Playbook!,
                executionContext.VariableSet,
                cancellationToken);

            if (!buildResult.Succeeded)
            {
                jobRun.MarkFailed(null, buildResult.ErrorMessage ?? "Execution workspace could not be created.", clock.UtcNow);
                await dbContext.SaveChangesAsync(cancellationToken);
                return;
            }

            var workspace = buildResult.Workspace!;
            jobRun.SetExecutionPaths(workspace.WorkspacePath, workspace.StdoutLogPath, workspace.StderrLogPath);
            await dbContext.SaveChangesAsync(cancellationToken);

            var runResult = await ansibleRunner.RunAsync(
                new AnsiblePlaybookRunRequest(
                    workspace.WorkspacePath,
                    workspace.VariableFileName,
                    workspace.StdoutLogPath,
                    workspace.StderrLogPath,
                    TimeSpan.FromSeconds(executionContext.Job!.DefaultTimeoutSeconds)),
                cancellationToken);

            if (runResult.TimedOut)
            {
                jobRun.MarkTimedOut(runResult.ExitCode, runResult.ErrorMessage ?? "ansible-playbook execution timed out.", clock.UtcNow);
            }
            else if (runResult.ExitCode == 0)
            {
                jobRun.MarkSucceeded(runResult.ExitCode.Value, clock.UtcNow);
            }
            else
            {
                var errorMessage = runResult.ErrorMessage
                    ?? $"ansible-playbook exited with code {runResult.ExitCode?.ToString() ?? "unknown"}.";
                jobRun.MarkFailed(runResult.ExitCode, errorMessage, clock.UtcNow);
            }

            await dbContext.SaveChangesAsync(cancellationToken);
        }
        catch (OperationCanceledException) when (cancellationToken.IsCancellationRequested)
        {
            throw;
        }
        catch (Exception exception)
        {
            jobRun.MarkFailed(null, $"Execution setup failed: {exception.Message}", clock.UtcNow);
            await dbContext.SaveChangesAsync(CancellationToken.None);
        }
    }

    private async Task<JobRunExecutionContext> LoadExecutionContextAsync(
        JobRun jobRun,
        CancellationToken cancellationToken)
    {
        var job = await dbContext.FindJobAsync(jobRun.CustomerId, jobRun.JobId, cancellationToken);
        if (job is null)
        {
            return JobRunExecutionContext.Failed("JobRun references a job outside its customer or a missing job.");
        }

        if (job.CustomerId != jobRun.CustomerId)
        {
            return JobRunExecutionContext.Failed("JobRun and Job customer ids do not match.");
        }

        var controlNode = await dbContext.FindControlNodeAsync(jobRun.CustomerId, job.ControlNodeId, cancellationToken);
        if (controlNode?.Status != ControlNodeStatus.Active || controlNode.CustomerId != jobRun.CustomerId)
        {
            return JobRunExecutionContext.Failed("JobRun references an unavailable control node.");
        }

        var inventoryGroup = await dbContext.FindInventoryGroupAsync(jobRun.CustomerId, job.InventoryGroupId, cancellationToken);
        if (inventoryGroup is null || inventoryGroup.IsArchived || inventoryGroup.CustomerId != jobRun.CustomerId)
        {
            return JobRunExecutionContext.Failed("JobRun references an unavailable inventory group.");
        }

        var playbook = await dbContext.FindPlaybookAsync(jobRun.CustomerId, job.PlaybookId, cancellationToken);
        if (playbook?.Status != PlaybookStatus.Active || playbook.CustomerId != jobRun.CustomerId)
        {
            return JobRunExecutionContext.Failed("JobRun references an unavailable playbook.");
        }

        VariableSet? variableSet = null;
        if (job.VariableSetId is not null)
        {
            variableSet = await dbContext.FindVariableSetAsync(jobRun.CustomerId, job.VariableSetId.Value, cancellationToken);
            if (variableSet?.Status != VariableSetStatus.Active || variableSet.CustomerId != jobRun.CustomerId)
            {
                return JobRunExecutionContext.Failed("JobRun references an unavailable variable set.");
            }
        }

        var managedNodes = await dbContext.ListActiveManagedNodesForInventoryGroupAsync(inventoryGroup.Id, cancellationToken);
        if (managedNodes.Any(managedNode => managedNode.CustomerId != jobRun.CustomerId))
        {
            return JobRunExecutionContext.Failed("Inventory group contains managed nodes outside the JobRun customer.");
        }

        return JobRunExecutionContext.Ok(job, controlNode, inventoryGroup, managedNodes, playbook, variableSet);
    }
}
