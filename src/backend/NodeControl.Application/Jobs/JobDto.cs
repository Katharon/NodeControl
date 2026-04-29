using NodeControl.Domain.Jobs;

namespace NodeControl.Application.Jobs;

public sealed record JobDto(
    Guid Id,
    Guid CustomerId,
    string Name,
    string Slug,
    string? Description,
    Guid ControlNodeId,
    Guid InventoryGroupId,
    Guid PlaybookId,
    Guid? VariableSetId,
    IReadOnlyList<JobTemplateArtifactDto> TemplateArtifacts,
    JobStatus Status,
    int DefaultTimeoutSeconds,
    DateTimeOffset CreatedAt,
    DateTimeOffset? UpdatedAt,
    DateTimeOffset? ArchivedAt);
