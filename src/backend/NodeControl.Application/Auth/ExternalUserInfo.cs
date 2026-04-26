namespace NodeControl.Application.Auth;

public sealed record ExternalUserInfo(
    string Provider,
    string Subject,
    string Email,
    string DisplayName,
    bool IsPlatformAdmin = false);
