namespace NodeControl.Domain.Customers;

public sealed class Customer
{
    private Customer()
    {
    }

    private Customer(
        Guid id,
        string name,
        string slug,
        string? description,
        DateTimeOffset createdAt)
    {
        Id = id;
        Name = name.Trim();
        Slug = NormalizeSlug(slug);
        Description = NormalizeDescription(description);
        Status = CustomerStatus.Active;
        CreatedAt = createdAt;
    }

    public Guid Id { get; private set; }

    public string Name { get; private set; } = string.Empty;

    public string Slug { get; private set; } = string.Empty;

    public string? Description { get; private set; }

    public CustomerStatus Status { get; private set; }

    public DateTimeOffset CreatedAt { get; private set; }

    public DateTimeOffset? UpdatedAt { get; private set; }

    public DateTimeOffset? ArchivedAt { get; private set; }

    public static Customer Create(
        string name,
        string slug,
        string? description,
        DateTimeOffset createdAt)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(name);
        ArgumentException.ThrowIfNullOrWhiteSpace(slug);

        return new Customer(Guid.NewGuid(), name, slug, description, createdAt);
    }

    public void Update(string name, string slug, string? description, DateTimeOffset updatedAt)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(name);
        ArgumentException.ThrowIfNullOrWhiteSpace(slug);

        Name = name.Trim();
        Slug = NormalizeSlug(slug);
        Description = NormalizeDescription(description);
        UpdatedAt = updatedAt;
    }

    public void Archive(DateTimeOffset archivedAt)
    {
        if (Status == CustomerStatus.Archived)
        {
            return;
        }

        Status = CustomerStatus.Archived;
        ArchivedAt = archivedAt;
        UpdatedAt = archivedAt;
    }

    private static string NormalizeSlug(string slug)
    {
        return slug.Trim().ToLowerInvariant();
    }

    private static string? NormalizeDescription(string? description)
    {
        return string.IsNullOrWhiteSpace(description) ? null : description.Trim();
    }
}
