namespace NodeControl.Domain.Users;

public sealed class ExternalIdentity
{
    private ExternalIdentity()
    {
    }

    private ExternalIdentity(
        Guid id,
        User user,
        string provider,
        string subject,
        string emailAtLogin,
        string displayNameAtLogin,
        DateTimeOffset createdAt)
    {
        Id = id;
        User = user;
        UserId = user.Id;
        Provider = provider.Trim();
        Subject = subject.Trim();
        EmailAtLogin = emailAtLogin.Trim();
        DisplayNameAtLogin = displayNameAtLogin.Trim();
        CreatedAt = createdAt;
        LastSeenAt = createdAt;
    }

    public Guid Id { get; private set; }

    public Guid UserId { get; private set; }

    public User User { get; private set; } = null!;

    public string Provider { get; private set; } = string.Empty;

    public string Subject { get; private set; } = string.Empty;

    public string EmailAtLogin { get; private set; } = string.Empty;

    public string DisplayNameAtLogin { get; private set; } = string.Empty;

    public DateTimeOffset CreatedAt { get; private set; }

    public DateTimeOffset LastSeenAt { get; private set; }

    public static ExternalIdentity Create(
        User user,
        string provider,
        string subject,
        string emailAtLogin,
        string displayNameAtLogin,
        DateTimeOffset createdAt)
    {
        return new ExternalIdentity(
            Guid.NewGuid(),
            user,
            provider,
            subject,
            emailAtLogin,
            displayNameAtLogin,
            createdAt);
    }

    public void RecordSeen(string emailAtLogin, string displayNameAtLogin, DateTimeOffset seenAt)
    {
        EmailAtLogin = emailAtLogin.Trim();
        DisplayNameAtLogin = displayNameAtLogin.Trim();
        LastSeenAt = seenAt;
    }
}
