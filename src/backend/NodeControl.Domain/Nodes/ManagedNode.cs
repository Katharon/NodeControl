namespace NodeControl.Domain.Nodes;

public sealed class ManagedNode
{
    private ManagedNode()
    {
    }

    private ManagedNode(
        Guid id,
        Guid customerId,
        string name,
        string hostname,
        int sshPort,
        string? sshUsername,
        Guid? sshPrivateKeySecretId,
        Guid? jumpHostManagedNodeId,
        string? operatingSystem,
        string? environment,
        string? description,
        DateTimeOffset createdAt)
    {
        Id = id;
        CustomerId = customerId;
        Name = name.Trim();
        Hostname = hostname.Trim();
        SshPort = sshPort;
        SshUsername = NormalizeOptionalSshUsername(sshUsername);
        SshPrivateKeySecretId = sshPrivateKeySecretId;
        JumpHostManagedNodeId = jumpHostManagedNodeId;
        OperatingSystem = NodeValidation.NormalizeOptional(operatingSystem, 100, nameof(operatingSystem));
        Environment = NodeValidation.NormalizeOptional(environment, 100, nameof(environment));
        Description = NodeValidation.NormalizeOptional(description, 1000, nameof(description));
        Status = ManagedNodeStatus.Active;
        CreatedAt = createdAt;
    }

    public Guid Id { get; private set; }

    public Guid CustomerId { get; private set; }

    public string Name { get; private set; } = string.Empty;

    public string Hostname { get; private set; } = string.Empty;

    public int SshPort { get; private set; }

    public string? SshUsername { get; private set; }

    public Guid? SshPrivateKeySecretId { get; private set; }

    public Guid? JumpHostManagedNodeId { get; private set; }

    public string? OperatingSystem { get; private set; }

    public string? Environment { get; private set; }

    public string? Description { get; private set; }

    public ManagedNodeStatus Status { get; private set; }

    public DateTimeOffset CreatedAt { get; private set; }

    public DateTimeOffset? UpdatedAt { get; private set; }

    public DateTimeOffset? ArchivedAt { get; private set; }

    public static ManagedNode Create(
        Guid customerId,
        string name,
        string hostname,
        int sshPort,
        string? operatingSystem,
        string? environment,
        string? description,
        DateTimeOffset createdAt,
        Guid? jumpHostManagedNodeId = null)
    {
        return Create(
            customerId,
            name,
            hostname,
            sshPort,
            sshUsername: null,
            sshPrivateKeySecretId: null,
            operatingSystem: operatingSystem,
            environment: environment,
            description: description,
            createdAt: createdAt,
            jumpHostManagedNodeId: jumpHostManagedNodeId);
    }

    public static ManagedNode Create(
        Guid customerId,
        string name,
        string hostname,
        int sshPort,
        string? sshUsername,
        Guid? sshPrivateKeySecretId,
        string? operatingSystem,
        string? environment,
        string? description,
        DateTimeOffset createdAt,
        Guid? jumpHostManagedNodeId = null)
    {
        NodeValidation.ValidateInventoryName(name);
        NodeValidation.ValidateHostname(hostname);
        NodeValidation.ValidateSshPort(sshPort);
        ValidateSshSettings(sshUsername);

        return new ManagedNode(
            Guid.NewGuid(),
            customerId,
            name,
            hostname,
            sshPort,
            sshUsername,
            sshPrivateKeySecretId,
            jumpHostManagedNodeId,
            operatingSystem,
            environment,
            description,
            createdAt);
    }

    public void Update(
        string name,
        string hostname,
        int sshPort,
        string? operatingSystem,
        string? environment,
        string? description,
        DateTimeOffset updatedAt,
        Guid? jumpHostManagedNodeId = null)
    {
        Update(
            name,
            hostname,
            sshPort,
            sshUsername: null,
            sshPrivateKeySecretId: null,
            operatingSystem: operatingSystem,
            environment: environment,
            description: description,
            updatedAt: updatedAt,
            jumpHostManagedNodeId: jumpHostManagedNodeId);
    }

    public void Update(
        string name,
        string hostname,
        int sshPort,
        string? sshUsername,
        Guid? sshPrivateKeySecretId,
        string? operatingSystem,
        string? environment,
        string? description,
        DateTimeOffset updatedAt,
        Guid? jumpHostManagedNodeId = null)
    {
        NodeValidation.ValidateInventoryName(name);
        NodeValidation.ValidateHostname(hostname);
        NodeValidation.ValidateSshPort(sshPort);
        ValidateSshSettings(sshUsername);

        Name = name.Trim();
        Hostname = hostname.Trim();
        SshPort = sshPort;
        SshUsername = NormalizeOptionalSshUsername(sshUsername);
        SshPrivateKeySecretId = sshPrivateKeySecretId;
        JumpHostManagedNodeId = jumpHostManagedNodeId;
        OperatingSystem = NodeValidation.NormalizeOptional(operatingSystem, 100, nameof(operatingSystem));
        Environment = NodeValidation.NormalizeOptional(environment, 100, nameof(environment));
        Description = NodeValidation.NormalizeOptional(description, 1000, nameof(description));
        UpdatedAt = updatedAt;
    }

    public void Archive(DateTimeOffset archivedAt)
    {
        if (Status == ManagedNodeStatus.Archived)
        {
            return;
        }

        Status = ManagedNodeStatus.Archived;
        ArchivedAt = archivedAt;
        UpdatedAt = archivedAt;
    }

    private static void ValidateSshSettings(string? sshUsername)
    {
        if (string.IsNullOrWhiteSpace(sshUsername))
        {
            return;
        }

        var normalizedUsername = sshUsername.Trim();
        if (normalizedUsername.Length > 100 || normalizedUsername.Any(char.IsWhiteSpace))
        {
            throw new ArgumentException("SSH username is invalid.", nameof(sshUsername));
        }
    }

    private static string? NormalizeOptionalSshUsername(string? sshUsername)
    {
        return string.IsNullOrWhiteSpace(sshUsername) ? null : sshUsername.Trim();
    }
}
