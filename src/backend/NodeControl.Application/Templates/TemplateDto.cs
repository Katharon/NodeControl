namespace NodeControl.Application.Templates;

public sealed record TemplateDto(
    Guid Id,
    Guid CustomerId,
    string Name,
    string Slug,
    string? Description,
    string TemplateType,
    string Content,
    string? Language,
    string Status,
    DateTimeOffset CreatedAt,
    DateTimeOffset? UpdatedAt,
    DateTimeOffset? ArchivedAt);
