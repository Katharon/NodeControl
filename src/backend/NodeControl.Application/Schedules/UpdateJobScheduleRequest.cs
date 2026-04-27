namespace NodeControl.Application.Schedules;

public sealed record UpdateJobScheduleRequest(
    string Name,
    string Slug,
    string? Description,
    Guid JobId,
    string CronExpression,
    string? TimeZoneId);
