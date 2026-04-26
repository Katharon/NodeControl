namespace NodeControl.Application.InventoryGroups;

public sealed record CreateInventoryGroupRequest(
    string Name,
    string? Description = null);
