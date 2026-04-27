using NodeControl.Domain.Playbooks;

namespace NodeControl.Application.Playbooks;

public sealed record PlaybookDto(
    Guid Id,
    Guid CustomerId,
    string Name,
    string Slug,
    string? Description,
    PlaybookSourceType SourceType,
    PlaybookStatus Status,
    string? InlineContent,
    string? EntryFilePath,
    DateTimeOffset CreatedAt,
    DateTimeOffset? UpdatedAt,
    DateTimeOffset? ArchivedAt);
