namespace NodeControl.Application.Schedules;

public sealed record CreateJobScheduleRequest(
    string Name,
    string Slug,
    string? Description,
    Guid JobId,
    string CronExpression,
    string? TimeZoneId);
