namespace NodeControl.Application.Secrets;

public sealed record SecretReferenceValidationResultDto(
    bool IsValid,
    IReadOnlyList<SecretReferenceDto> References,
    IReadOnlyList<string> Errors,
    IReadOnlyList<string> Warnings);
