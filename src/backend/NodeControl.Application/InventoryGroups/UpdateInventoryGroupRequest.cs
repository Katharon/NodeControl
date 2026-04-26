namespace NodeControl.Application.InventoryGroups;

public sealed record UpdateInventoryGroupRequest(
    string Name,
    string? Description = null);
