namespace NodeControl.Domain.Nodes;

public sealed class HostConnectionCheck
{
    private HostConnectionCheck()
    {
    }

    private HostConnectionCheck(
        Guid id,
        Guid customerId,
        HostConnectionTargetType targetType,
        Guid? controlNodeId,
        Guid? managedNodeId,
        string hostname,
        int port,
        Guid? requestedByUserId,
        DateTimeOffset queuedAtUtc)
    {
        Id = id;
        CustomerId = customerId;
        TargetType = targetType;
        ControlNodeId = controlNodeId;
        ManagedNodeId = managedNodeId;
        Hostname = hostname.Trim();
        Port = port;
        Status = HostConnectionCheckStatus.Queued;
        RequestedByUserId = requestedByUserId;
        QueuedAtUtc = queuedAtUtc;
    }

    public Guid Id { get; private set; }

    public Guid CustomerId { get; private set; }

    public HostConnectionTargetType TargetType { get; private set; }

    public Guid? ControlNodeId { get; private set; }

    public Guid? ManagedNodeId { get; private set; }

    public string Hostname { get; private set; } = string.Empty;

    public int Port { get; private set; }

    public HostConnectionCheckStatus Status { get; private set; }

    public Guid? RequestedByUserId { get; private set; }

    public DateTimeOffset QueuedAtUtc { get; private set; }

    public DateTimeOffset? StartedAtUtc { get; private set; }

    public DateTimeOffset? FinishedAtUtc { get; private set; }

    public long? DurationMs { get; private set; }

    public string? ResultMessage { get; private set; }

    public string? ErrorMessage { get; private set; }

    public static HostConnectionCheck CreateForControlNode(
        ControlNode controlNode,
        Guid? requestedByUserId,
        DateTimeOffset queuedAtUtc)
    {
        if (controlNode.Status != ControlNodeStatus.Active)
        {
            throw new InvalidOperationException("Control node must be active.");
        }

        ValidateEndpoint(controlNode.Hostname, controlNode.SshPort);
        return new HostConnectionCheck(
            Guid.NewGuid(),
            controlNode.CustomerId,
            HostConnectionTargetType.ControlNode,
            controlNode.Id,
            null,
            controlNode.Hostname,
            controlNode.SshPort,
            requestedByUserId,
            queuedAtUtc);
    }

    public static HostConnectionCheck CreateForManagedNode(
        ManagedNode managedNode,
        Guid? requestedByUserId,
        DateTimeOffset queuedAtUtc)
    {
        if (managedNode.Status != ManagedNodeStatus.Active)
        {
            throw new InvalidOperationException("Managed node must be active.");
        }

        ValidateEndpoint(managedNode.Hostname, managedNode.SshPort);
        return new HostConnectionCheck(
            Guid.NewGuid(),
            managedNode.CustomerId,
            HostConnectionTargetType.ManagedNode,
            null,
            managedNode.Id,
            managedNode.Hostname,
            managedNode.SshPort,
            requestedByUserId,
            queuedAtUtc);
    }

    public void MarkRunning(DateTimeOffset startedAtUtc)
    {
        if (Status != HostConnectionCheckStatus.Queued)
        {
            return;
        }

        Status = HostConnectionCheckStatus.Running;
        StartedAtUtc = startedAtUtc;
    }

    public void MarkSucceeded(string resultMessage, DateTimeOffset finishedAtUtc)
    {
        MarkCompleted(HostConnectionCheckStatus.Succeeded, resultMessage, null, finishedAtUtc);
    }

    public void MarkFailed(string errorMessage, DateTimeOffset finishedAtUtc)
    {
        MarkCompleted(HostConnectionCheckStatus.Failed, null, errorMessage, finishedAtUtc);
    }

    public void MarkTimedOut(string errorMessage, DateTimeOffset finishedAtUtc)
    {
        MarkCompleted(HostConnectionCheckStatus.TimedOut, null, errorMessage, finishedAtUtc);
    }

    private void MarkCompleted(
        HostConnectionCheckStatus status,
        string? resultMessage,
        string? errorMessage,
        DateTimeOffset finishedAtUtc)
    {
        if (Status != HostConnectionCheckStatus.Running)
        {
            throw new InvalidOperationException("Only running checks can be completed.");
        }

        Status = status;
        FinishedAtUtc = finishedAtUtc;
        DurationMs = StartedAtUtc is null
            ? null
            : Math.Max(0, (long)(finishedAtUtc - StartedAtUtc.Value).TotalMilliseconds);
        ResultMessage = NormalizeOptional(resultMessage, 2000, nameof(resultMessage));
        ErrorMessage = NormalizeOptional(errorMessage, 4000, nameof(errorMessage));
    }

    private static void ValidateEndpoint(string hostname, int port)
    {
        NodeValidation.ValidateHostname(hostname);
        NodeValidation.ValidateSshPort(port);
    }

    private static string? NormalizeOptional(string? value, int maxLength, string name)
    {
        return NodeValidation.NormalizeOptional(value, maxLength, name);
    }
}
