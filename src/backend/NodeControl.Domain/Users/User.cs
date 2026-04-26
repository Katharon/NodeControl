namespace NodeControl.Domain.Users;

public sealed class User
{
    private User()
    {
    }

    private User(
        Guid id,
        string displayName,
        string email,
        bool isPlatformAdmin,
        DateTimeOffset createdAt)
    {
        Id = id;
        DisplayName = displayName;
        Email = email;
        NormalizedEmail = NormalizeEmail(email);
        IsActive = true;
        IsPlatformAdmin = isPlatformAdmin;
        CreatedAt = createdAt;
        LastLoginAt = createdAt;
    }

    public Guid Id { get; private set; }

    public string DisplayName { get; private set; } = string.Empty;

    public string Email { get; private set; } = string.Empty;

    public string NormalizedEmail { get; private set; } = string.Empty;

    public bool IsActive { get; private set; }

    public bool IsPlatformAdmin { get; private set; }

    public DateTimeOffset CreatedAt { get; private set; }

    public DateTimeOffset? UpdatedAt { get; private set; }

    public DateTimeOffset? LastLoginAt { get; private set; }

    public static User Create(
        string displayName,
        string email,
        bool isPlatformAdmin,
        DateTimeOffset createdAt)
    {
        return new User(Guid.NewGuid(), displayName.Trim(), email.Trim(), isPlatformAdmin, createdAt);
    }

    public void RecordLogin(string displayName, string email, DateTimeOffset loginAt)
    {
        var trimmedDisplayName = displayName.Trim();
        var trimmedEmail = email.Trim();
        var normalizedEmail = NormalizeEmail(trimmedEmail);

        if (DisplayName != trimmedDisplayName || Email != trimmedEmail || NormalizedEmail != normalizedEmail)
        {
            DisplayName = trimmedDisplayName;
            Email = trimmedEmail;
            NormalizedEmail = normalizedEmail;
            UpdatedAt = loginAt;
        }

        LastLoginAt = loginAt;
    }

    private static string NormalizeEmail(string email)
    {
        return email.Trim().ToUpperInvariant();
    }
}
