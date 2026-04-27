namespace NodeControl.Application.Secrets;

public sealed record SecretReferenceDto(
    string Slug,
    bool Found,
    Guid? SecretId,
    string? Status);
