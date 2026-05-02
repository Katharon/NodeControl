using NodeControl.Application.Abstractions.Execution;
using NodeControl.Application.Abstractions.Persistence;
using NodeControl.Application.Abstractions.Security;
using NodeControl.Application.Abstractions.Time;
using NodeControl.Application.Jobs;
using NodeControl.Application.Secrets;
using NodeControl.Domain.Jobs;
using NodeControl.Domain.Nodes;
using NodeControl.Domain.Playbooks;
using NodeControl.Domain.Secrets;
using NodeControl.Domain.Templates;
using NodeControl.Domain.VariableSets;

namespace NodeControl.Application.JobRuns;

public sealed class JobRunExecutionService(
    INodeControlDbContext dbContext,
    IJobRunWorkspaceBuilder workspaceBuilder,
    IControlNodeDispatcher controlNodeDispatcher,
    SecretReferenceParser secretReferenceParser,
    ISecretProtector secretProtector,
    IClock clock)
{
    private readonly SemaphoreSlim logWriteLock = new(1, 1);

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
        var currentStatus = await dbContext.GetJobRunStatusAsync(jobRun.Id, cancellationToken);
        if (jobRun.Status != JobRunStatus.Queued || currentStatus != JobRunStatus.Queued)
        {
            return;
        }

        jobRun.MarkRunning(clock.UtcNow);
        await dbContext.SaveChangesAsync(cancellationToken);
        await AppendLogAsync(jobRun, JobRunLogStream.System, JobRunLogLevel.Info, "JobRun processing started.", cancellationToken);

        try
        {
            await AppendLogAsync(jobRun, JobRunLogStream.System, JobRunLogLevel.Info, "Loading execution inputs.", cancellationToken);
            var executionContext = await LoadExecutionContextAsync(jobRun, cancellationToken);
            if (!executionContext.Succeeded)
            {
                await AppendLogAsync(jobRun, JobRunLogStream.System, JobRunLogLevel.Error, executionContext.ErrorMessage!, cancellationToken);
                jobRun.MarkFailed(null, executionContext.ErrorMessage!, clock.UtcNow);
                await dbContext.SaveChangesAsync(cancellationToken);
                return;
            }

            await AppendLogAsync(jobRun, JobRunLogStream.System, JobRunLogLevel.Info, "Creating execution workspace.", cancellationToken);
            var buildResult = await workspaceBuilder.BuildAsync(
                jobRun,
                executionContext.Job!,
                executionContext.ControlNode!,
                executionContext.InventoryGroup!,
                executionContext.ManagedNodes!,
                executionContext.Playbook!,
                executionContext.VariableSet,
                executionContext.TemplateArtifacts!,
                executionContext.SecretValuesBySlug!,
                cancellationToken);

            if (!buildResult.Succeeded)
            {
                var errorMessage = buildResult.ErrorMessage ?? "Execution workspace could not be created.";
                await AppendLogAsync(jobRun, JobRunLogStream.System, JobRunLogLevel.Error, errorMessage, cancellationToken);
                jobRun.MarkFailed(null, errorMessage, clock.UtcNow);
                await dbContext.SaveChangesAsync(cancellationToken);
                return;
            }

            var workspace = buildResult.Workspace!;
            jobRun.SetExecutionPaths(workspace.WorkspacePath, workspace.StdoutLogPath, workspace.StderrLogPath);
            await dbContext.SaveChangesAsync(cancellationToken);
            await AppendLogAsync(jobRun, JobRunLogStream.System, JobRunLogLevel.Info, "Execution workspace created.", cancellationToken);

            await AppendLogAsync(
                jobRun,
                JobRunLogStream.System,
                JobRunLogLevel.Info,
                $"Dispatching run to control node '{executionContext.ControlNode!.Name}'.",
                cancellationToken);
            var runResult = await controlNodeDispatcher.DispatchAsync(
                new ControlNodeDispatchRequest(
                    jobRun,
                    executionContext.ControlNode!,
                    workspace,
                    TimeSpan.FromSeconds(executionContext.Job!.DefaultTimeoutSeconds),
                    new ControlNodeCredentialMaterial(executionContext.ControlNodeSshPrivateKey),
                    (line, token) => AppendLogAsync(jobRun, JobRunLogStream.System, JobRunLogLevel.Info, line, token, SensitiveValues(executionContext)),
                    (line, token) => AppendLogAsync(jobRun, JobRunLogStream.StdOut, JobRunLogLevel.Info, line, token, SensitiveValues(executionContext)),
                    (line, token) => AppendLogAsync(jobRun, JobRunLogStream.StdErr, JobRunLogLevel.Error, line, token, SensitiveValues(executionContext)),
                    token => dbContext.IsJobRunCancellationRequestedAsync(jobRun.Id, token)),
                cancellationToken);

            if (runResult.Cancelled || await dbContext.IsJobRunCancellationRequestedAsync(jobRun.Id, cancellationToken))
            {
                var errorMessage = runResult.ErrorMessage ?? "JobRun was cancelled.";
                await AppendLogAsync(jobRun, JobRunLogStream.System, JobRunLogLevel.Warning, "Cancellation observed by worker.", cancellationToken);
                await AppendLogAsync(jobRun, JobRunLogStream.System, JobRunLogLevel.Warning, "Control-node execution terminated because cancellation was requested.", cancellationToken);
                jobRun.MarkCancelled(runResult.ExitCode, RedactForStorage(errorMessage, SensitiveValues(executionContext)), clock.UtcNow);
                await AppendLogAsync(jobRun, JobRunLogStream.System, JobRunLogLevel.Warning, "JobRun cancelled.", cancellationToken);
            }
            else if (runResult.TimedOut)
            {
                var errorMessage = runResult.ErrorMessage ?? "Control-node execution timed out.";
                await AppendLogAsync(jobRun, JobRunLogStream.System, JobRunLogLevel.Error, errorMessage, cancellationToken, SensitiveValues(executionContext));
                jobRun.MarkTimedOut(runResult.ExitCode, RedactForStorage(errorMessage, SensitiveValues(executionContext)), clock.UtcNow);
            }
            else if (runResult.ExitCode == 0)
            {
                await AppendLogAsync(jobRun, JobRunLogStream.System, JobRunLogLevel.Info, "Control-node execution exited with code 0.", cancellationToken);
                jobRun.MarkSucceeded(runResult.ExitCode.Value, clock.UtcNow);
            }
            else
            {
                var errorMessage = runResult.ErrorMessage
                    ?? $"Control-node execution exited with code {runResult.ExitCode?.ToString() ?? "unknown"}.";
                await AppendLogAsync(jobRun, JobRunLogStream.System, JobRunLogLevel.Error, errorMessage, cancellationToken, SensitiveValues(executionContext));
                jobRun.MarkFailed(runResult.ExitCode, RedactForStorage(errorMessage, SensitiveValues(executionContext)), clock.UtcNow);
            }

            await dbContext.SaveChangesAsync(cancellationToken);
        }
        catch (OperationCanceledException) when (cancellationToken.IsCancellationRequested)
        {
            throw;
        }
        catch (Exception exception)
        {
            await AppendLogAsync(jobRun, JobRunLogStream.System, JobRunLogLevel.Error, exception.Message, CancellationToken.None);
            jobRun.MarkFailed(null, $"Execution setup failed: {exception.Message}", clock.UtcNow);
            await dbContext.SaveChangesAsync(CancellationToken.None);
        }
    }

    private async Task AppendLogAsync(
        JobRun jobRun,
        JobRunLogStream stream,
        JobRunLogLevel level,
        string message,
        CancellationToken cancellationToken,
        IEnumerable<string>? secretValues = null)
    {
        if (string.IsNullOrWhiteSpace(message))
        {
            return;
        }

        await logWriteLock.WaitAsync(cancellationToken);
        try
        {
            var sequence = await dbContext.GetNextJobRunLogSequenceAsync(jobRun.Id, cancellationToken);
            dbContext.AddJobRunLogEntry(JobRunLogEntry.Create(
                jobRun,
                sequence,
                clock.UtcNow,
                stream,
                level,
                RedactForStorage(message, secretValues)));
            await dbContext.SaveChangesAsync(cancellationToken);
        }
        finally
        {
            logWriteLock.Release();
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

        var controlNode = await dbContext.FindControlNodeAsync(jobRun.CustomerId, jobRun.ControlNodeId, cancellationToken);
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

        var templateArtifacts = await LoadTemplateArtifactsAsync(jobRun.CustomerId, job.TemplateArtifactsJson, cancellationToken);
        if (!templateArtifacts.Succeeded)
        {
            return JobRunExecutionContext.Failed(templateArtifacts.ErrorMessage!);
        }

        var secretValues = await ResolveSecretReferencesAsync(jobRun.CustomerId, variableSet, templateArtifacts.Artifacts!, cancellationToken);
        if (!secretValues.Succeeded)
        {
            return JobRunExecutionContext.Failed(secretValues.ErrorMessage!);
        }

        var controlNodeCredential = await ResolveControlNodeCredentialAsync(jobRun.CustomerId, controlNode, cancellationToken);
        if (!controlNodeCredential.Succeeded)
        {
            return JobRunExecutionContext.Failed(controlNodeCredential.ErrorMessage!);
        }

        return JobRunExecutionContext.Ok(
            job,
            controlNode,
            inventoryGroup,
            managedNodes,
            playbook,
            variableSet,
            templateArtifacts.Artifacts!,
            secretValues.SecretValuesBySlug!,
            controlNodeCredential.SshPrivateKey);
    }

    private async Task<ControlNodeCredentialResolutionResult> ResolveControlNodeCredentialAsync(
        Guid customerId,
        ControlNode controlNode,
        CancellationToken cancellationToken)
    {
        if (controlNode.SshPrivateKeySecretId is null)
        {
            return ControlNodeCredentialResolutionResult.Ok(null);
        }

        var secret = await dbContext.FindSecretAsync(customerId, controlNode.SshPrivateKeySecretId.Value, cancellationToken);
        if (secret?.Status != SecretStatus.Active || secret.CustomerId != customerId || secret.Kind != SecretKind.SshPrivateKey)
        {
            return ControlNodeCredentialResolutionResult.Failed("Control node SSH private key secret is unavailable for execution.");
        }

        try
        {
            return ControlNodeCredentialResolutionResult.Ok(secretProtector.Unprotect(secret.ProtectedValue));
        }
        catch (Exception)
        {
            return ControlNodeCredentialResolutionResult.Failed("Control node SSH private key secret could not be resolved for execution.");
        }
    }

    private async Task<TemplateArtifactLoadResult> LoadTemplateArtifactsAsync(
        Guid customerId,
        string? templateArtifactsJson,
        CancellationToken cancellationToken)
    {
        var definitions = JobService.DeserializeTemplateArtifacts(templateArtifactsJson);
        if (definitions.Count == 0)
        {
            return TemplateArtifactLoadResult.Ok([]);
        }

        var artifacts = new List<JobRunTemplateArtifact>(definitions.Count);
        foreach (var definition in definitions)
        {
            var template = await dbContext.FindTemplateAsync(customerId, definition.TemplateId, cancellationToken);
            if (template?.Status != TemplateStatus.Active || template.CustomerId != customerId)
            {
                return TemplateArtifactLoadResult.Failed("JobRun references an unavailable template artifact.");
            }

            artifacts.Add(new JobRunTemplateArtifact(template, definition.Path));
        }

        return TemplateArtifactLoadResult.Ok(artifacts);
    }

    private async Task<SecretReferenceResolutionResult> ResolveSecretReferencesAsync(
        Guid customerId,
        VariableSet? variableSet,
        IReadOnlyList<JobRunTemplateArtifact> templateArtifacts,
        CancellationToken cancellationToken)
    {
        var slugs = new HashSet<string>(StringComparer.Ordinal);
        if (variableSet is not null)
        {
            foreach (var slug in secretReferenceParser.ParseDistinctSlugs(variableSet.Content))
            {
                slugs.Add(slug);
            }
        }

        foreach (var templateArtifact in templateArtifacts)
        {
            foreach (var slug in secretReferenceParser.ParseDistinctSlugs(templateArtifact.Template.Content))
            {
                slugs.Add(slug);
            }
        }

        if (slugs.Count == 0)
        {
            return SecretReferenceResolutionResult.Ok(new Dictionary<string, string>(StringComparer.Ordinal));
        }

        var secretValuesBySlug = new Dictionary<string, string>(StringComparer.Ordinal);
        foreach (var slug in slugs.Order(StringComparer.Ordinal))
        {
            var secret = await dbContext.FindSecretBySlugAsync(customerId, slug, cancellationToken);
            if (secret?.Status != SecretStatus.Active || secret.CustomerId != customerId)
            {
                return SecretReferenceResolutionResult.Failed($"Secret reference 'secret://{slug}' is unavailable for execution.");
            }

            try
            {
                secretValuesBySlug[slug] = secretProtector.Unprotect(secret.ProtectedValue);
            }
            catch (Exception)
            {
                return SecretReferenceResolutionResult.Failed($"Secret reference 'secret://{slug}' could not be resolved for execution.");
            }
        }

        return SecretReferenceResolutionResult.Ok(secretValuesBySlug);
    }

    private static string RedactForStorage(string value, IEnumerable<string>? secretValues)
    {
        var redacted = JobRunLogRedactor.Redact(value) ?? value;
        if (secretValues is null)
        {
            return redacted;
        }

        foreach (var secretValue in secretValues)
        {
            if (!string.IsNullOrEmpty(secretValue))
            {
                redacted = redacted.Replace(secretValue, "[REDACTED]", StringComparison.Ordinal);
            }
        }

        return redacted;
    }

    private static IEnumerable<string> SensitiveValues(JobRunExecutionContext executionContext)
    {
        if (executionContext.SecretValuesBySlug is not null)
        {
            foreach (var secretValue in executionContext.SecretValuesBySlug.Values)
            {
                yield return secretValue;
            }
        }

        if (!string.IsNullOrEmpty(executionContext.ControlNodeSshPrivateKey))
        {
            yield return executionContext.ControlNodeSshPrivateKey;
        }
    }

    private sealed record TemplateArtifactLoadResult(
        bool Succeeded,
        IReadOnlyList<JobRunTemplateArtifact>? Artifacts,
        string? ErrorMessage)
    {
        public static TemplateArtifactLoadResult Ok(IReadOnlyList<JobRunTemplateArtifact> artifacts)
        {
            return new TemplateArtifactLoadResult(true, artifacts, null);
        }

        public static TemplateArtifactLoadResult Failed(string errorMessage)
        {
            return new TemplateArtifactLoadResult(false, null, errorMessage);
        }
    }

    private sealed record SecretReferenceResolutionResult(
        bool Succeeded,
        IReadOnlyDictionary<string, string>? SecretValuesBySlug,
        string? ErrorMessage)
    {
        public static SecretReferenceResolutionResult Ok(IReadOnlyDictionary<string, string> secretValuesBySlug)
        {
            return new SecretReferenceResolutionResult(true, secretValuesBySlug, null);
        }

        public static SecretReferenceResolutionResult Failed(string errorMessage)
        {
            return new SecretReferenceResolutionResult(false, null, errorMessage);
        }
    }

    private sealed record ControlNodeCredentialResolutionResult(
        bool Succeeded,
        string? SshPrivateKey,
        string? ErrorMessage)
    {
        public static ControlNodeCredentialResolutionResult Ok(string? sshPrivateKey)
        {
            return new ControlNodeCredentialResolutionResult(true, sshPrivateKey, null);
        }

        public static ControlNodeCredentialResolutionResult Failed(string errorMessage)
        {
            return new ControlNodeCredentialResolutionResult(false, null, errorMessage);
        }
    }
}
