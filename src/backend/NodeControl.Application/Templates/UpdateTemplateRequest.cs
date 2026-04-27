namespace NodeControl.Application.Templates;

public sealed record UpdateTemplateRequest(
    string Name,
    string Slug,
    string? Description,
    string TemplateType,
    string Content,
    string? Language);
