namespace NodeControl.Application.Auth;

public sealed record CurrentUserDto(
    Guid Id,
    string DisplayName,
    string Email,
    bool IsActive,
    bool IsPlatformAdmin,
    string AuthProvider,
    string ExternalSubject);
