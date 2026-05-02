namespace NodeControl.Domain.Nodes;

public sealed class ControlNode
{
    private ControlNode()
    {
    }

    private ControlNode(
        Guid id,
        Guid customerId,
        string name,
        string hostname,
        int sshPort,
        string? sshUsername,
        Guid? sshPrivateKeySecretId,
        string? remoteWorkspaceRoot,
        string? description,
        DateTimeOffset createdAt)
    {
        Id = id;
        CustomerId = customerId;
        Name = name.Trim();
        Hostname = hostname.Trim();
        SshPort = sshPort;
        SshUsername = NormalizeOptionalRemoteText(sshUsername, 100, nameof(sshUsername));
        SshPrivateKeySecretId = sshPrivateKeySecretId;
        RemoteWorkspaceRoot = NormalizeOptionalRemoteText(remoteWorkspaceRoot, 500, nameof(remoteWorkspaceRoot));
        Description = NodeValidation.NormalizeOptional(description, 1000, nameof(description));
        Status = ControlNodeStatus.Active;
        CreatedAt = createdAt;
    }

    public Guid Id { get; private set; }

    public Guid CustomerId { get; private set; }

    public string Name { get; private set; } = string.Empty;

    public string Hostname { get; private set; } = string.Empty;

    public int SshPort { get; private set; }

    public string? SshUsername { get; private set; }

    public Guid? SshPrivateKeySecretId { get; private set; }

    public string? RemoteWorkspaceRoot { get; private set; }

    public string? Description { get; private set; }

    public ControlNodeStatus Status { get; private set; }

    public DateTimeOffset CreatedAt { get; private set; }

    public DateTimeOffset? UpdatedAt { get; private set; }

    public DateTimeOffset? ArchivedAt { get; private set; }

    public static ControlNode Create(
        Guid customerId,
        string name,
        string hostname,
        int sshPort,
        string? description,
        DateTimeOffset createdAt)
    {
        return Create(
            customerId,
            name,
            hostname,
            sshPort,
            sshUsername: null,
            sshPrivateKeySecretId: null,
            remoteWorkspaceRoot: null,
            description,
            createdAt);
    }

    public static ControlNode Create(
        Guid customerId,
        string name,
        string hostname,
        int sshPort,
        string? sshUsername,
        Guid? sshPrivateKeySecretId,
        string? remoteWorkspaceRoot,
        string? description,
        DateTimeOffset createdAt)
    {
        NodeValidation.ValidateName(name, 200);
        NodeValidation.ValidateHostname(hostname);
        NodeValidation.ValidateSshPort(sshPort);
        ValidateRemoteDispatchSettings(sshUsername, sshPrivateKeySecretId, remoteWorkspaceRoot);

        return new ControlNode(
            Guid.NewGuid(),
            customerId,
            name,
            hostname,
            sshPort,
            sshUsername,
            sshPrivateKeySecretId,
            remoteWorkspaceRoot,
            description,
            createdAt);
    }

    public void Update(string name, string hostname, int sshPort, string? description, DateTimeOffset updatedAt)
    {
        Update(
            name,
            hostname,
            sshPort,
            sshUsername: null,
            sshPrivateKeySecretId: null,
            remoteWorkspaceRoot: null,
            description,
            updatedAt);
    }

    public void Update(
        string name,
        string hostname,
        int sshPort,
        string? sshUsername,
        Guid? sshPrivateKeySecretId,
        string? remoteWorkspaceRoot,
        string? description,
        DateTimeOffset updatedAt)
    {
        NodeValidation.ValidateName(name, 200);
        NodeValidation.ValidateHostname(hostname);
        NodeValidation.ValidateSshPort(sshPort);
        ValidateRemoteDispatchSettings(sshUsername, sshPrivateKeySecretId, remoteWorkspaceRoot);

        Name = name.Trim();
        Hostname = hostname.Trim();
        SshPort = sshPort;
        SshUsername = NormalizeOptionalRemoteText(sshUsername, 100, nameof(sshUsername));
        SshPrivateKeySecretId = sshPrivateKeySecretId;
        RemoteWorkspaceRoot = NormalizeOptionalRemoteText(remoteWorkspaceRoot, 500, nameof(remoteWorkspaceRoot));
        Description = NodeValidation.NormalizeOptional(description, 1000, nameof(description));
        UpdatedAt = updatedAt;
    }

    public void Archive(DateTimeOffset archivedAt)
    {
        if (Status == ControlNodeStatus.Archived)
        {
            return;
        }

        Status = ControlNodeStatus.Archived;
        ArchivedAt = archivedAt;
        UpdatedAt = archivedAt;
    }

    private static void ValidateRemoteDispatchSettings(
        string? sshUsername,
        Guid? sshPrivateKeySecretId,
        string? remoteWorkspaceRoot)
    {
        var hasUsername = !string.IsNullOrWhiteSpace(sshUsername);
        var hasKey = sshPrivateKeySecretId.HasValue;
        var hasWorkspaceRoot = !string.IsNullOrWhiteSpace(remoteWorkspaceRoot);

        if (!hasUsername && !hasKey && !hasWorkspaceRoot)
        {
            return;
        }

        if (!hasUsername || !hasKey || !hasWorkspaceRoot)
        {
            throw new ArgumentException("SSH username, private key secret, and remote workspace root are required together.");
        }

        var normalizedUsername = sshUsername!.Trim();
        if (normalizedUsername.Length > 100 || normalizedUsername.Any(char.IsWhiteSpace))
        {
            throw new ArgumentException("SSH username is invalid.", nameof(sshUsername));
        }

        var normalizedRoot = remoteWorkspaceRoot!.Trim().Replace('\\', '/');
        if (normalizedRoot.Length > 500
            || !normalizedRoot.StartsWith("/", StringComparison.Ordinal)
            || normalizedRoot.EndsWith("/", StringComparison.Ordinal)
            || normalizedRoot.Any(char.IsWhiteSpace)
            || normalizedRoot.Split('/').Any(part => part is "." or ".."))
        {
            throw new ArgumentException("Remote workspace root must be an absolute Unix path without whitespace.", nameof(remoteWorkspaceRoot));
        }
    }

    private static string? NormalizeOptionalRemoteText(string? value, int maxLength, string parameterName)
    {
        if (string.IsNullOrWhiteSpace(value))
        {
            return null;
        }

        var normalized = value.Trim().Replace('\\', '/');
        if (normalized.Length > maxLength)
        {
            throw new ArgumentException($"{parameterName} must not exceed {maxLength} characters.", parameterName);
        }

        return normalized;
    }
}
