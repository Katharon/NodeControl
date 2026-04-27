using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using NodeControl.Domain.Customers;
using NodeControl.Domain.VariableSets;

namespace NodeControl.Infrastructure.Persistence.Configurations;

public sealed class VariableSetConfiguration : IEntityTypeConfiguration<VariableSet>
{
    public void Configure(EntityTypeBuilder<VariableSet> builder)
    {
        builder.ToTable("variable_sets");

        builder.HasKey(variableSet => variableSet.Id);

        builder.Property(variableSet => variableSet.Id)
            .HasColumnName("id")
            .ValueGeneratedNever();

        builder.Property(variableSet => variableSet.CustomerId)
            .HasColumnName("customer_id")
            .IsRequired();

        builder.Property(variableSet => variableSet.Name)
            .HasColumnName("name")
            .HasMaxLength(200)
            .IsRequired();

        builder.Property(variableSet => variableSet.Slug)
            .HasColumnName("slug")
            .HasMaxLength(100)
            .IsRequired();

        builder.Property(variableSet => variableSet.Description)
            .HasColumnName("description")
            .HasMaxLength(1000);

        builder.Property(variableSet => variableSet.Format)
            .HasColumnName("format")
            .HasConversion<string>()
            .HasMaxLength(40)
            .IsRequired();

        builder.Property(variableSet => variableSet.Content)
            .HasColumnName("content")
            .HasMaxLength(200000)
            .IsRequired();

        builder.Property(variableSet => variableSet.ContainsSensitiveValues)
            .HasColumnName("contains_sensitive_values")
            .IsRequired();

        builder.Property(variableSet => variableSet.Status)
            .HasColumnName("status")
            .HasConversion<string>()
            .HasMaxLength(40)
            .IsRequired();

        builder.Property(variableSet => variableSet.CreatedAt)
            .HasColumnName("created_at")
            .IsRequired();

        builder.Property(variableSet => variableSet.UpdatedAt)
            .HasColumnName("updated_at");

        builder.Property(variableSet => variableSet.ArchivedAt)
            .HasColumnName("archived_at");

        builder.HasOne<Customer>()
            .WithMany()
            .HasForeignKey(variableSet => variableSet.CustomerId)
            .OnDelete(DeleteBehavior.Restrict)
            .IsRequired();

        builder.HasIndex(variableSet => new { variableSet.CustomerId, variableSet.Slug })
            .IsUnique()
            .HasDatabaseName("ux_variable_sets_customer_id_slug");
    }
}
