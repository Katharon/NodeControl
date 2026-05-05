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

        var recentLogs = new RecentLogBuffer();

        try
        {
            await AppendLogAsync(jobRun, JobRunLogStream.System, JobRunLogLevel.Info, "Loading execution inputs.", cancellationToken);
            var executionContext = await LoadExecutionContextAsync(jobRun, cancellationToken);
            if (!executionContext.Succeeded)
            {
                await AppendLogAsync(jobRun, JobRunLogStream.System, JobRunLogLevel.Error, executionContext.ErrorMessage!, cancellationToken);
                await ApplyFailureDiagnosticAsync(
                    jobRun,
                    JobRunFailurePhase.ExecutionInput,
                    executionContext.ErrorMessage!,
                    null,
                    recentLogs,
                    null,
                    markTimedOut: false,
                    cancellationToken);
                await dbContext.SaveChangesAsync(cancellationToken);
                return;
            }

            var sensitiveValues = SensitiveValues(executionContext).ToArray();
            await AppendLogAsync(jobRun, JobRunLogStream.System, JobRunLogLevel.Info, "Creating execution workspace.", cancellationToken);
            var buildResult = await workspaceBuilder.BuildAsync(
                jobRun,
                executionContext.Job!,
                executionContext.ControlNode!,
                executionContext.InventoryGroup!,
                executionContext.ManagedNodes!,
                executionContext.JumpHostsByNodeId!,
                executionContext.Playbook!,
                executionContext.VariableSet,
                executionContext.TemplateArtifacts!,
                executionContext.SecretValuesBySlug!,
                cancellationToken);

            if (!buildResult.Succeeded)
            {
                var errorMessage = buildResult.ErrorMessage ?? "Execution workspace could not be created.";
                await AppendLogAsync(jobRun, JobRunLogStream.System, JobRunLogLevel.Error, errorMessage, cancellationToken);
                await ApplyFailureDiagnosticAsync(
                    jobRun,
                    JobRunFailurePhase.Workspace,
                    errorMessage,
                    null,
                    recentLogs,
                    sensitiveValues,
                    markTimedOut: false,
                    cancellationToken);
                await dbContext.SaveChangesAsync(cancellationToken);
                return;
            }

            var workspace = buildResult.Workspace!;
            var credentialMaterialization = await MaterializeManagedNodeCredentialsAsync(
                workspace,
                executionContext.ManagedNodes!.Concat(executionContext.JumpHostsByNodeId!.Values).DistinctBy(managedNode => managedNode.Id).ToArray(),
                executionContext.ManagedNodeSshPrivateKeysByNodeId!,
                cancellationToken);
            if (!credentialMaterialization.Succeeded)
            {
                var errorMessage = credentialMaterialization.ErrorMessage ?? "Managed host SSH credentials could not be materialized.";
                await AppendLogAsync(jobRun, JobRunLogStream.System, JobRunLogLevel.Error, errorMessage, cancellationToken, sensitiveValues);
                await ApplyFailureDiagnosticAsync(
                    jobRun,
                    JobRunFailurePhase.CredentialMaterialization,
                    errorMessage,
                    null,
                    recentLogs,
                    sensitiveValues,
                    markTimedOut: false,
                    cancellationToken);
                await dbContext.SaveChangesAsync(cancellationToken);
                return;
            }

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
                    (line, token) => AppendBufferedLogAsync(jobRun, recentLogs, JobRunLogStream.System, JobRunLogLevel.Info, line, token, sensitiveValues),
                    (line, token) => AppendBufferedLogAsync(jobRun, recentLogs, JobRunLogStream.StdOut, JobRunLogLevel.Info, line, token, sensitiveValues),
                    (line, token) => AppendBufferedLogAsync(jobRun, recentLogs, JobRunLogStream.StdErr, JobRunLogLevel.Error, line, token, sensitiveValues),
                    token => dbContext.IsJobRunCancellationRequestedAsync(jobRun.Id, token)),
                cancellationToken);

            if (runResult.Cancelled || await dbContext.IsJobRunCancellationRequestedAsync(jobRun.Id, cancellationToken))
            {
                var errorMessage = runResult.ErrorMessage ?? "JobRun was cancelled.";
                await AppendLogAsync(jobRun, JobRunLogStream.System, JobRunLogLevel.Warning, "Cancellation observed by worker.", cancellationToken);
                await AppendLogAsync(jobRun, JobRunLogStream.System, JobRunLogLevel.Warning, "Control-node execution terminated because cancellation was requested.", cancellationToken);
                var diagnostic = JobRunFailureDiagnostics.Classify(
                    JobRunFailurePhase.Cancellation,
                    RedactForStorage(errorMessage, sensitiveValues),
                    runResult.ExitCode,
                    recentLogs.Snapshot());
                jobRun.MarkCancelled(runResult.ExitCode, diagnostic.ToErrorMessage(), clock.UtcNow);
                await AppendLogAsync(jobRun, JobRunLogStream.System, JobRunLogLevel.Warning, "JobRun cancelled.", cancellationToken);
            }
            else if (runResult.TimedOut)
            {
                var errorMessage = runResult.ErrorMessage ?? "Control-node execution timed out.";
                await AppendLogAsync(jobRun, JobRunLogStream.System, JobRunLogLevel.Error, errorMessage, cancellationToken, sensitiveValues);
                await ApplyFailureDiagnosticAsync(
                    jobRun,
                    JobRunFailurePhase.Timeout,
                    errorMessage,
                    runResult.ExitCode,
                    recentLogs,
                    sensitiveValues,
                    markTimedOut: true,
                    cancellationToken);
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
                await AppendLogAsync(jobRun, JobRunLogStream.System, JobRunLogLevel.Error, errorMessage, cancellationToken, sensitiveValues);
                await ApplyFailureDiagnosticAsync(
                    jobRun,
                    DetermineRunFailurePhase(runResult, recentLogs),
                    errorMessage,
                    runResult.ExitCode,
                    recentLogs,
                    sensitiveValues,
                    markTimedOut: false,
                    cancellationToken);
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
            await ApplyFailureDiagnosticAsync(
                jobRun,
                JobRunFailurePhase.Unhandled,
                $"Execution setup failed: {exception.Message}",
                null,
                recentLogs,
                null,
                markTimedOut: false,
                CancellationToken.None);
            await dbContext.SaveChangesAsync(CancellationToken.None);
        }
    }

    private async Task AppendBufferedLogAsync(
        JobRun jobRun,
        RecentLogBuffer recentLogs,
        JobRunLogStream stream,
        JobRunLogLevel level,
        string message,
        CancellationToken cancellationToken,
        IEnumerable<string>? secretValues)
    {
        recentLogs.Add(RedactForStorage(message, secretValues));
        await AppendLogAsync(jobRun, stream, level, message, cancellationToken, secretValues);
    }

    private async Task ApplyFailureDiagnosticAsync(
        JobRun jobRun,
        JobRunFailurePhase phase,
        string errorMessage,
        int? exitCode,
        RecentLogBuffer recentLogs,
        IEnumerable<string>? secretValues,
        bool markTimedOut,
        CancellationToken cancellationToken)
    {
        var safeErrorMessage = RedactForStorage(errorMessage, secretValues);
        var diagnostic = JobRunFailureDiagnostics.Classify(
            phase,
            safeErrorMessage,
            exitCode,
            recentLogs.Snapshot());

        await AppendLogAsync(
            jobRun,
            JobRunLogStream.System,
            JobRunLogLevel.Error,
            diagnostic.ToLogMessage(),
            cancellationToken,
            secretValues);

        var storedMessage = RedactForStorage(diagnostic.ToErrorMessage(), secretValues);
        if (markTimedOut)
        {
            jobRun.MarkTimedOut(exitCode, storedMessage, clock.UtcNow);
        }
        else
        {
            jobRun.MarkFailed(exitCode, storedMessage, clock.UtcNow);
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

        var jumpHosts = await ResolveJumpHostsAsync(jobRun.CustomerId, managedNodes, cancellationToken);
        if (!jumpHosts.Succeeded)
        {
            return JobRunExecutionContext.Failed(jumpHosts.ErrorMessage!);
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

        var credentialNodes = managedNodes
            .Concat(jumpHosts.JumpHostsByNodeId!.Values)
            .DistinctBy(managedNode => managedNode.Id)
            .ToArray();
        var managedNodeCredentials = await ResolveManagedNodeCredentialsAsync(jobRun.CustomerId, credentialNodes, cancellationToken);
        if (!managedNodeCredentials.Succeeded)
        {
            return JobRunExecutionContext.Failed(managedNodeCredentials.ErrorMessage!);
        }

        return JobRunExecutionContext.Ok(
            job,
            controlNode,
            inventoryGroup,
            managedNodes,
            jumpHosts.JumpHostsByNodeId!,
            playbook,
            variableSet,
            templateArtifacts.Artifacts!,
            secretValues.SecretValuesBySlug!,
            controlNodeCredential.SshPrivateKey,
            managedNodeCredentials.SshPrivateKeysByNodeId!);
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

    private async Task<JumpHostResolutionResult> ResolveJumpHostsAsync(
        Guid customerId,
        IReadOnlyList<ManagedNode> managedNodes,
        CancellationToken cancellationToken)
    {
        var jumpHostIds = managedNodes
            .Where(managedNode => managedNode.JumpHostManagedNodeId is not null)
            .Select(managedNode => managedNode.JumpHostManagedNodeId!.Value)
            .Distinct()
            .ToArray();
        if (jumpHostIds.Length == 0)
        {
            return JumpHostResolutionResult.Ok(new Dictionary<Guid, ManagedNode>());
        }

        var activeNodes = await dbContext.ListActiveManagedNodesAsync(customerId, cancellationToken);
        var activeNodesById = activeNodes.ToDictionary(managedNode => managedNode.Id);
        var jumpHostsByNodeId = new Dictionary<Guid, ManagedNode>();
        foreach (var managedNode in managedNodes.OrderBy(managedNode => managedNode.Name, StringComparer.Ordinal))
        {
            if (managedNode.JumpHostManagedNodeId is null)
            {
                continue;
            }

            if (managedNode.JumpHostManagedNodeId.Value == managedNode.Id)
            {
                return JumpHostResolutionResult.Failed($"Managed host '{managedNode.Name}' cannot use itself as a jump host.");
            }

            if (!activeNodesById.TryGetValue(managedNode.JumpHostManagedNodeId.Value, out var jumpHost)
                || jumpHost.CustomerId != customerId)
            {
                return JumpHostResolutionResult.Failed($"Managed host '{managedNode.Name}' references an unavailable jump host.");
            }

            if (jumpHost.JumpHostManagedNodeId is not null)
            {
                return JumpHostResolutionResult.Failed($"Managed host '{managedNode.Name}' references a jump host that is itself jump-routed.");
            }

            jumpHostsByNodeId[managedNode.Id] = jumpHost;
        }

        return JumpHostResolutionResult.Ok(jumpHostsByNodeId);
    }

    private async Task<ManagedNodeCredentialResolutionResult> ResolveManagedNodeCredentialsAsync(
        Guid customerId,
        IReadOnlyList<ManagedNode> managedNodes,
        CancellationToken cancellationToken)
    {
        var valuesByNodeId = new Dictionary<Guid, string>();
        var valuesBySecretId = new Dictionary<Guid, string>();

        foreach (var managedNode in managedNodes
            .Where(managedNode => managedNode.SshPrivateKeySecretId is not null)
            .OrderBy(managedNode => managedNode.Name, StringComparer.Ordinal))
        {
            var secretId = managedNode.SshPrivateKeySecretId!.Value;
            if (!valuesBySecretId.TryGetValue(secretId, out var privateKey))
            {
                var secret = await dbContext.FindSecretAsync(customerId, secretId, cancellationToken);
                if (secret?.Status != SecretStatus.Active || secret.CustomerId != customerId || secret.Kind != SecretKind.SshPrivateKey)
                {
                    return ManagedNodeCredentialResolutionResult.Failed(
                        $"Managed host '{managedNode.Name}' SSH private key secret is unavailable for execution.");
                }

                try
                {
                    privateKey = secretProtector.Unprotect(secret.ProtectedValue);
                }
                catch (Exception)
                {
                    return ManagedNodeCredentialResolutionResult.Failed(
                        $"Managed host '{managedNode.Name}' SSH private key secret could not be resolved for execution.");
                }

                valuesBySecretId[secretId] = privateKey;
            }

            valuesByNodeId[managedNode.Id] = privateKey;
        }

        return ManagedNodeCredentialResolutionResult.Ok(valuesByNodeId);
    }

    private static async Task<CredentialMaterializationResult> MaterializeManagedNodeCredentialsAsync(
        JobRunWorkspace workspace,
        IReadOnlyList<ManagedNode> managedNodes,
        IReadOnlyDictionary<Guid, string> sshPrivateKeysByNodeId,
        CancellationToken cancellationToken)
    {
        if (sshPrivateKeysByNodeId.Count == 0)
        {
            return CredentialMaterializationResult.Ok();
        }

        var keyDirectory = Path.Combine(workspace.WorkspacePath, ".nodecontrol", "managed-host-keys");
        try
        {
            Directory.CreateDirectory(keyDirectory);
            foreach (var managedNode in managedNodes
                .Where(managedNode => sshPrivateKeysByNodeId.ContainsKey(managedNode.Id))
                .OrderBy(managedNode => managedNode.Name, StringComparer.Ordinal))
            {
                var keyPath = Path.Combine(keyDirectory, $"{managedNode.Id:D}.key");
                await File.WriteAllTextAsync(keyPath, NormalizePrivateKey(sshPrivateKeysByNodeId[managedNode.Id]), cancellationToken);
                TrySetPrivateKeyPermissions(keyPath);
            }
        }
        catch (Exception exception) when (exception is IOException or UnauthorizedAccessException)
        {
            TryDeleteDirectory(keyDirectory);
            return CredentialMaterializationResult.Failed($"Managed host SSH credentials could not be written: {exception.Message}");
        }

        return CredentialMaterializationResult.Ok();
    }

    private static void TryDeleteDirectory(string directory)
    {
        try
        {
            if (Directory.Exists(directory))
            {
                Directory.Delete(directory, recursive: true);
            }
        }
        catch (IOException)
        {
        }
        catch (UnauthorizedAccessException)
        {
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

        if (executionContext.ManagedNodeSshPrivateKeysByNodeId is not null)
        {
            foreach (var privateKey in executionContext.ManagedNodeSshPrivateKeysByNodeId.Values)
            {
                yield return privateKey;
            }
        }
    }

    private static JobRunFailurePhase DetermineRunFailurePhase(
        ControlNodeDispatchResult runResult,
        RecentLogBuffer recentLogs)
    {
        var text = string.Join('\n', recentLogs.Snapshot().Prepend(runResult.ErrorMessage ?? string.Empty))
            .ToLowerInvariant();

        if (text.Contains("ansible-playbook could not be started", StringComparison.Ordinal))
        {
            return JobRunFailurePhase.ProcessStart;
        }

        if (text.Contains("starting ansible-playbook on remote control node", StringComparison.Ordinal)
            || text.Contains("play recap", StringComparison.Ordinal)
            || text.Contains("task [", StringComparison.Ordinal)
            || text.Contains("fatal:", StringComparison.Ordinal)
            || runResult.ErrorMessage is null && runResult.ExitCode is not null)
        {
            return JobRunFailurePhase.PlaybookExecution;
        }

        return JobRunFailurePhase.Dispatch;
    }

    private static string NormalizePrivateKey(string privateKey)
    {
        return privateKey.EndsWith('\n') ? privateKey : privateKey + "\n";
    }

    private static void TrySetPrivateKeyPermissions(string keyPath)
    {
        try
        {
            if (OperatingSystem.IsLinux() || OperatingSystem.IsMacOS() || OperatingSystem.IsFreeBSD())
            {
                File.SetUnixFileMode(keyPath, UnixFileMode.UserRead | UnixFileMode.UserWrite);
            }
        }
        catch (PlatformNotSupportedException)
        {
        }
        catch (UnauthorizedAccessException)
        {
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

    private sealed record JumpHostResolutionResult(
        bool Succeeded,
        IReadOnlyDictionary<Guid, ManagedNode>? JumpHostsByNodeId,
        string? ErrorMessage)
    {
        public static JumpHostResolutionResult Ok(IReadOnlyDictionary<Guid, ManagedNode> jumpHostsByNodeId)
        {
            return new JumpHostResolutionResult(true, jumpHostsByNodeId, null);
        }

        public static JumpHostResolutionResult Failed(string errorMessage)
        {
            return new JumpHostResolutionResult(false, null, errorMessage);
        }
    }

    private sealed record ManagedNodeCredentialResolutionResult(
        bool Succeeded,
        IReadOnlyDictionary<Guid, string>? SshPrivateKeysByNodeId,
        string? ErrorMessage)
    {
        public static ManagedNodeCredentialResolutionResult Ok(IReadOnlyDictionary<Guid, string> sshPrivateKeysByNodeId)
        {
            return new ManagedNodeCredentialResolutionResult(true, sshPrivateKeysByNodeId, null);
        }

        public static ManagedNodeCredentialResolutionResult Failed(string errorMessage)
        {
            return new ManagedNodeCredentialResolutionResult(false, null, errorMessage);
        }
    }

    private sealed record CredentialMaterializationResult(
        bool Succeeded,
        string? ErrorMessage)
    {
        public static CredentialMaterializationResult Ok()
        {
            return new CredentialMaterializationResult(true, null);
        }

        public static CredentialMaterializationResult Failed(string errorMessage)
        {
            return new CredentialMaterializationResult(false, errorMessage);
        }
    }

    private sealed class RecentLogBuffer
    {
        private const int MaxLines = 80;
        private readonly Queue<string> lines = new();

        public void Add(string line)
        {
            if (string.IsNullOrWhiteSpace(line))
            {
                return;
            }

            lines.Enqueue(line);
            while (lines.Count > MaxLines)
            {
                lines.Dequeue();
            }
        }

        public IReadOnlyList<string> Snapshot()
        {
            return lines.ToArray();
        }
    }
}
