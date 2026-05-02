using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using NodeControl.Domain.Customers;
using NodeControl.Domain.Nodes;
using NodeControl.Domain.Secrets;

namespace NodeControl.Infrastructure.Persistence.Configurations;

public sealed class ManagedNodeConfiguration : IEntityTypeConfiguration<ManagedNode>
{
    public void Configure(EntityTypeBuilder<ManagedNode> builder)
    {
        builder.ToTable("managed_nodes");

        builder.HasKey(managedNode => managedNode.Id);

        builder.Property(managedNode => managedNode.Id)
            .HasColumnName("id")
            .ValueGeneratedNever();

        builder.Property(managedNode => managedNode.CustomerId)
            .HasColumnName("customer_id")
            .IsRequired();

        builder.Property(managedNode => managedNode.Name)
            .HasColumnName("name")
            .HasMaxLength(200)
            .IsRequired();

        builder.Property(managedNode => managedNode.Hostname)
            .HasColumnName("hostname")
            .HasMaxLength(253)
            .IsRequired();

        builder.Property(managedNode => managedNode.SshPort)
            .HasColumnName("ssh_port")
            .IsRequired();

        builder.Property(managedNode => managedNode.SshUsername)
            .HasColumnName("ssh_username")
            .HasMaxLength(100);

        builder.Property(managedNode => managedNode.SshPrivateKeySecretId)
            .HasColumnName("ssh_private_key_secret_id");

        builder.Property(managedNode => managedNode.OperatingSystem)
            .HasColumnName("operating_system")
            .HasMaxLength(100);

        builder.Property(managedNode => managedNode.Environment)
            .HasColumnName("environment")
            .HasMaxLength(100);

        builder.Property(managedNode => managedNode.Description)
            .HasColumnName("description")
            .HasMaxLength(1000);

        builder.Property(managedNode => managedNode.Status)
            .HasColumnName("status")
            .HasConversion<string>()
            .HasMaxLength(40)
            .IsRequired();

        builder.Property(managedNode => managedNode.CreatedAt)
            .HasColumnName("created_at")
            .IsRequired();

        builder.Property(managedNode => managedNode.UpdatedAt)
            .HasColumnName("updated_at");

        builder.Property(managedNode => managedNode.ArchivedAt)
            .HasColumnName("archived_at");

        builder.HasOne<Customer>()
            .WithMany()
            .HasForeignKey(managedNode => managedNode.CustomerId)
            .OnDelete(DeleteBehavior.Restrict)
            .IsRequired();

        builder.HasOne<Secret>()
            .WithMany()
            .HasForeignKey(managedNode => managedNode.SshPrivateKeySecretId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasIndex(managedNode => new { managedNode.CustomerId, managedNode.Name })
            .IsUnique()
            .HasDatabaseName("ux_managed_nodes_customer_id_name");

        builder.HasIndex(managedNode => managedNode.SshPrivateKeySecretId)
            .HasDatabaseName("ix_managed_nodes_ssh_private_key_secret_id");
    }
}
