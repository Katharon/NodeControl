namespace NodeControl.Application.Users;

public sealed record UserLookupDto(
    Guid Id,
    string DisplayName,
    string Email,
    bool IsActive,
    bool IsPlatformAdmin);
