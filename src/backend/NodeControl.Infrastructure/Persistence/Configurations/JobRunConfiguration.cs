using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using NodeControl.Domain.Customers;
using NodeControl.Domain.Jobs;
using NodeControl.Domain.Users;

namespace NodeControl.Infrastructure.Persistence.Configurations;

public sealed class JobRunConfiguration : IEntityTypeConfiguration<JobRun>
{
    public void Configure(EntityTypeBuilder<JobRun> builder)
    {
        builder.ToTable("job_runs");

        builder.HasKey(jobRun => jobRun.Id);

        builder.Property(jobRun => jobRun.Id)
            .HasColumnName("id")
            .ValueGeneratedNever();

        builder.Property(jobRun => jobRun.CustomerId)
            .HasColumnName("customer_id")
            .IsRequired();

        builder.Property(jobRun => jobRun.JobId)
            .HasColumnName("job_id")
            .IsRequired();

        builder.Property(jobRun => jobRun.TriggerType)
            .HasColumnName("trigger_type")
            .HasConversion<string>()
            .HasMaxLength(40)
            .IsRequired();

        builder.Property(jobRun => jobRun.TriggeredByUserId)
            .HasColumnName("triggered_by_user_id");

        builder.Property(jobRun => jobRun.ScheduleId)
            .HasColumnName("schedule_id");

        builder.Property(jobRun => jobRun.RetriedFromJobRunId)
            .HasColumnName("retried_from_job_run_id");

        builder.Property(jobRun => jobRun.RetryAttempt)
            .HasColumnName("retry_attempt")
            .IsRequired();

        builder.Property(jobRun => jobRun.Status)
            .HasColumnName("status")
            .HasConversion<string>()
            .HasMaxLength(40)
            .IsRequired();

        builder.Property(jobRun => jobRun.QueuedAt)
            .HasColumnName("queued_at")
            .IsRequired();

        builder.Property(jobRun => jobRun.StartedAt)
            .HasColumnName("started_at");

        builder.Property(jobRun => jobRun.FinishedAt)
            .HasColumnName("finished_at");

        builder.Property(jobRun => jobRun.ExitCode)
            .HasColumnName("exit_code");

        builder.Property(jobRun => jobRun.ErrorMessage)
            .HasColumnName("error_message")
            .HasMaxLength(4000);

        builder.Property(jobRun => jobRun.WorkspacePath)
            .HasColumnName("workspace_path")
            .HasMaxLength(1000);

        builder.Property(jobRun => jobRun.StdoutLogPath)
            .HasColumnName("stdout_log_path")
            .HasMaxLength(1000);

        builder.Property(jobRun => jobRun.StderrLogPath)
            .HasColumnName("stderr_log_path")
            .HasMaxLength(1000);

        builder.Property(jobRun => jobRun.CancellationRequestedAtUtc)
            .HasColumnName("cancellation_requested_at_utc");

        builder.Property(jobRun => jobRun.CancellationRequestedByUserId)
            .HasColumnName("cancellation_requested_by_user_id");

        builder.Property(jobRun => jobRun.CancellationReason)
            .HasColumnName("cancellation_reason")
            .HasMaxLength(1000);

        builder.Property(jobRun => jobRun.CreatedAt)
            .HasColumnName("created_at")
            .IsRequired();

        builder.HasOne<Customer>()
            .WithMany()
            .HasForeignKey(jobRun => jobRun.CustomerId)
            .OnDelete(DeleteBehavior.Restrict)
            .IsRequired();

        builder.HasOne<Job>()
            .WithMany()
            .HasForeignKey(jobRun => jobRun.JobId)
            .OnDelete(DeleteBehavior.Restrict)
            .IsRequired();

        builder.HasOne<User>()
            .WithMany()
            .HasForeignKey(jobRun => jobRun.TriggeredByUserId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasOne<User>()
            .WithMany()
            .HasForeignKey(jobRun => jobRun.CancellationRequestedByUserId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasOne<JobRun>()
            .WithMany()
            .HasForeignKey(jobRun => jobRun.RetriedFromJobRunId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasIndex(jobRun => new { jobRun.CustomerId, jobRun.CreatedAt })
            .HasDatabaseName("ix_job_runs_customer_id_created_at");

        builder.HasIndex(jobRun => new { jobRun.Status, jobRun.QueuedAt })
            .HasDatabaseName("ix_job_runs_status_queued_at");
    }
}
