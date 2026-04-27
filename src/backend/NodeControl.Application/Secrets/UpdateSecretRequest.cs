namespace NodeControl.Application.Secrets;

public sealed record UpdateSecretRequest(
    string Name,
    string Slug,
    string? Description,
    string Kind);
