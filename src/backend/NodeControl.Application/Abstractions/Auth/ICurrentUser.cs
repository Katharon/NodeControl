namespace NodeControl.Application.Abstractions.Auth;

public interface ICurrentUser
{
    bool IsAuthenticated { get; }

    string? AuthProvider { get; }

    string? ExternalSubject { get; }

    string? Email { get; }

    string? DisplayName { get; }

    bool IsPlatformAdmin { get; }
}
