using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using NodeControl.Domain.Customers;
using NodeControl.Domain.Nodes;
using NodeControl.Domain.Secrets;

namespace NodeControl.Infrastructure.Persistence.Configurations;

public sealed class ControlNodeConfiguration : IEntityTypeConfiguration<ControlNode>
{
    public void Configure(EntityTypeBuilder<ControlNode> builder)
    {
        builder.ToTable("control_nodes");

        builder.HasKey(controlNode => controlNode.Id);

        builder.Property(controlNode => controlNode.Id)
            .HasColumnName("id")
            .ValueGeneratedNever();

        builder.Property(controlNode => controlNode.CustomerId)
            .HasColumnName("customer_id")
            .IsRequired();

        builder.Property(controlNode => controlNode.Name)
            .HasColumnName("name")
            .HasMaxLength(200)
            .IsRequired();

        builder.Property(controlNode => controlNode.Hostname)
            .HasColumnName("hostname")
            .HasMaxLength(253)
            .IsRequired();

        builder.Property(controlNode => controlNode.SshPort)
            .HasColumnName("ssh_port")
            .IsRequired();

        builder.Property(controlNode => controlNode.SshUsername)
            .HasColumnName("ssh_username")
            .HasMaxLength(100);

        builder.Property(controlNode => controlNode.SshPrivateKeySecretId)
            .HasColumnName("ssh_private_key_secret_id");

        builder.Property(controlNode => controlNode.RemoteWorkspaceRoot)
            .HasColumnName("remote_workspace_root")
            .HasMaxLength(500);

        builder.Property(controlNode => controlNode.Description)
            .HasColumnName("description")
            .HasMaxLength(1000);

        builder.Property(controlNode => controlNode.Status)
            .HasColumnName("status")
            .HasConversion<string>()
            .HasMaxLength(40)
            .IsRequired();

        builder.Property(controlNode => controlNode.CreatedAt)
            .HasColumnName("created_at")
            .IsRequired();

        builder.Property(controlNode => controlNode.UpdatedAt)
            .HasColumnName("updated_at");

        builder.Property(controlNode => controlNode.ArchivedAt)
            .HasColumnName("archived_at");

        builder.HasOne<Customer>()
            .WithMany()
            .HasForeignKey(controlNode => controlNode.CustomerId)
            .OnDelete(DeleteBehavior.Restrict)
            .IsRequired();

        builder.HasOne<Secret>()
            .WithMany()
            .HasForeignKey(controlNode => controlNode.SshPrivateKeySecretId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasIndex(controlNode => new { controlNode.CustomerId, controlNode.Name })
            .IsUnique()
            .HasDatabaseName("ux_control_nodes_customer_id_name");

        builder.HasIndex(controlNode => controlNode.SshPrivateKeySecretId)
            .HasDatabaseName("ix_control_nodes_ssh_private_key_secret_id");
    }
}
