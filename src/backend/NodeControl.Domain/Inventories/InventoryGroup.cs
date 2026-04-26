using NodeControl.Domain.Nodes;

namespace NodeControl.Domain.Inventories;

public sealed class InventoryGroup
{
    private InventoryGroup()
    {
    }

    private InventoryGroup(
        Guid id,
        Guid customerId,
        string name,
        string? description,
        DateTimeOffset createdAt)
    {
        Id = id;
        CustomerId = customerId;
        Name = name.Trim();
        Description = NodeValidation.NormalizeOptional(description, 1000, nameof(description));
        CreatedAt = createdAt;
    }

    public Guid Id { get; private set; }

    public Guid CustomerId { get; private set; }

    public string Name { get; private set; } = string.Empty;

    public string? Description { get; private set; }

    public DateTimeOffset CreatedAt { get; private set; }

    public DateTimeOffset? UpdatedAt { get; private set; }

    public DateTimeOffset? ArchivedAt { get; private set; }

    public bool IsArchived => ArchivedAt is not null;

    public static InventoryGroup Create(
        Guid customerId,
        string name,
        string? description,
        DateTimeOffset createdAt)
    {
        NodeValidation.ValidateInventoryName(name);

        return new InventoryGroup(Guid.NewGuid(), customerId, name, description, createdAt);
    }

    public void Update(string name, string? description, DateTimeOffset updatedAt)
    {
        NodeValidation.ValidateInventoryName(name);

        Name = name.Trim();
        Description = NodeValidation.NormalizeOptional(description, 1000, nameof(description));
        UpdatedAt = updatedAt;
    }

    public void Archive(DateTimeOffset archivedAt)
    {
        if (ArchivedAt is not null)
        {
            return;
        }

        ArchivedAt = archivedAt;
        UpdatedAt = archivedAt;
    }
}
