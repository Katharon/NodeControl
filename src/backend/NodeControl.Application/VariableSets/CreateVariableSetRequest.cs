using NodeControl.Domain.VariableSets;

namespace NodeControl.Application.VariableSets;

public sealed record CreateVariableSetRequest(
    string Name,
    string Slug,
    string? Description,
    VariableSetFormat Format,
    string Content,
    bool ContainsSensitiveValues);
