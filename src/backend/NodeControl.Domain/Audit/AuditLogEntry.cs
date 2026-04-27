namespace NodeControl.Domain.Audit;

public sealed class AuditLogEntry
{
    public const int ActorDisplayNameMaxLength = 300;
    public const int ActionMaxLength = 200;
    public const int EntityTypeMaxLength = 100;
    public const int EntityDisplayNameMaxLength = 300;
    public const int MessageMaxLength = 1000;
    public const int MetadataJsonMaxLength = 8000;
    public const int IpAddressMaxLength = 100;
    public const int UserAgentMaxLength = 500;

    private AuditLogEntry()
    {
    }

    private AuditLogEntry(
        Guid id,
        Guid? customerId,
        Guid? actorUserId,
        string? actorDisplayName,
        AuditActorType actorType,
        string action,
        string entityType,
        Guid? entityId,
        string? entityDisplayName,
        AuditOutcome outcome,
        string message,
        string? metadataJson,
        string? ipAddress,
        string? userAgent,
        DateTimeOffset createdAtUtc)
    {
        Id = id;
        CustomerId = customerId;
        ActorUserId = actorUserId;
        ActorDisplayName = actorDisplayName;
        ActorType = actorType;
        Action = action;
        EntityType = entityType;
        EntityId = entityId;
        EntityDisplayName = entityDisplayName;
        Outcome = outcome;
        Message = message;
        MetadataJson = metadataJson;
        IpAddress = ipAddress;
        UserAgent = userAgent;
        CreatedAtUtc = createdAtUtc;
    }

    public Guid Id { get; private set; }

    public Guid? CustomerId { get; private set; }

    public Guid? ActorUserId { get; private set; }

    public string? ActorDisplayName { get; private set; }

    public AuditActorType ActorType { get; private set; }

    public string Action { get; private set; } = string.Empty;

    public string EntityType { get; private set; } = string.Empty;

    public Guid? EntityId { get; private set; }

    public string? EntityDisplayName { get; private set; }

    public AuditOutcome Outcome { get; private set; }

    public string Message { get; private set; } = string.Empty;

    public string? MetadataJson { get; private set; }

    public string? IpAddress { get; private set; }

    public string? UserAgent { get; private set; }

    public DateTimeOffset CreatedAtUtc { get; private set; }

    public static AuditLogEntry Create(
        Guid? customerId,
        Guid? actorUserId,
        string? actorDisplayName,
        AuditActorType actorType,
        string action,
        string entityType,
        Guid? entityId,
        string? entityDisplayName,
        AuditOutcome outcome,
        string message,
        string? metadataJson,
        string? ipAddress,
        string? userAgent,
        DateTimeOffset createdAtUtc)
    {
        return new AuditLogEntry(
            Guid.NewGuid(),
            customerId,
            actorUserId,
            NormalizeOptional(actorDisplayName, ActorDisplayNameMaxLength, nameof(actorDisplayName)),
            actorType,
            NormalizeRequired(action, ActionMaxLength, nameof(action)),
            NormalizeRequired(entityType, EntityTypeMaxLength, nameof(entityType)),
            entityId,
            NormalizeOptional(entityDisplayName, EntityDisplayNameMaxLength, nameof(entityDisplayName)),
            outcome,
            NormalizeRequired(message, MessageMaxLength, nameof(message)),
            NormalizeOptional(metadataJson, MetadataJsonMaxLength, nameof(metadataJson)),
            NormalizeOptional(ipAddress, IpAddressMaxLength, nameof(ipAddress)),
            NormalizeOptional(userAgent, UserAgentMaxLength, nameof(userAgent)),
            createdAtUtc);
    }

    private static string NormalizeRequired(string value, int maxLength, string parameterName)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(value, parameterName);
        var trimmed = value.Trim();
        if (trimmed.Length > maxLength)
        {
            throw new ArgumentException($"{parameterName} must be at most {maxLength} characters.", parameterName);
        }

        return trimmed;
    }

    private static string? NormalizeOptional(string? value, int maxLength, string parameterName)
    {
        if (string.IsNullOrWhiteSpace(value))
        {
            return null;
        }

        var trimmed = value.Trim();
        if (trimmed.Length > maxLength)
        {
            throw new ArgumentException($"{parameterName} must be at most {maxLength} characters.", parameterName);
        }

        return trimmed;
    }
}
