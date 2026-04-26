namespace NodeControl.Application.InventoryGroups;

public sealed record InventoryGroupDto(
    Guid Id,
    Guid CustomerId,
    string Name,
    string? Description,
    DateTimeOffset CreatedAt,
    DateTimeOffset? UpdatedAt,
    DateTimeOffset? ArchivedAt,
    IReadOnlyCollection<Guid> ManagedNodeIds);
