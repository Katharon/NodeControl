using System.Text.RegularExpressions;

namespace NodeControl.Domain.Jobs;

public sealed class JobSchedule
{
    private static readonly Regex SlugPattern = new("^[a-z0-9][a-z0-9-]{1,99}$", RegexOptions.Compiled);

    private JobSchedule()
    {
    }

    private JobSchedule(
        Guid id,
        Guid customerId,
        Guid jobId,
        string name,
        string slug,
        string? description,
        string cronExpression,
        string timeZoneId,
        DateTimeOffset? nextRunAtUtc,
        DateTimeOffset createdAt)
    {
        Id = id;
        CustomerId = customerId;
        JobId = jobId;
        Name = name.Trim();
        Slug = slug.Trim();
        Description = NormalizeOptional(description, 1000, nameof(description));
        CronExpression = cronExpression.Trim();
        TimeZoneId = timeZoneId.Trim();
        Status = JobScheduleStatus.Active;
        NextRunAtUtc = nextRunAtUtc;
        CreatedAt = createdAt;
    }

    public Guid Id { get; private set; }

    public Guid CustomerId { get; private set; }

    public Guid JobId { get; private set; }

    public Job Job { get; private set; } = null!;

    public string Name { get; private set; } = string.Empty;

    public string Slug { get; private set; } = string.Empty;

    public string? Description { get; private set; }

    public string CronExpression { get; private set; } = string.Empty;

    public string TimeZoneId { get; private set; } = "UTC";

    public JobScheduleStatus Status { get; private set; }

    public DateTimeOffset? NextRunAtUtc { get; private set; }

    public DateTimeOffset? LastRunAtUtc { get; private set; }

    public Guid? LastJobRunId { get; private set; }

    public DateTimeOffset CreatedAt { get; private set; }

    public DateTimeOffset? UpdatedAt { get; private set; }

    public DateTimeOffset? ArchivedAt { get; private set; }

    public static JobSchedule Create(
        Job job,
        string name,
        string slug,
        string? description,
        string cronExpression,
        string timeZoneId,
        DateTimeOffset? nextRunAtUtc,
        DateTimeOffset createdAt)
    {
        ArgumentNullException.ThrowIfNull(job);
        if (job.Status != JobStatus.Active)
        {
            throw new InvalidOperationException("Archived jobs cannot be scheduled.");
        }

        Validate(name, slug, job.Id, cronExpression, timeZoneId);
        return new JobSchedule(
            Guid.NewGuid(),
            job.CustomerId,
            job.Id,
            name,
            slug,
            description,
            cronExpression,
            timeZoneId,
            nextRunAtUtc,
            createdAt);
    }

    public void Update(
        Job job,
        string name,
        string slug,
        string? description,
        string cronExpression,
        string timeZoneId,
        DateTimeOffset? nextRunAtUtc,
        DateTimeOffset updatedAt)
    {
        ArgumentNullException.ThrowIfNull(job);
        if (job.CustomerId != CustomerId)
        {
            throw new InvalidOperationException("Schedule and job must belong to the same customer.");
        }

        if (job.Status != JobStatus.Active)
        {
            throw new InvalidOperationException("Archived jobs cannot be scheduled.");
        }

        Validate(name, slug, job.Id, cronExpression, timeZoneId);
        JobId = job.Id;
        Name = name.Trim();
        Slug = slug.Trim();
        Description = NormalizeOptional(description, 1000, nameof(description));
        CronExpression = cronExpression.Trim();
        TimeZoneId = timeZoneId.Trim();
        if (Status == JobScheduleStatus.Active)
        {
            NextRunAtUtc = nextRunAtUtc;
        }

        UpdatedAt = updatedAt;
    }

    public void Pause(DateTimeOffset updatedAt)
    {
        if (Status == JobScheduleStatus.Archived)
        {
            return;
        }

        Status = JobScheduleStatus.Paused;
        NextRunAtUtc = null;
        UpdatedAt = updatedAt;
    }

    public void Resume(Job job, DateTimeOffset? nextRunAtUtc, DateTimeOffset updatedAt)
    {
        ArgumentNullException.ThrowIfNull(job);
        if (Status == JobScheduleStatus.Archived)
        {
            return;
        }

        if (job.CustomerId != CustomerId || job.Id != JobId || job.Status != JobStatus.Active)
        {
            throw new InvalidOperationException("Schedule cannot be resumed for an unavailable job.");
        }

        Status = JobScheduleStatus.Active;
        NextRunAtUtc = nextRunAtUtc;
        UpdatedAt = updatedAt;
    }

    public void Archive(DateTimeOffset archivedAt)
    {
        if (Status == JobScheduleStatus.Archived)
        {
            return;
        }

        Status = JobScheduleStatus.Archived;
        NextRunAtUtc = null;
        ArchivedAt = archivedAt;
        UpdatedAt = archivedAt;
    }

    public void MarkTriggered(JobRun jobRun, DateTimeOffset triggeredAtUtc, DateTimeOffset? nextRunAtUtc)
    {
        ArgumentNullException.ThrowIfNull(jobRun);
        if (jobRun.CustomerId != CustomerId || jobRun.JobId != JobId)
        {
            throw new InvalidOperationException("Scheduled JobRun does not match the schedule.");
        }

        LastRunAtUtc = triggeredAtUtc;
        LastJobRunId = jobRun.Id;
        NextRunAtUtc = nextRunAtUtc;
        UpdatedAt = triggeredAtUtc;
    }

    private static void Validate(
        string name,
        string slug,
        Guid jobId,
        string cronExpression,
        string timeZoneId)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(name);
        if (name.Trim().Length > 200)
        {
            throw new ArgumentException("Name must be at most 200 characters.", nameof(name));
        }

        if (string.IsNullOrWhiteSpace(slug) || !SlugPattern.IsMatch(slug.Trim()))
        {
            throw new ArgumentException("Slug must use lowercase letters, numbers, and hyphens.", nameof(slug));
        }

        if (jobId == Guid.Empty)
        {
            throw new ArgumentException("A job id is required.", nameof(jobId));
        }

        ArgumentException.ThrowIfNullOrWhiteSpace(cronExpression);
        if (cronExpression.Trim().Length > 120)
        {
            throw new ArgumentException("Cron expression must be at most 120 characters.", nameof(cronExpression));
        }

        ArgumentException.ThrowIfNullOrWhiteSpace(timeZoneId);
        if (timeZoneId.Trim().Length > 100)
        {
            throw new ArgumentException("Time zone id must be at most 100 characters.", nameof(timeZoneId));
        }
    }

    private static string? NormalizeOptional(string? value, int maxLength, string name)
    {
        if (string.IsNullOrWhiteSpace(value))
        {
            return null;
        }

        var trimmed = value.Trim();
        if (trimmed.Length > maxLength)
        {
            throw new ArgumentException($"{name} must be at most {maxLength} characters.", name);
        }

        return trimmed;
    }
}
