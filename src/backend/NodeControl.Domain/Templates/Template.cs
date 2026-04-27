using NodeControl.Domain.Playbooks;

namespace NodeControl.Domain.Templates;

public sealed class Template
{
    private Template()
    {
    }

    private Template(
        Guid id,
        Guid customerId,
        string name,
        string slug,
        string? description,
        TemplateType templateType,
        string content,
        string? language,
        DateTimeOffset createdAt)
    {
        Id = id;
        CustomerId = customerId;
        Name = name.Trim();
        Slug = slug.Trim();
        Description = DefinitionValidation.NormalizeOptional(description, 1000, nameof(description));
        TemplateType = templateType;
        Content = content;
        Language = DefinitionValidation.NormalizeOptional(language, 100, nameof(language));
        Status = TemplateStatus.Active;
        CreatedAt = createdAt;
    }

    public Guid Id { get; private set; }

    public Guid CustomerId { get; private set; }

    public string Name { get; private set; } = string.Empty;

    public string Slug { get; private set; } = string.Empty;

    public string? Description { get; private set; }

    public TemplateType TemplateType { get; private set; }

    public string Content { get; private set; } = string.Empty;

    public string? Language { get; private set; }

    public TemplateStatus Status { get; private set; }

    public DateTimeOffset CreatedAt { get; private set; }

    public DateTimeOffset? UpdatedAt { get; private set; }

    public DateTimeOffset? ArchivedAt { get; private set; }

    public static Template Create(
        Guid customerId,
        string name,
        string slug,
        string? description,
        TemplateType templateType,
        string content,
        string? language,
        DateTimeOffset createdAt)
    {
        Validate(name, slug, content);
        return new Template(Guid.NewGuid(), customerId, name, slug, description, templateType, content, language, createdAt);
    }

    public void Update(
        string name,
        string slug,
        string? description,
        TemplateType templateType,
        string content,
        string? language,
        DateTimeOffset updatedAt)
    {
        Validate(name, slug, content);
        Name = name.Trim();
        Slug = slug.Trim();
        Description = DefinitionValidation.NormalizeOptional(description, 1000, nameof(description));
        TemplateType = templateType;
        Content = content;
        Language = DefinitionValidation.NormalizeOptional(language, 100, nameof(language));
        UpdatedAt = updatedAt;
    }

    public void Archive(DateTimeOffset archivedAt)
    {
        if (Status == TemplateStatus.Archived)
        {
            return;
        }

        Status = TemplateStatus.Archived;
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
