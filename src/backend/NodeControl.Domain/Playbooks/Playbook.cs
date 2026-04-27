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
        DateTimeOffset createdAt)
    {
        Id = id;
        CustomerId = customerId;
        Name = name.Trim();
        Slug = slug.Trim();
        Description = DefinitionValidation.NormalizeOptional(description, 1000, nameof(description));
        SourceType = sourceType;
        Status = PlaybookStatus.Active;
        InlineContent = inlineContent;
        EntryFilePath = DefinitionValidation.ValidateEntryFilePath(entryFilePath);
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
        DateTimeOffset createdAt)
    {
        Validate(name, slug, sourceType, inlineContent);
        return new Playbook(Guid.NewGuid(), customerId, name, slug, description, sourceType, inlineContent, entryFilePath, createdAt);
    }

    public void Update(
        string name,
        string slug,
        string? description,
        PlaybookSourceType sourceType,
        string? inlineContent,
        string? entryFilePath,
        DateTimeOffset updatedAt)
    {
        Validate(name, slug, sourceType, inlineContent);
        Name = name.Trim();
        Slug = slug.Trim();
        Description = DefinitionValidation.NormalizeOptional(description, 1000, nameof(description));
        SourceType = sourceType;
        InlineContent = inlineContent;
        EntryFilePath = DefinitionValidation.ValidateEntryFilePath(entryFilePath);
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
        string? inlineContent)
    {
        DefinitionValidation.ValidateName(name);
        DefinitionValidation.ValidateSlug(slug);
        if (sourceType == PlaybookSourceType.InlineYaml)
        {
            DefinitionValidation.ValidateContent(inlineContent ?? string.Empty, 200000, nameof(inlineContent));
        }
    }
}
