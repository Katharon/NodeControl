namespace NodeControl.Application.Schedules;

public sealed record JobScheduleDto(
    Guid Id,
    Guid CustomerId,
    Guid JobId,
    string Name,
    string Slug,
    string? Description,
    string CronExpression,
    string TimeZoneId,
    string Status,
    DateTimeOffset? NextRunAtUtc,
    DateTimeOffset? LastRunAtUtc,
    Guid? LastJobRunId,
    DateTimeOffset CreatedAt,
    DateTimeOffset? UpdatedAt,
    DateTimeOffset? ArchivedAt);
