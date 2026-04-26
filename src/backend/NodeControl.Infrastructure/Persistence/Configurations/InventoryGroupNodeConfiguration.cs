using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using NodeControl.Domain.Inventories;

namespace NodeControl.Infrastructure.Persistence.Configurations;

public sealed class InventoryGroupNodeConfiguration : IEntityTypeConfiguration<InventoryGroupNode>
{
    public void Configure(EntityTypeBuilder<InventoryGroupNode> builder)
    {
        builder.ToTable("inventory_group_nodes");

        builder.HasKey(link => new { link.InventoryGroupId, link.ManagedNodeId });

        builder.Property(link => link.InventoryGroupId)
            .HasColumnName("inventory_group_id")
            .IsRequired();

        builder.Property(link => link.ManagedNodeId)
            .HasColumnName("managed_node_id")
            .IsRequired();

        builder.Property(link => link.CreatedAt)
            .HasColumnName("created_at")
            .IsRequired();

        builder.HasOne<InventoryGroup>()
            .WithMany()
            .HasForeignKey(link => link.InventoryGroupId)
            .OnDelete(DeleteBehavior.Restrict)
            .IsRequired();

        builder.HasOne<NodeControl.Domain.Nodes.ManagedNode>()
            .WithMany()
            .HasForeignKey(link => link.ManagedNodeId)
            .OnDelete(DeleteBehavior.Restrict)
            .IsRequired();
    }
}
