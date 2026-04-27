namespace NodeControl.Application.Secrets;

public sealed record SecretDto(
    Guid Id,
    Guid CustomerId,
    string Name,
    string Slug,
    string? Description,
    string Kind,
    string Status,
    bool HasValue,
    DateTimeOffset? LastRotatedAtUtc,
    DateTimeOffset CreatedAt,
    DateTimeOffset? UpdatedAt,
    DateTimeOffset? ArchivedAt);
