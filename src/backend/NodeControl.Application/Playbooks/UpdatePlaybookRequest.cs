using NodeControl.Domain.Playbooks;

namespace NodeControl.Application.Playbooks;

public sealed record UpdatePlaybookRequest(
    string Name,
    string Slug,
    string? Description,
    PlaybookSourceType SourceType,
    string? InlineContent,
    string? EntryFilePath,
    IReadOnlyList<PlaybookArtifactFileDto>? ArtifactFiles = null);
