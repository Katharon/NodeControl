namespace NodeControl.Application.InventoryGroups;

public sealed record InventoryPreviewDto(
    Guid InventoryGroupId,
    string InventoryGroupName,
    string Format,
    string Content);
