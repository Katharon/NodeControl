using NodeControl.Domain.GitRepositories;

namespace NodeControl.Application.GitRepositories;

public sealed record GitRepositoryDto(
    Guid Id,
    Guid CustomerId,
    string Name,
    string RepositoryUrl,
    string? Branch,
    string? Revision,
    string? Subpath,
    GitRepositoryStatus Status,
    DateTimeOffset CreatedAt,
    DateTimeOffset? UpdatedAt,
    DateTimeOffset? ArchivedAt);
