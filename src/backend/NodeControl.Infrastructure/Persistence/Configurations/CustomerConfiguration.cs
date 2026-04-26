using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using NodeControl.Domain.Customers;

namespace NodeControl.Infrastructure.Persistence.Configurations;

public sealed class CustomerConfiguration : IEntityTypeConfiguration<Customer>
{
    public void Configure(EntityTypeBuilder<Customer> builder)
    {
        builder.ToTable("customers");

        builder.HasKey(customer => customer.Id);

        builder.Property(customer => customer.Id)
            .HasColumnName("id")
            .ValueGeneratedNever();

        builder.Property(customer => customer.Name)
            .HasColumnName("name")
            .HasMaxLength(200)
            .IsRequired();

        builder.Property(customer => customer.Slug)
            .HasColumnName("slug")
            .HasMaxLength(100)
            .IsRequired();

        builder.Property(customer => customer.Description)
            .HasColumnName("description")
            .HasMaxLength(1000);

        builder.Property(customer => customer.Status)
            .HasColumnName("status")
            .HasConversion<string>()
            .HasMaxLength(40)
            .IsRequired();

        builder.Property(customer => customer.CreatedAt)
            .HasColumnName("created_at")
            .IsRequired();

        builder.Property(customer => customer.UpdatedAt)
            .HasColumnName("updated_at");

        builder.Property(customer => customer.ArchivedAt)
            .HasColumnName("archived_at");

        builder.HasIndex(customer => customer.Slug)
            .IsUnique()
            .HasDatabaseName("ux_customers_slug");
    }
}
