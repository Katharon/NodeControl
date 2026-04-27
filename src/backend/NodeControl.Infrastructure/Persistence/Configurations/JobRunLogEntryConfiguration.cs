using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using NodeControl.Domain.Jobs;

namespace NodeControl.Infrastructure.Persistence.Configurations;

public sealed class JobRunLogEntryConfiguration : IEntityTypeConfiguration<JobRunLogEntry>
{
    public void Configure(EntityTypeBuilder<JobRunLogEntry> builder)
    {
        builder.ToTable("job_run_log_entries");

        builder.HasKey(entry => entry.Id);

        builder.Property(entry => entry.Id)
            .HasColumnName("id")
            .ValueGeneratedNever();

        builder.Property(entry => entry.JobRunId)
            .HasColumnName("job_run_id")
            .IsRequired();

        builder.Property(entry => entry.Sequence)
            .HasColumnName("sequence")
            .IsRequired();

        builder.Property(entry => entry.TimestampUtc)
            .HasColumnName("timestamp_utc")
            .IsRequired();

        builder.Property(entry => entry.Stream)
            .HasColumnName("stream")
            .HasConversion<string>()
            .HasMaxLength(40)
            .IsRequired();

        builder.Property(entry => entry.Level)
            .HasColumnName("level")
            .HasConversion<string>()
            .HasMaxLength(40)
            .IsRequired();

        builder.Property(entry => entry.Message)
            .HasColumnName("message")
            .HasMaxLength(16000)
            .IsRequired();

        builder.HasOne(entry => entry.JobRun)
            .WithMany()
            .HasForeignKey(entry => entry.JobRunId)
            .OnDelete(DeleteBehavior.Cascade)
            .IsRequired();

        builder.HasIndex(entry => new { entry.JobRunId, entry.Sequence })
            .IsUnique()
            .HasDatabaseName("ux_job_run_log_entries_job_run_id_sequence");
    }
}
