namespace NodeControl.Application.Schedules;

public interface ICronScheduleCalculator
{
    bool IsValidExpression(string cronExpression);

    bool IsValidTimeZone(string timeZoneId);

    DateTimeOffset? GetNextRunUtc(string cronExpression, string timeZoneId, DateTimeOffset fromUtc);
}
