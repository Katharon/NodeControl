namespace NodeControl.Application.Templates;

public sealed record CreateTemplateRequest(
    string Name,
    string Slug,
    string? Description,
    string TemplateType,
    string Content,
    string? Language);
