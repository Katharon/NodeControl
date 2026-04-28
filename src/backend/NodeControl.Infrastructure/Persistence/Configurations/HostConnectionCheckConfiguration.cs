using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using NodeControl.Domain.Customers;
using NodeControl.Domain.Nodes;
using NodeControl.Domain.Users;

namespace NodeControl.Infrastructure.Persistence.Configurations;

public sealed class HostConnectionCheckConfiguration : IEntityTypeConfiguration<HostConnectionCheck>
{
    public void Configure(EntityTypeBuilder<HostConnectionCheck> builder)
    {
        builder.ToTable("host_connection_checks", table =>
        {
            table.HasCheckConstraint(
                "ck_host_connection_checks_target_reference",
                "((target_type = 'ControlNode' AND control_node_id IS NOT NULL AND managed_node_id IS NULL) OR (target_type = 'ManagedNode' AND managed_node_id IS NOT NULL AND control_node_id IS NULL))");
            table.HasCheckConstraint(
                "ck_host_connection_checks_port",
                "port >= 1 AND port <= 65535");
        });

        builder.HasKey(check => check.Id);

        builder.Property(check => check.Id)
            .HasColumnName("id")
            .ValueGeneratedNever();

        builder.Property(check => check.CustomerId)
            .HasColumnName("customer_id")
            .IsRequired();

        builder.Property(check => check.TargetType)
            .HasColumnName("target_type")
            .HasConversion<string>()
            .HasMaxLength(40)
            .IsRequired();

        builder.Property(check => check.ControlNodeId)
            .HasColumnName("control_node_id");

        builder.Property(check => check.ManagedNodeId)
            .HasColumnName("managed_node_id");

        builder.Property(check => check.Hostname)
            .HasColumnName("hostname")
            .HasMaxLength(253)
            .IsRequired();

        builder.Property(check => check.Port)
            .HasColumnName("port")
            .IsRequired();

        builder.Property(check => check.Status)
            .HasColumnName("status")
            .HasConversion<string>()
            .HasMaxLength(40)
            .IsRequired();

        builder.Property(check => check.RequestedByUserId)
            .HasColumnName("requested_by_user_id");

        builder.Property(check => check.QueuedAtUtc)
            .HasColumnName("queued_at_utc")
            .IsRequired();

        builder.Property(check => check.StartedAtUtc)
            .HasColumnName("started_at_utc");

        builder.Property(check => check.FinishedAtUtc)
            .HasColumnName("finished_at_utc");

        builder.Property(check => check.DurationMs)
            .HasColumnName("duration_ms");

        builder.Property(check => check.ResultMessage)
            .HasColumnName("result_message")
            .HasMaxLength(2000);

        builder.Property(check => check.ErrorMessage)
            .HasColumnName("error_message")
            .HasMaxLength(4000);

        builder.HasOne<Customer>()
            .WithMany()
            .HasForeignKey(check => check.CustomerId)
            .OnDelete(DeleteBehavior.Restrict)
            .IsRequired();

        builder.HasOne<ControlNode>()
            .WithMany()
            .HasForeignKey(check => check.ControlNodeId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasOne<ManagedNode>()
            .WithMany()
            .HasForeignKey(check => check.ManagedNodeId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasOne<User>()
            .WithMany()
            .HasForeignKey(check => check.RequestedByUserId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasIndex(check => new { check.CustomerId, check.QueuedAtUtc })
            .HasDatabaseName("ix_host_connection_checks_customer_id_queued_at_utc");

        builder.HasIndex(check => new { check.Status, check.QueuedAtUtc })
            .HasDatabaseName("ix_host_connection_checks_status_queued_at_utc");

        builder.HasIndex(check => new { check.CustomerId, check.TargetType })
            .HasDatabaseName("ix_host_connection_checks_customer_id_target_type");

        builder.HasIndex(check => check.ControlNodeId)
            .HasDatabaseName("ix_host_connection_checks_control_node_id");

        builder.HasIndex(check => check.ManagedNodeId)
            .HasDatabaseName("ix_host_connection_checks_managed_node_id");
    }
}
