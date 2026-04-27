using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using NodeControl.Domain.Audit;

namespace NodeControl.Infrastructure.Persistence.Configurations;

public sealed class AuditLogEntryConfiguration : IEntityTypeConfiguration<AuditLogEntry>
{
    public void Configure(EntityTypeBuilder<AuditLogEntry> builder)
    {
        builder.ToTable("audit_log_entries");

        builder.HasKey(entry => entry.Id);

        builder.Property(entry => entry.Id)
            .HasColumnName("id")
            .ValueGeneratedNever();

        builder.Property(entry => entry.CustomerId)
            .HasColumnName("customer_id");

        builder.Property(entry => entry.ActorUserId)
            .HasColumnName("actor_user_id");

        builder.Property(entry => entry.ActorDisplayName)
            .HasColumnName("actor_display_name")
            .HasMaxLength(AuditLogEntry.ActorDisplayNameMaxLength);

        builder.Property(entry => entry.ActorType)
            .HasColumnName("actor_type")
            .HasConversion<string>()
            .HasMaxLength(40)
            .IsRequired();

        builder.Property(entry => entry.Action)
            .HasColumnName("action")
            .HasMaxLength(AuditLogEntry.ActionMaxLength)
            .IsRequired();

        builder.Property(entry => entry.EntityType)
            .HasColumnName("entity_type")
            .HasMaxLength(AuditLogEntry.EntityTypeMaxLength)
            .IsRequired();

        builder.Property(entry => entry.EntityId)
            .HasColumnName("entity_id");

        builder.Property(entry => entry.EntityDisplayName)
            .HasColumnName("entity_display_name")
            .HasMaxLength(AuditLogEntry.EntityDisplayNameMaxLength);

        builder.Property(entry => entry.Outcome)
            .HasColumnName("outcome")
            .HasConversion<string>()
            .HasMaxLength(40)
            .IsRequired();

        builder.Property(entry => entry.Message)
            .HasColumnName("message")
            .HasMaxLength(AuditLogEntry.MessageMaxLength)
            .IsRequired();

        builder.Property(entry => entry.MetadataJson)
            .HasColumnName("metadata_json")
            .HasMaxLength(AuditLogEntry.MetadataJsonMaxLength);

        builder.Property(entry => entry.IpAddress)
            .HasColumnName("ip_address")
            .HasMaxLength(AuditLogEntry.IpAddressMaxLength);

        builder.Property(entry => entry.UserAgent)
            .HasColumnName("user_agent")
            .HasMaxLength(AuditLogEntry.UserAgentMaxLength);

        builder.Property(entry => entry.CreatedAtUtc)
            .HasColumnName("created_at_utc")
            .IsRequired();

        builder.HasIndex(entry => new { entry.CustomerId, entry.CreatedAtUtc })
            .HasDatabaseName("ix_audit_log_entries_customer_id_created_at_utc");

        builder.HasIndex(entry => new { entry.CustomerId, entry.EntityType, entry.EntityId })
            .HasDatabaseName("ix_audit_log_entries_customer_id_entity");

        builder.HasIndex(entry => new { entry.CustomerId, entry.Action })
            .HasDatabaseName("ix_audit_log_entries_customer_id_action");

        builder.HasIndex(entry => new { entry.ActorUserId, entry.CreatedAtUtc })
            .HasDatabaseName("ix_audit_log_entries_actor_user_id_created_at_utc");
    }
}
