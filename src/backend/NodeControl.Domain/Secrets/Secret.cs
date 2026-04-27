using NodeControl.Domain.Playbooks;

namespace NodeControl.Domain.Secrets;

public sealed class Secret
{
    private Secret()
    {
    }

    private Secret(
        Guid id,
        Guid customerId,
        string name,
        string slug,
        string? description,
        SecretKind kind,
        string protectedValue,
        DateTimeOffset createdAt)
    {
        Id = id;
        CustomerId = customerId;
        Name = name.Trim();
        Slug = slug.Trim();
        Description = DefinitionValidation.NormalizeOptional(description, 1000, nameof(description));
        Kind = kind;
        ProtectedValue = protectedValue;
        Status = SecretStatus.Active;
        LastRotatedAtUtc = createdAt;
        CreatedAt = createdAt;
    }

    public Guid Id { get; private set; }

    public Guid CustomerId { get; private set; }

    public string Name { get; private set; } = string.Empty;

    public string Slug { get; private set; } = string.Empty;

    public string? Description { get; private set; }

    public SecretKind Kind { get; private set; }

    public string ProtectedValue { get; private set; } = string.Empty;

    public SecretStatus Status { get; private set; }

    public DateTimeOffset? LastRotatedAtUtc { get; private set; }

    public DateTimeOffset CreatedAt { get; private set; }

    public DateTimeOffset? UpdatedAt { get; private set; }

    public DateTimeOffset? ArchivedAt { get; private set; }

    public static Secret Create(
        Guid customerId,
        string name,
        string slug,
        string? description,
        SecretKind kind,
        string protectedValue,
        DateTimeOffset createdAt)
    {
        ValidateMetadata(name, slug);
        ValidateProtectedValue(protectedValue);
        return new Secret(Guid.NewGuid(), customerId, name, slug, description, kind, protectedValue, createdAt);
    }

    public void UpdateMetadata(
        string name,
        string slug,
        string? description,
        SecretKind kind,
        DateTimeOffset updatedAt)
    {
        ValidateMetadata(name, slug);
        Name = name.Trim();
        Slug = slug.Trim();
        Description = DefinitionValidation.NormalizeOptional(description, 1000, nameof(description));
        Kind = kind;
        UpdatedAt = updatedAt;
    }

    public void Rotate(string protectedValue, DateTimeOffset rotatedAtUtc)
    {
        ValidateProtectedValue(protectedValue);
        ProtectedValue = protectedValue;
        LastRotatedAtUtc = rotatedAtUtc;
        UpdatedAt = rotatedAtUtc;
    }

    public void Archive(DateTimeOffset archivedAt)
    {
        if (Status == SecretStatus.Archived)
        {
            return;
        }

        Status = SecretStatus.Archived;
        ArchivedAt = archivedAt;
        UpdatedAt = archivedAt;
    }

    private static void ValidateMetadata(string name, string slug)
    {
        DefinitionValidation.ValidateName(name);
        DefinitionValidation.ValidateSlug(slug);
    }

    private static void ValidateProtectedValue(string protectedValue)
    {
        DefinitionValidation.ValidateContent(protectedValue, 200000, nameof(protectedValue));
    }
}
