using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using NodeControl.Domain.Customers;
using NodeControl.Domain.Inventories;
using NodeControl.Domain.Jobs;
using NodeControl.Domain.Nodes;
using NodeControl.Domain.Playbooks;
using NodeControl.Domain.VariableSets;

namespace NodeControl.Infrastructure.Persistence.Configurations;

public sealed class JobConfiguration : IEntityTypeConfiguration<Job>
{
    public void Configure(EntityTypeBuilder<Job> builder)
    {
        builder.ToTable("jobs");

        builder.HasKey(job => job.Id);

        builder.Property(job => job.Id)
            .HasColumnName("id")
            .ValueGeneratedNever();

        builder.Property(job => job.CustomerId)
            .HasColumnName("customer_id")
            .IsRequired();

        builder.Property(job => job.Name)
            .HasColumnName("name")
            .HasMaxLength(200)
            .IsRequired();

        builder.Property(job => job.Slug)
            .HasColumnName("slug")
            .HasMaxLength(100)
            .IsRequired();

        builder.Property(job => job.Description)
            .HasColumnName("description")
            .HasMaxLength(1000);

        builder.Property(job => job.ControlNodeId)
            .HasColumnName("control_node_id")
            .IsRequired();

        builder.Property(job => job.InventoryGroupId)
            .HasColumnName("inventory_group_id")
            .IsRequired();

        builder.Property(job => job.PlaybookId)
            .HasColumnName("playbook_id")
            .IsRequired();

        builder.Property(job => job.VariableSetId)
            .HasColumnName("variable_set_id");

        builder.Property(job => job.Status)
            .HasColumnName("status")
            .HasConversion<string>()
            .HasMaxLength(40)
            .IsRequired();

        builder.Property(job => job.DefaultTimeoutSeconds)
            .HasColumnName("default_timeout_seconds")
            .IsRequired();

        builder.Property(job => job.CreatedAt)
            .HasColumnName("created_at")
            .IsRequired();

        builder.Property(job => job.UpdatedAt)
            .HasColumnName("updated_at");

        builder.Property(job => job.ArchivedAt)
            .HasColumnName("archived_at");

        builder.HasOne<Customer>()
            .WithMany()
            .HasForeignKey(job => job.CustomerId)
            .OnDelete(DeleteBehavior.Restrict)
            .IsRequired();

        builder.HasOne<ControlNode>()
            .WithMany()
            .HasForeignKey(job => job.ControlNodeId)
            .OnDelete(DeleteBehavior.Restrict)
            .IsRequired();

        builder.HasOne<InventoryGroup>()
            .WithMany()
            .HasForeignKey(job => job.InventoryGroupId)
            .OnDelete(DeleteBehavior.Restrict)
            .IsRequired();

        builder.HasOne<Playbook>()
            .WithMany()
            .HasForeignKey(job => job.PlaybookId)
            .OnDelete(DeleteBehavior.Restrict)
            .IsRequired();

        builder.HasOne<VariableSet>()
            .WithMany()
            .HasForeignKey(job => job.VariableSetId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasIndex(job => new { job.CustomerId, job.Slug })
            .IsUnique()
            .HasDatabaseName("ux_jobs_customer_id_slug");
    }
}
