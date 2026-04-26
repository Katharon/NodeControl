using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using NodeControl.Domain.Customers;
using NodeControl.Domain.Inventories;

namespace NodeControl.Infrastructure.Persistence.Configurations;

public sealed class InventoryGroupConfiguration : IEntityTypeConfiguration<InventoryGroup>
{
    public void Configure(EntityTypeBuilder<InventoryGroup> builder)
    {
        builder.ToTable("inventory_groups");

        builder.HasKey(group => group.Id);

        builder.Property(group => group.Id)
            .HasColumnName("id")
            .ValueGeneratedNever();

        builder.Property(group => group.CustomerId)
            .HasColumnName("customer_id")
            .IsRequired();

        builder.Property(group => group.Name)
            .HasColumnName("name")
            .HasMaxLength(100)
            .IsRequired();

        builder.Property(group => group.Description)
            .HasColumnName("description")
            .HasMaxLength(1000);

        builder.Property(group => group.CreatedAt)
            .HasColumnName("created_at")
            .IsRequired();

        builder.Property(group => group.UpdatedAt)
            .HasColumnName("updated_at");

        builder.Property(group => group.ArchivedAt)
            .HasColumnName("archived_at");

        builder.Ignore(group => group.IsArchived);

        builder.HasOne<Customer>()
            .WithMany()
            .HasForeignKey(group => group.CustomerId)
            .OnDelete(DeleteBehavior.Restrict)
            .IsRequired();

        builder.HasIndex(group => new { group.CustomerId, group.Name })
            .IsUnique()
            .HasDatabaseName("ux_inventory_groups_customer_id_name");
    }
}
