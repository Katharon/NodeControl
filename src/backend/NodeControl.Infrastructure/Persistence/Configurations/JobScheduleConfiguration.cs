using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using NodeControl.Domain.Customers;
using NodeControl.Domain.Jobs;

namespace NodeControl.Infrastructure.Persistence.Configurations;

public sealed class JobScheduleConfiguration : IEntityTypeConfiguration<JobSchedule>
{
    public void Configure(EntityTypeBuilder<JobSchedule> builder)
    {
        builder.ToTable("job_schedules");

        builder.HasKey(schedule => schedule.Id);

        builder.Property(schedule => schedule.Id)
            .HasColumnName("id")
            .ValueGeneratedNever();

        builder.Property(schedule => schedule.CustomerId)
            .HasColumnName("customer_id")
            .IsRequired();

        builder.Property(schedule => schedule.JobId)
            .HasColumnName("job_id")
            .IsRequired();

        builder.Property(schedule => schedule.Name)
            .HasColumnName("name")
            .HasMaxLength(200)
            .IsRequired();

        builder.Property(schedule => schedule.Slug)
            .HasColumnName("slug")
            .HasMaxLength(100)
            .IsRequired();

        builder.Property(schedule => schedule.Description)
            .HasColumnName("description")
            .HasMaxLength(1000);

        builder.Property(schedule => schedule.CronExpression)
            .HasColumnName("cron_expression")
            .HasMaxLength(120)
            .IsRequired();

        builder.Property(schedule => schedule.TimeZoneId)
            .HasColumnName("time_zone_id")
            .HasMaxLength(100)
            .IsRequired();

        builder.Property(schedule => schedule.Status)
            .HasColumnName("status")
            .HasConversion<string>()
            .HasMaxLength(40)
            .IsRequired();

        builder.Property(schedule => schedule.NextRunAtUtc)
            .HasColumnName("next_run_at_utc");

        builder.Property(schedule => schedule.LastRunAtUtc)
            .HasColumnName("last_run_at_utc");

        builder.Property(schedule => schedule.LastJobRunId)
            .HasColumnName("last_job_run_id");

        builder.Property(schedule => schedule.CreatedAt)
            .HasColumnName("created_at")
            .IsRequired();

        builder.Property(schedule => schedule.UpdatedAt)
            .HasColumnName("updated_at");

        builder.Property(schedule => schedule.ArchivedAt)
            .HasColumnName("archived_at");

        builder.HasOne<Customer>()
            .WithMany()
            .HasForeignKey(schedule => schedule.CustomerId)
            .OnDelete(DeleteBehavior.Restrict)
            .IsRequired();

        builder.HasOne(schedule => schedule.Job)
            .WithMany()
            .HasForeignKey(schedule => schedule.JobId)
            .OnDelete(DeleteBehavior.Restrict)
            .IsRequired();

        builder.HasOne<JobRun>()
            .WithMany()
            .HasForeignKey(schedule => schedule.LastJobRunId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasIndex(schedule => new { schedule.CustomerId, schedule.Slug })
            .IsUnique()
            .HasDatabaseName("ux_job_schedules_customer_id_slug");

        builder.HasIndex(schedule => new { schedule.Status, schedule.NextRunAtUtc })
            .HasDatabaseName("ix_job_schedules_status_next_run_at_utc");
    }
}
