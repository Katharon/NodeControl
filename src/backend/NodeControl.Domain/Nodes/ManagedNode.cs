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
        DateTimeOffset createdAt)
    {
        NodeValidation.ValidateInventoryName(name);
        NodeValidation.ValidateHostname(hostname);
        NodeValidation.ValidateSshPort(sshPort);

        return new ManagedNode(
            Guid.NewGuid(),
            customerId,
            name,
            hostname,
            sshPort,
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
        DateTimeOffset updatedAt)
    {
        NodeValidation.ValidateInventoryName(name);
        NodeValidation.ValidateHostname(hostname);
        NodeValidation.ValidateSshPort(sshPort);

        Name = name.Trim();
        Hostname = hostname.Trim();
        SshPort = sshPort;
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
}
