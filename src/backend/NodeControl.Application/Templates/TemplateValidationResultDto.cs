namespace NodeControl.Application.Templates;

public sealed record TemplateValidationResultDto(
    bool IsValid,
    IReadOnlyList<string> Errors,
    IReadOnlyList<string> Warnings);
