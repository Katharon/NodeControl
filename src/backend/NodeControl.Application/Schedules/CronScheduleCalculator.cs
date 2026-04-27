namespace NodeControl.Application.Schedules;

public sealed class CronScheduleCalculator : ICronScheduleCalculator
{
    private const int SearchWindowMinutes = 366 * 24 * 60;

    public bool IsValidExpression(string cronExpression)
    {
        return TryParse(cronExpression, out _);
    }

    public bool IsValidTimeZone(string timeZoneId)
    {
        if (string.IsNullOrWhiteSpace(timeZoneId))
        {
            return false;
        }

        try
        {
            _ = TimeZoneInfo.FindSystemTimeZoneById(timeZoneId.Trim());
            return true;
        }
        catch (TimeZoneNotFoundException)
        {
            return false;
        }
        catch (InvalidTimeZoneException)
        {
            return false;
        }
    }

    public DateTimeOffset? GetNextRunUtc(string cronExpression, string timeZoneId, DateTimeOffset fromUtc)
    {
        if (!TryParse(cronExpression, out var schedule))
        {
            return null;
        }

        TimeZoneInfo timeZone;
        try
        {
            timeZone = TimeZoneInfo.FindSystemTimeZoneById(timeZoneId.Trim());
        }
        catch (TimeZoneNotFoundException)
        {
            return null;
        }
        catch (InvalidTimeZoneException)
        {
            return null;
        }

        var localFrom = TimeZoneInfo.ConvertTime(fromUtc, timeZone);
        var candidate = new DateTime(
            localFrom.Year,
            localFrom.Month,
            localFrom.Day,
            localFrom.Hour,
            localFrom.Minute,
            0,
            DateTimeKind.Unspecified).AddMinutes(1);

        for (var index = 0; index < SearchWindowMinutes; index++)
        {
            if (!timeZone.IsInvalidTime(candidate) && schedule.Matches(candidate))
            {
                var offset = timeZone.GetUtcOffset(candidate);
                return new DateTimeOffset(candidate, offset).ToUniversalTime();
            }

            candidate = candidate.AddMinutes(1);
        }

        return null;
    }

    private static bool TryParse(string cronExpression, out CronSchedule schedule)
    {
        schedule = default;
        if (string.IsNullOrWhiteSpace(cronExpression))
        {
            return false;
        }

        var parts = cronExpression.Trim().Split(' ', StringSplitOptions.RemoveEmptyEntries);
        if (parts.Length != 5)
        {
            return false;
        }

        if (!TryParseField(parts[0], 0, 59, out var minutes)
            || !TryParseField(parts[1], 0, 23, out var hours)
            || !TryParseField(parts[2], 1, 31, out var daysOfMonth)
            || !TryParseField(parts[3], 1, 12, out var months)
            || !TryParseField(parts[4], 0, 7, out var daysOfWeek))
        {
            return false;
        }

        if (daysOfWeek.Remove(7))
        {
            daysOfWeek.Add(0);
        }

        schedule = new CronSchedule(minutes, hours, daysOfMonth, months, daysOfWeek);
        return true;
    }

    private static bool TryParseField(string field, int min, int max, out HashSet<int> values)
    {
        values = [];
        foreach (var part in field.Split(',', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries))
        {
            if (!TryAddFieldPart(part, min, max, values))
            {
                return false;
            }
        }

        return values.Count > 0;
    }

    private static bool TryAddFieldPart(string part, int min, int max, ISet<int> values)
    {
        var rangePart = part;
        var step = 1;
        var slashIndex = part.IndexOf('/', StringComparison.Ordinal);
        if (slashIndex >= 0)
        {
            rangePart = part[..slashIndex];
            if (!int.TryParse(part[(slashIndex + 1)..], out step) || step <= 0)
            {
                return false;
            }
        }

        int start;
        int end;
        if (rangePart == "*")
        {
            start = min;
            end = max;
        }
        else
        {
            var dashIndex = rangePart.IndexOf('-', StringComparison.Ordinal);
            if (dashIndex >= 0)
            {
                if (!int.TryParse(rangePart[..dashIndex], out start)
                    || !int.TryParse(rangePart[(dashIndex + 1)..], out end))
                {
                    return false;
                }
            }
            else
            {
                if (!int.TryParse(rangePart, out start))
                {
                    return false;
                }

                end = start;
            }
        }

        if (start < min || end > max || start > end)
        {
            return false;
        }

        for (var value = start; value <= end; value += step)
        {
            values.Add(value);
        }

        return true;
    }

    private readonly record struct CronSchedule(
        HashSet<int> Minutes,
        HashSet<int> Hours,
        HashSet<int> DaysOfMonth,
        HashSet<int> Months,
        HashSet<int> DaysOfWeek)
    {
        public bool Matches(DateTime localTime)
        {
            return Minutes.Contains(localTime.Minute)
                && Hours.Contains(localTime.Hour)
                && DaysOfMonth.Contains(localTime.Day)
                && Months.Contains(localTime.Month)
                && DaysOfWeek.Contains((int)localTime.DayOfWeek);
        }
    }
}
