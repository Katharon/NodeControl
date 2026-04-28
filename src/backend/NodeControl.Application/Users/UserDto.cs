namespace NodeControl.Application.Users;

public sealed record UserDto(
    Guid Id,
    string DisplayName,
    string Email,
    bool IsActive,
    bool IsPlatformAdmin,
    DateTimeOffset CreatedAt,
    DateTimeOffset? LastLoginAt,
    IReadOnlyList<ExternalIdentitySummaryDto> ExternalIdentities);

public sealed record ExternalIdentitySummaryDto(
    string Provider,
    string Subject);
