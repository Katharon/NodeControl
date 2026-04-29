using System.Text.Json;
using YamlDotNet.Serialization;
using NodeControl.Application.Abstractions.Execution;
using NodeControl.Application.Playbooks;
using NodeControl.Domain.Inventories;
using NodeControl.Domain.Jobs;
using NodeControl.Domain.Nodes;
using NodeControl.Domain.Playbooks;
using NodeControl.Domain.Templates;
using NodeControl.Domain.VariableSets;

namespace NodeControl.Application.JobRuns;

public sealed class JobRunWorkspaceBuilder(string runWorkspaceRoot) : IJobRunWorkspaceBuilder
{
    private static readonly JsonSerializerOptions ArtifactJsonOptions = new(JsonSerializerDefaults.Web);
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
        IReadOnlyList<JobRunTemplateArtifact> templateArtifacts,
        IReadOnlyDictionary<string, string> secretValuesBySlug,
        CancellationToken cancellationToken = default)
    {
        if (job.CustomerId != jobRun.CustomerId
            || controlNode.CustomerId != jobRun.CustomerId
            || inventoryGroup.CustomerId != jobRun.CustomerId
            || playbook.CustomerId != jobRun.CustomerId
            || variableSet is not null && variableSet.CustomerId != jobRun.CustomerId
            || templateArtifacts.Any(templateArtifact => templateArtifact.Template.CustomerId != jobRun.CustomerId))
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

        if (playbook.SourceType == PlaybookSourceType.InlineYaml
            && string.IsNullOrWhiteSpace(playbook.InlineContent))
        {
            return JobRunWorkspaceBuildResult.Failed("Inline playbook content is required for execution.");
        }

        if (playbook.SourceType == PlaybookSourceType.ArtifactDirectory
            && string.IsNullOrWhiteSpace(playbook.ArtifactFilesJson))
        {
            return JobRunWorkspaceBuildResult.Failed("Artifact-directory playbook files are required for execution.");
        }

        if (playbook.SourceType is not (PlaybookSourceType.InlineYaml or PlaybookSourceType.ArtifactDirectory))
        {
            return JobRunWorkspaceBuildResult.Failed("Playbook source type is not supported for execution.");
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
        var playbookFileName = "playbook/site.yml";
        var playbookPath = Path.Combine(playbookDirectory, "site.yml");
        var stdoutLogPath = Path.Combine(workspacePath, "stdout.log");
        var stderrLogPath = Path.Combine(workspacePath, "stderr.log");

        Directory.CreateDirectory(playbookDirectory);

        await File.WriteAllTextAsync(inventoryPath, BuildInventoryYaml(inventoryGroup, managedNodes), cancellationToken);
        await File.WriteAllTextAsync(
            variablePath,
            variableSet is null ? "{}" : ResolveSecretReferences(variableSet.Content, secretValuesBySlug),
            cancellationToken);

        if (playbook.SourceType == PlaybookSourceType.InlineYaml)
        {
            await File.WriteAllTextAsync(playbookPath, playbook.InlineContent, cancellationToken);
        }
        else
        {
            var artifactResult = await WriteArtifactFilesAsync(playbook, playbookDirectory, cancellationToken);
            if (!artifactResult.Succeeded)
            {
                return JobRunWorkspaceBuildResult.Failed(artifactResult.ErrorMessage ?? "Artifact-directory playbook files could not be materialized.");
            }

            playbookPath = artifactResult.PlaybookPath!;
            playbookFileName = artifactResult.PlaybookFileName!;
        }

        var templateResult = await WriteTemplateArtifactsAsync(templateArtifacts, playbookDirectory, secretValuesBySlug, cancellationToken);
        if (!templateResult.Succeeded)
        {
            return JobRunWorkspaceBuildResult.Failed(templateResult.ErrorMessage ?? "Template artifacts could not be materialized.");
        }

        await File.WriteAllTextAsync(stdoutLogPath, string.Empty, cancellationToken);
        await File.WriteAllTextAsync(stderrLogPath, string.Empty, cancellationToken);

        return JobRunWorkspaceBuildResult.Ok(new JobRunWorkspace(
            workspacePath,
            inventoryPath,
            variablePath,
            variableFileName,
            playbookPath,
            playbookFileName,
            stdoutLogPath,
            stderrLogPath));
    }

    private async Task<ArtifactPlaybookWriteResult> WriteTemplateArtifactsAsync(
        IReadOnlyList<JobRunTemplateArtifact> templateArtifacts,
        string playbookDirectory,
        IReadOnlyDictionary<string, string> secretValuesBySlug,
        CancellationToken cancellationToken)
    {
        if (templateArtifacts.Count == 0)
        {
            return ArtifactPlaybookWriteResult.Ok(string.Empty, string.Empty);
        }

        var playbookRoot = Path.GetFullPath(playbookDirectory);
        var seenPaths = new HashSet<string>(StringComparer.Ordinal);
        foreach (var templateArtifact in templateArtifacts)
        {
            string relativePath;
            try
            {
                relativePath = NormalizeArtifactPath(templateArtifact.Path);
            }
            catch (ArgumentException exception)
            {
                return ArtifactPlaybookWriteResult.Failed(exception.Message);
            }

            if (!seenPaths.Add(relativePath))
            {
                return ArtifactPlaybookWriteResult.Failed("Template artifact path is duplicated.");
            }

            if (templateArtifact.Template.Status != TemplateStatus.Active)
            {
                return ArtifactPlaybookWriteResult.Failed("Template artifact references an unavailable template.");
            }

            var absolutePath = Path.GetFullPath(Path.Combine(playbookRoot, relativePath));
            if (!absolutePath.StartsWith(playbookRoot + Path.DirectorySeparatorChar, StringComparison.Ordinal))
            {
                return ArtifactPlaybookWriteResult.Failed("Template artifact path is invalid.");
            }

            if (File.Exists(absolutePath))
            {
                return ArtifactPlaybookWriteResult.Failed("Template artifact path conflicts with an existing playbook file.");
            }

            Directory.CreateDirectory(Path.GetDirectoryName(absolutePath)!);
            await File.WriteAllTextAsync(
                absolutePath,
                ResolveSecretReferences(templateArtifact.Template.Content, secretValuesBySlug),
                cancellationToken);
        }

        return ArtifactPlaybookWriteResult.Ok(string.Empty, string.Empty);
    }

    private static string ResolveSecretReferences(
        string content,
        IReadOnlyDictionary<string, string> secretValuesBySlug)
    {
        var resolved = content;
        foreach (var (slug, value) in secretValuesBySlug)
        {
            resolved = resolved.Replace($"secret://{slug}", value, StringComparison.Ordinal);
        }

        return resolved;
    }

    private async Task<ArtifactPlaybookWriteResult> WriteArtifactFilesAsync(
        Playbook playbook,
        string playbookDirectory,
        CancellationToken cancellationToken)
    {
        IReadOnlyList<PlaybookArtifactFileDto> artifactFiles;
        string entryFilePath;
        try
        {
            artifactFiles = DeserializeArtifactFiles(playbook.ArtifactFilesJson);
            entryFilePath = NormalizeArtifactPath(playbook.EntryFilePath);
        }
        catch (ArgumentException exception)
        {
            return ArtifactPlaybookWriteResult.Failed(exception.Message);
        }

        if (artifactFiles.Count == 0)
        {
            return ArtifactPlaybookWriteResult.Failed("Artifact-directory playbook files are required for execution.");
        }

        var playbookRoot = Path.GetFullPath(playbookDirectory);
        string? entryAbsolutePath = null;
        foreach (var artifactFile in artifactFiles)
        {
            var relativePath = NormalizeArtifactPath(artifactFile.Path);
            if (artifactFile.Content is null)
            {
                return ArtifactPlaybookWriteResult.Failed("Artifact file content is required.");
            }

            var absolutePath = Path.GetFullPath(Path.Combine(playbookRoot, relativePath));
            if (!absolutePath.StartsWith(playbookRoot + Path.DirectorySeparatorChar, StringComparison.Ordinal))
            {
                return ArtifactPlaybookWriteResult.Failed("Artifact file path is invalid.");
            }

            Directory.CreateDirectory(Path.GetDirectoryName(absolutePath)!);
            await File.WriteAllTextAsync(absolutePath, artifactFile.Content, cancellationToken);

            if (relativePath == entryFilePath)
            {
                entryAbsolutePath = absolutePath;
            }
        }

        if (entryAbsolutePath is null)
        {
            return ArtifactPlaybookWriteResult.Failed("Artifact entry file is missing.");
        }

        return ArtifactPlaybookWriteResult.Ok(entryAbsolutePath, $"playbook/{entryFilePath}");
    }

    private static IReadOnlyList<PlaybookArtifactFileDto> DeserializeArtifactFiles(string? artifactFilesJson)
    {
        if (string.IsNullOrWhiteSpace(artifactFilesJson))
        {
            return [];
        }

        return JsonSerializer.Deserialize<IReadOnlyList<PlaybookArtifactFileDto>>(artifactFilesJson, ArtifactJsonOptions) ?? [];
    }

    private static string NormalizeArtifactPath(string? path)
    {
        if (string.IsNullOrWhiteSpace(path))
        {
            throw new ArgumentException("Artifact file path is required.", nameof(path));
        }

        var normalized = path.Trim().Replace('\\', '/');
        if (normalized.Length > 500
            || normalized.StartsWith("/", StringComparison.Ordinal)
            || normalized.EndsWith("/", StringComparison.Ordinal)
            || Path.IsPathRooted(normalized)
            || normalized.Split('/').Any(part => string.IsNullOrWhiteSpace(part) || part == "." || part == ".."))
        {
            throw new ArgumentException("Artifact file path is invalid.", nameof(path));
        }

        return normalized;
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

    private sealed record ArtifactPlaybookWriteResult(
        bool Succeeded,
        string? PlaybookPath,
        string? PlaybookFileName,
        string? ErrorMessage)
    {
        public static ArtifactPlaybookWriteResult Ok(string playbookPath, string playbookFileName)
        {
            return new ArtifactPlaybookWriteResult(true, playbookPath, playbookFileName, null);
        }

        public static ArtifactPlaybookWriteResult Failed(string errorMessage)
        {
            return new ArtifactPlaybookWriteResult(false, null, null, errorMessage);
        }
    }
}
