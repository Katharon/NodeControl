namespace NodeControl.Application.Jobs;

public sealed record CreateJobRequest(
    string Name,
    string Slug,
    string? Description,
    Guid ControlNodeId,
    Guid InventoryGroupId,
    Guid PlaybookId,
    Guid? VariableSetId,
    int DefaultTimeoutSeconds = 1800,
    IReadOnlyList<JobTemplateArtifactDto>? TemplateArtifacts = null);
