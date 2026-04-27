using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using NodeControl.Domain.Customers;
using NodeControl.Domain.Secrets;

namespace NodeControl.Infrastructure.Persistence.Configurations;

public sealed class SecretConfiguration : IEntityTypeConfiguration<Secret>
{
    public void Configure(EntityTypeBuilder<Secret> builder)
    {
        builder.ToTable("secrets");

        builder.HasKey(secret => secret.Id);

        builder.Property(secret => secret.Id)
            .HasColumnName("id")
            .ValueGeneratedNever();

        builder.Property(secret => secret.CustomerId)
            .HasColumnName("customer_id")
            .IsRequired();

        builder.Property(secret => secret.Name)
            .HasColumnName("name")
            .HasMaxLength(200)
            .IsRequired();

        builder.Property(secret => secret.Slug)
            .HasColumnName("slug")
            .HasMaxLength(100)
            .IsRequired();

        builder.Property(secret => secret.Description)
            .HasColumnName("description")
            .HasMaxLength(1000);

        builder.Property(secret => secret.Kind)
            .HasColumnName("kind")
            .HasConversion<string>()
            .HasMaxLength(40)
            .IsRequired();

        builder.Property(secret => secret.ProtectedValue)
            .HasColumnName("protected_value")
            .HasMaxLength(200000)
            .IsRequired();

        builder.Property(secret => secret.Status)
            .HasColumnName("status")
            .HasConversion<string>()
            .HasMaxLength(40)
            .IsRequired();

        builder.Property(secret => secret.LastRotatedAtUtc)
            .HasColumnName("last_rotated_at_utc");

        builder.Property(secret => secret.CreatedAt)
            .HasColumnName("created_at")
            .IsRequired();

        builder.Property(secret => secret.UpdatedAt)
            .HasColumnName("updated_at");

        builder.Property(secret => secret.ArchivedAt)
            .HasColumnName("archived_at");

        builder.HasOne<Customer>()
            .WithMany()
            .HasForeignKey(secret => secret.CustomerId)
            .OnDelete(DeleteBehavior.Restrict)
            .IsRequired();

        builder.HasIndex(secret => new { secret.CustomerId, secret.Slug })
            .IsUnique()
            .HasDatabaseName("ux_secrets_customer_id_slug");

        builder.HasIndex(secret => new { secret.CustomerId, secret.Status })
            .HasDatabaseName("ix_secrets_customer_id_status");
    }
}
