# Scheduling

## Scheduling Goal

NodeControl must support cyclic execution of Ansible jobs.

Examples:

- Run update checks every night.
- Validate backups every morning.
- Restart selected services every Sunday.
- Check certificate expiration daily.
- Apply a baseline configuration every week.

## Main Decision

Use Quartz.NET for scheduling.

Reason:

- Cron expressions are a core requirement.
- Scheduling is a product feature.
- Building reliable cron/misfire behavior manually is unnecessary complexity.

## Important Domain Separation

Quartz is an implementation detail.

NodeControl owns the domain model:

- Job
- JobRun
- Schedule

## Concepts

### Job

A reusable execution template.

Example:

```text
Run "Update packages" playbook on "Production Linux Servers" with "Default Maintenance Vars".
```

### JobRun

One concrete execution of a Job.

Example:

```text
JobRun #123 started at 2026-04-26 02:00 and failed with exit code 2.
```

### Schedule

A recurring trigger rule that creates JobRuns.

Example:

```text
Run Job #5 every Monday at 02:00 Europe/Vienna.
```

## Schedule Fields

Initial fields:

- Id
- CustomerId
- JobId
- Name
- CronExpression
- TimeZone
- IsEnabled
- MisfirePolicy
- LastTriggeredAt
- NextRunAt
- CreatedAt
- UpdatedAt

## Trigger Types

JobRun should distinguish:

- Manual
- Scheduled
- System

## Execution Model

Scheduled and manual jobs must use the same execution path.

```text
Manual Run:
User -> API -> JobRun Queued -> Worker -> Execute

Scheduled Run:
Quartz -> Worker -> JobRun Queued -> Worker -> Execute
```

The actual Ansible execution logic must not be duplicated.

## Cron Validation

When a Schedule is created or updated:

1. Validate cron expression.
2. Validate time zone.
3. Calculate next run preview.
4. Store normalized configuration.
5. Register/update Quartz trigger.

## Misfire Policy

MVP can start simple.

Possible values:

- IgnoreMisfire
- FireOnceNow
- SkipMissedRuns

Recommended MVP default:

```text
SkipMissedRuns
```

Reason:

If NodeControl was offline, it should not unexpectedly run a large backlog of maintenance jobs.

## Concurrency Rules

MVP should prevent the same Job from running concurrently unless explicitly allowed later.

Initial rule:

```text
A Job cannot have two active JobRuns at the same time.
```

Potential future field:

```text
Job.AllowConcurrentRuns
```

## Worker Responsibility

The Worker:

- Hosts Quartz scheduler
- Reacts to scheduled triggers
- Creates scheduled JobRuns
- Executes queued JobRuns
- Updates statuses
- Writes logs
- Writes audit entries

## API Responsibility

The API:

- Creates schedules
- Updates schedules
- Enables/disables schedules
- Shows schedule previews
- Does not execute jobs directly

## Frontend Requirements

The schedule UI should show:

- Name
- Job
- Cron expression
- Time zone
- Enabled/disabled state
- Next run time
- Last triggered time
- Preview of upcoming runs

## Post-MVP Scheduling Features

Potential later features:

- Calendar exclusions
- Maintenance windows
- Approval before scheduled run
- Notification on failure
- Retry policy per job
- Maximum runtime per job
- Concurrency configuration
- Schedule templates
