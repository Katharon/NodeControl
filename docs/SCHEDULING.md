# Scheduling

## Scheduling Goal

NodeControl supports cyclic execution of existing Jobs by creating queued JobRuns when a Schedule is due.

Examples:

- Run update checks every night.
- Validate backups every morning.
- Restart selected services every Sunday.
- Check certificate expiration daily.
- Apply a baseline configuration every week.

## Current Decision

The current implementation uses a simple database-backed poller in `NodeControl.Worker`.

Reason:

- It keeps the MVP small.
- Manual and scheduled runs use the same JobRun execution path.
- The API manages schedule definitions only.
- Quartz or another scheduler can still be introduced later if the product needs richer misfire and clustering behavior.

Quartz.NET is not part of the current implementation.

## Concepts

### Job

A reusable execution template.

### JobRun

One concrete execution of a Job.

### JobSchedule

A customer-scoped recurring trigger rule for an existing Job.

Important fields:

- CustomerId
- JobId
- Name
- Slug
- CronExpression
- TimeZoneId
- Status: Active, Paused, Archived
- NextRunAtUtc
- LastRunAtUtc
- LastJobRunId

## Execution Model

Scheduled and manual jobs use the same execution path.

```text
Manual Run:
User -> API -> JobRun Queued -> Worker -> Execute

Scheduled Run:
Worker schedule poller -> JobRun Queued -> Worker queued-run processor -> Execute
```

The schedule poller never runs Ansible. It only creates queued JobRuns.

## Cron Validation

Schedules use standard 5-field cron expressions:

```text
*/5 * * * *
0 * * * *
30 2 * * *
```

When a Schedule is created or updated:

1. Validate the cron expression.
2. Validate the time zone id.
3. Calculate and store `NextRunAtUtc` for active schedules.

## Worker Responsibility

The Worker:

- Polls active schedules with `NextRunAtUtc <= now`.
- Creates queued JobRuns with `TriggerType = Scheduled`.
- Sets `ScheduleId`.
- Leaves `TriggeredByUserId` null.
- Updates `LastRunAtUtc`, `LastJobRunId`, and `NextRunAtUtc`.
- Processes queued JobRuns through the existing execution worker.

## API Responsibility

The API:

- Creates schedules.
- Updates schedules.
- Pauses, resumes, and archives schedules.
- Enforces customer-scoped authorization.
- Does not create due scheduled JobRuns.
- Does not execute jobs directly.

## Frontend Requirements

The schedule UI shows:

- Name
- Job
- Cron expression
- Time zone
- Status
- Next run time
- Last run time
- Last JobRun link where available

## Post-MVP Scheduling Features

Potential later features:

- Quartz.NET integration
- Misfire policies
- Calendar exclusions
- Maintenance windows
- Approval before scheduled run
- Notification on failure
- Retry policy per job
- Maximum runtime per job
- Concurrency configuration
- Schedule templates
