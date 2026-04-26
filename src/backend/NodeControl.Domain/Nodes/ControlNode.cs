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
        string? description,
        DateTimeOffset createdAt)
    {
        Id = id;
        CustomerId = customerId;
        Name = name.Trim();
        Hostname = hostname.Trim();
        SshPort = sshPort;
        Description = NodeValidation.NormalizeOptional(description, 1000, nameof(description));
        Status = ControlNodeStatus.Active;
        CreatedAt = createdAt;
    }

    public Guid Id { get; private set; }

    public Guid CustomerId { get; private set; }

    public string Name { get; private set; } = string.Empty;

    public string Hostname { get; private set; } = string.Empty;

    public int SshPort { get; private set; }

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
        NodeValidation.ValidateName(name, 200);
        NodeValidation.ValidateHostname(hostname);
        NodeValidation.ValidateSshPort(sshPort);

        return new ControlNode(Guid.NewGuid(), customerId, name, hostname, sshPort, description, createdAt);
    }

    public void Update(string name, string hostname, int sshPort, string? description, DateTimeOffset updatedAt)
    {
        NodeValidation.ValidateName(name, 200);
        NodeValidation.ValidateHostname(hostname);
        NodeValidation.ValidateSshPort(sshPort);

        Name = name.Trim();
        Hostname = hostname.Trim();
        SshPort = sshPort;
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
}
