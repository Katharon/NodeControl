namespace NodeControl.Application.Jobs;

public sealed record UpdateJobRequest(
    string Name,
    string Slug,
    string? Description,
    Guid ControlNodeId,
    Guid InventoryGroupId,
    Guid PlaybookId,
    Guid? VariableSetId,
    int DefaultTimeoutSeconds = 1800);
