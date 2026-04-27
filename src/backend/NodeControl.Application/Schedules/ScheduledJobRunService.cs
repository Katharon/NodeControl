using NodeControl.Application.Abstractions.Persistence;
using NodeControl.Application.Abstractions.Time;
using NodeControl.Domain.Jobs;

namespace NodeControl.Application.Schedules;

public sealed class ScheduledJobRunService(
    INodeControlDbContext dbContext,
    ICronScheduleCalculator cronScheduleCalculator,
    IClock clock)
{
    private const int DueScheduleBatchSize = 50;

    public async Task<int> EnqueueDueSchedulesAsync(CancellationToken cancellationToken = default)
    {
        var now = clock.UtcNow;
        var dueSchedules = await dbContext.ListDueActiveJobSchedulesAsync(
            now,
            DueScheduleBatchSize,
            cancellationToken);

        var enqueuedCount = 0;
        foreach (var schedule in dueSchedules)
        {
            if (schedule.Status != JobScheduleStatus.Active || schedule.NextRunAtUtc is null || schedule.NextRunAtUtc > now)
            {
                continue;
            }

            var job = await dbContext.FindJobAsync(schedule.CustomerId, schedule.JobId, cancellationToken);
            if (job?.Status != JobStatus.Active)
            {
                continue;
            }

            var jobRun = JobRun.CreateScheduled(job, schedule, now);
            dbContext.AddJobRun(jobRun);

            var nextRunAtUtc = cronScheduleCalculator.GetNextRunUtc(
                schedule.CronExpression,
                schedule.TimeZoneId,
                now);

            schedule.MarkTriggered(jobRun, now, nextRunAtUtc);
            enqueuedCount++;
        }

        if (enqueuedCount > 0)
        {
            await dbContext.SaveChangesAsync(cancellationToken);
        }

        return enqueuedCount;
    }
}
