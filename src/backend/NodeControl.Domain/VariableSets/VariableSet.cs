using NodeControl.Domain.Playbooks;

namespace NodeControl.Domain.VariableSets;

public sealed class VariableSet
{
    private VariableSet()
    {
    }

    private VariableSet(
        Guid id,
        Guid customerId,
        string name,
        string slug,
        string? description,
        VariableSetFormat format,
        string content,
        bool containsSensitiveValues,
        DateTimeOffset createdAt)
    {
        Id = id;
        CustomerId = customerId;
        Name = name.Trim();
        Slug = slug.Trim();
        Description = DefinitionValidation.NormalizeOptional(description, 1000, nameof(description));
        Format = format;
        Content = content;
        ContainsSensitiveValues = containsSensitiveValues;
        Status = VariableSetStatus.Active;
        CreatedAt = createdAt;
    }

    public Guid Id { get; private set; }

    public Guid CustomerId { get; private set; }

    public string Name { get; private set; } = string.Empty;

    public string Slug { get; private set; } = string.Empty;

    public string? Description { get; private set; }

    public VariableSetFormat Format { get; private set; }

    public string Content { get; private set; } = string.Empty;

    public bool ContainsSensitiveValues { get; private set; }

    public VariableSetStatus Status { get; private set; }

    public DateTimeOffset CreatedAt { get; private set; }

    public DateTimeOffset? UpdatedAt { get; private set; }

    public DateTimeOffset? ArchivedAt { get; private set; }

    public static VariableSet Create(
        Guid customerId,
        string name,
        string slug,
        string? description,
        VariableSetFormat format,
        string content,
        bool containsSensitiveValues,
        DateTimeOffset createdAt)
    {
        Validate(name, slug, content);
        return new VariableSet(Guid.NewGuid(), customerId, name, slug, description, format, content, containsSensitiveValues, createdAt);
    }

    public void Update(
        string name,
        string slug,
        string? description,
        VariableSetFormat format,
        string content,
        bool containsSensitiveValues,
        DateTimeOffset updatedAt)
    {
        Validate(name, slug, content);
        Name = name.Trim();
        Slug = slug.Trim();
        Description = DefinitionValidation.NormalizeOptional(description, 1000, nameof(description));
        Format = format;
        Content = content;
        ContainsSensitiveValues = containsSensitiveValues;
        UpdatedAt = updatedAt;
    }

    public void Archive(DateTimeOffset archivedAt)
    {
        if (Status == VariableSetStatus.Archived)
        {
            return;
        }

        Status = VariableSetStatus.Archived;
        ArchivedAt = archivedAt;
        UpdatedAt = archivedAt;
    }

    private static void Validate(string name, string slug, string content)
    {
        DefinitionValidation.ValidateName(name);
        DefinitionValidation.ValidateSlug(slug);
        DefinitionValidation.ValidateContent(content, 200000, nameof(content));
    }
}
