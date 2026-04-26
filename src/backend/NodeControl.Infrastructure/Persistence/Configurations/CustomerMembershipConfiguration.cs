using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using NodeControl.Domain.Customers;

namespace NodeControl.Infrastructure.Persistence.Configurations;

public sealed class CustomerMembershipConfiguration : IEntityTypeConfiguration<CustomerMembership>
{
    public void Configure(EntityTypeBuilder<CustomerMembership> builder)
    {
        builder.ToTable("customer_memberships");

        builder.HasKey(membership => membership.Id);

        builder.Property(membership => membership.Id)
            .HasColumnName("id")
            .ValueGeneratedNever();

        builder.Property(membership => membership.CustomerId)
            .HasColumnName("customer_id")
            .IsRequired();

        builder.Property(membership => membership.UserId)
            .HasColumnName("user_id")
            .IsRequired();

        builder.Property(membership => membership.Role)
            .HasColumnName("role")
            .HasConversion<string>()
            .HasMaxLength(40)
            .IsRequired();

        builder.Property(membership => membership.IsActive)
            .HasColumnName("is_active")
            .IsRequired();

        builder.Property(membership => membership.CreatedAt)
            .HasColumnName("created_at")
            .IsRequired();

        builder.Property(membership => membership.UpdatedAt)
            .HasColumnName("updated_at");

        builder.Property(membership => membership.DeactivatedAt)
            .HasColumnName("deactivated_at");

        builder.HasOne(membership => membership.Customer)
            .WithMany()
            .HasForeignKey(membership => membership.CustomerId)
            .OnDelete(DeleteBehavior.Restrict)
            .IsRequired();

        builder.HasOne(membership => membership.User)
            .WithMany()
            .HasForeignKey(membership => membership.UserId)
            .OnDelete(DeleteBehavior.Restrict)
            .IsRequired();

        builder.HasIndex(membership => membership.CustomerId)
            .HasDatabaseName("ix_customer_memberships_customer_id");

        builder.HasIndex(membership => membership.UserId)
            .HasDatabaseName("ix_customer_memberships_user_id");

        builder.HasIndex(membership => new { membership.CustomerId, membership.UserId })
            .IsUnique()
            .HasDatabaseName("ux_customer_memberships_customer_id_user_id");
    }
}
