using NodeControl.Domain.VariableSets;

namespace NodeControl.Application.VariableSets;

public sealed record VariableSetDto(
    Guid Id,
    Guid CustomerId,
    string Name,
    string Slug,
    string? Description,
    VariableSetFormat Format,
    string Content,
    bool ContainsSensitiveValues,
    VariableSetStatus Status,
    DateTimeOffset CreatedAt,
    DateTimeOffset? UpdatedAt,
    DateTimeOffset? ArchivedAt);
