namespace NodeControl.Application.Secrets;

public sealed record CreateSecretRequest(
    string Name,
    string Slug,
    string? Description,
    string Kind,
    string Value);
