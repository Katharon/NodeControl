namespace NodeControl.Domain.Playbooks;

public sealed class Playbook
{
    private Playbook()
    {
    }

    private Playbook(
        Guid id,
        Guid customerId,
        string name,
        string slug,
        string? description,
        PlaybookSourceType sourceType,
        string? inlineContent,
        string? entryFilePath,
        string? artifactFilesJson,
        DateTimeOffset createdAt)
    {
        Id = id;
        CustomerId = customerId;
        Name = name.Trim();
        Slug = slug.Trim();
        Description = DefinitionValidation.NormalizeOptional(description, 1000, nameof(description));
        SourceType = sourceType;
        Status = PlaybookStatus.Active;
        InlineContent = NormalizeInlineContent(sourceType, inlineContent);
        EntryFilePath = DefinitionValidation.ValidateEntryFilePath(entryFilePath);
        ArtifactFilesJson = NormalizeArtifactFilesJson(sourceType, artifactFilesJson);
        CreatedAt = createdAt;
    }

    public Guid Id { get; private set; }

    public Guid CustomerId { get; private set; }

    public string Name { get; private set; } = string.Empty;

    public string Slug { get; private set; } = string.Empty;

    public string? Description { get; private set; }

    public PlaybookSourceType SourceType { get; private set; }

    public PlaybookStatus Status { get; private set; }

    public string? InlineContent { get; private set; }

    public string? EntryFilePath { get; private set; }

    public string? ArtifactFilesJson { get; private set; }

    public DateTimeOffset CreatedAt { get; private set; }

    public DateTimeOffset? UpdatedAt { get; private set; }

    public DateTimeOffset? ArchivedAt { get; private set; }

    public static Playbook Create(
        Guid customerId,
        string name,
        string slug,
        string? description,
        PlaybookSourceType sourceType,
        string? inlineContent,
        string? entryFilePath,
        DateTimeOffset createdAt,
        string? artifactFilesJson = null)
    {
        Validate(name, slug, sourceType, inlineContent, artifactFilesJson);
        return new Playbook(
            Guid.NewGuid(),
            customerId,
            name,
            slug,
            description,
            sourceType,
            inlineContent,
            entryFilePath,
            artifactFilesJson,
            createdAt);
    }

    public void Update(
        string name,
        string slug,
        string? description,
        PlaybookSourceType sourceType,
        string? inlineContent,
        string? entryFilePath,
        DateTimeOffset updatedAt,
        string? artifactFilesJson = null)
    {
        Validate(name, slug, sourceType, inlineContent, artifactFilesJson);
        Name = name.Trim();
        Slug = slug.Trim();
        Description = DefinitionValidation.NormalizeOptional(description, 1000, nameof(description));
        SourceType = sourceType;
        InlineContent = NormalizeInlineContent(sourceType, inlineContent);
        EntryFilePath = DefinitionValidation.ValidateEntryFilePath(entryFilePath);
        ArtifactFilesJson = NormalizeArtifactFilesJson(sourceType, artifactFilesJson);
        UpdatedAt = updatedAt;
    }

    public void Archive(DateTimeOffset archivedAt)
    {
        if (Status == PlaybookStatus.Archived)
        {
            return;
        }

        Status = PlaybookStatus.Archived;
        ArchivedAt = archivedAt;
        UpdatedAt = archivedAt;
    }

    private static void Validate(
        string name,
        string slug,
        PlaybookSourceType sourceType,
        string? inlineContent,
        string? artifactFilesJson)
    {
        DefinitionValidation.ValidateName(name);
        DefinitionValidation.ValidateSlug(slug);
        if (sourceType == PlaybookSourceType.InlineYaml)
        {
            DefinitionValidation.ValidateContent(inlineContent ?? string.Empty, 200000, nameof(inlineContent));
            if (!string.IsNullOrWhiteSpace(artifactFilesJson))
            {
                throw new ArgumentException("Inline playbooks cannot contain artifact files.", nameof(artifactFilesJson));
            }
        }
        else if (sourceType == PlaybookSourceType.ArtifactDirectory)
        {
            DefinitionValidation.ValidateContent(artifactFilesJson ?? string.Empty, 1000000, nameof(artifactFilesJson));
            if (!string.IsNullOrWhiteSpace(inlineContent))
            {
                throw new ArgumentException("Artifact-directory playbooks cannot contain inline YAML.", nameof(inlineContent));
            }
        }
        else
        {
            throw new ArgumentException("Playbook source type is not supported.", nameof(sourceType));
        }
    }

    private static string? NormalizeInlineContent(PlaybookSourceType sourceType, string? inlineContent)
    {
        return sourceType == PlaybookSourceType.InlineYaml ? inlineContent : null;
    }

    private static string? NormalizeArtifactFilesJson(PlaybookSourceType sourceType, string? artifactFilesJson)
    {
        return sourceType == PlaybookSourceType.ArtifactDirectory ? artifactFilesJson : null;
    }
}
