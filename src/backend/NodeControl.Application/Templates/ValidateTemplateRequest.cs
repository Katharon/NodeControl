namespace NodeControl.Application.Templates;

public sealed record ValidateTemplateRequest(
    string TemplateType,
    string Content,
    string? Language);
