using System.Text.RegularExpressions;
using NodeControl.Domain.Playbooks;

namespace NodeControl.Domain.Jobs;

public sealed class Job
{
    public const int DefaultTimeoutSecondsDefault = 1800;
    public const int DefaultTimeoutSecondsMin = 30;
    public const int DefaultTimeoutSecondsMax = 86400;

    private static readonly Regex SlugPattern = new("^[a-z0-9][a-z0-9-]{1,99}$", RegexOptions.Compiled);

    private Job()
    {
    }

    private Job(
        Guid id,
        Guid customerId,
        string name,
        string slug,
        string? description,
        Guid controlNodeId,
        Guid inventoryGroupId,
        Guid playbookId,
        Guid? variableSetId,
        int defaultTimeoutSeconds,
        DateTimeOffset createdAt)
    {
        Id = id;
        CustomerId = customerId;
        Name = name.Trim();
        Slug = slug.Trim();
        Description = DefinitionValidation.NormalizeOptional(description, 1000, nameof(description));
        ControlNodeId = controlNodeId;
        InventoryGroupId = inventoryGroupId;
        PlaybookId = playbookId;
        VariableSetId = variableSetId;
        Status = JobStatus.Active;
        DefaultTimeoutSeconds = defaultTimeoutSeconds;
        CreatedAt = createdAt;
    }

    public Guid Id { get; private set; }

    public Guid CustomerId { get; private set; }

    public string Name { get; private set; } = string.Empty;

    public string Slug { get; private set; } = string.Empty;

    public string? Description { get; private set; }

    public Guid ControlNodeId { get; private set; }

    public Guid InventoryGroupId { get; private set; }

    public Guid PlaybookId { get; private set; }

    public Guid? VariableSetId { get; private set; }

    public JobStatus Status { get; private set; }

    public int DefaultTimeoutSeconds { get; private set; }

    public DateTimeOffset CreatedAt { get; private set; }

    public DateTimeOffset? UpdatedAt { get; private set; }

    public DateTimeOffset? ArchivedAt { get; private set; }

    public static Job Create(
        Guid customerId,
        string name,
        string slug,
        string? description,
        Guid controlNodeId,
        Guid inventoryGroupId,
        Guid playbookId,
        Guid? variableSetId,
        int defaultTimeoutSeconds,
        DateTimeOffset createdAt)
    {
        Validate(name, slug, controlNodeId, inventoryGroupId, playbookId, defaultTimeoutSeconds);
        return new Job(
            Guid.NewGuid(),
            customerId,
            name,
            slug,
            description,
            controlNodeId,
            inventoryGroupId,
            playbookId,
            variableSetId,
            defaultTimeoutSeconds,
            createdAt);
    }

    public void Update(
        string name,
        string slug,
        string? description,
        Guid controlNodeId,
        Guid inventoryGroupId,
        Guid playbookId,
        Guid? variableSetId,
        int defaultTimeoutSeconds,
        DateTimeOffset updatedAt)
    {
        Validate(name, slug, controlNodeId, inventoryGroupId, playbookId, defaultTimeoutSeconds);
        Name = name.Trim();
        Slug = slug.Trim();
        Description = DefinitionValidation.NormalizeOptional(description, 1000, nameof(description));
        ControlNodeId = controlNodeId;
        InventoryGroupId = inventoryGroupId;
        PlaybookId = playbookId;
        VariableSetId = variableSetId;
        DefaultTimeoutSeconds = defaultTimeoutSeconds;
        UpdatedAt = updatedAt;
    }

    public void Archive(DateTimeOffset archivedAt)
    {
        if (Status == JobStatus.Archived)
        {
            return;
        }

        Status = JobStatus.Archived;
        ArchivedAt = archivedAt;
        UpdatedAt = archivedAt;
    }

    private static void Validate(
        string name,
        string slug,
        Guid controlNodeId,
        Guid inventoryGroupId,
        Guid playbookId,
        int defaultTimeoutSeconds)
    {
        DefinitionValidation.ValidateName(name);
        ValidateSlug(slug);
        ValidateRequiredId(controlNodeId, nameof(controlNodeId));
        ValidateRequiredId(inventoryGroupId, nameof(inventoryGroupId));
        ValidateRequiredId(playbookId, nameof(playbookId));
        if (defaultTimeoutSeconds is < DefaultTimeoutSecondsMin or > DefaultTimeoutSecondsMax)
        {
            throw new ArgumentException("Default timeout seconds is outside the allowed range.", nameof(defaultTimeoutSeconds));
        }
    }

    private static void ValidateSlug(string slug)
    {
        if (string.IsNullOrWhiteSpace(slug) || !SlugPattern.IsMatch(slug.Trim()))
        {
            throw new ArgumentException("Slug must use lowercase letters, numbers, and hyphens.", nameof(slug));
        }
    }

    private static void ValidateRequiredId(Guid id, string parameterName)
    {
        if (id == Guid.Empty)
        {
            throw new ArgumentException("A resource id is required.", parameterName);
        }
    }
}
