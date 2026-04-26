using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using NodeControl.Domain.Users;

namespace NodeControl.Infrastructure.Persistence.Configurations;

public sealed class ExternalIdentityConfiguration : IEntityTypeConfiguration<ExternalIdentity>
{
    public void Configure(EntityTypeBuilder<ExternalIdentity> builder)
    {
        builder.ToTable("external_identities");

        builder.HasKey(externalIdentity => externalIdentity.Id);

        builder.Property(externalIdentity => externalIdentity.Id)
            .HasColumnName("id")
            .ValueGeneratedNever();

        builder.Property(externalIdentity => externalIdentity.UserId)
            .HasColumnName("user_id")
            .IsRequired();

        builder.Property(externalIdentity => externalIdentity.Provider)
            .HasColumnName("provider")
            .HasMaxLength(100)
            .IsRequired();

        builder.Property(externalIdentity => externalIdentity.Subject)
            .HasColumnName("subject")
            .HasMaxLength(300)
            .IsRequired();

        builder.Property(externalIdentity => externalIdentity.EmailAtLogin)
            .HasColumnName("email_at_login")
            .HasMaxLength(320)
            .IsRequired();

        builder.Property(externalIdentity => externalIdentity.DisplayNameAtLogin)
            .HasColumnName("display_name_at_login")
            .HasMaxLength(200)
            .IsRequired();

        builder.Property(externalIdentity => externalIdentity.CreatedAt)
            .HasColumnName("created_at")
            .IsRequired();

        builder.Property(externalIdentity => externalIdentity.LastSeenAt)
            .HasColumnName("last_seen_at")
            .IsRequired();

        builder.HasOne(externalIdentity => externalIdentity.User)
            .WithMany()
            .HasForeignKey(externalIdentity => externalIdentity.UserId)
            .OnDelete(DeleteBehavior.Restrict)
            .IsRequired();

        builder.HasIndex(externalIdentity => new { externalIdentity.Provider, externalIdentity.Subject })
            .IsUnique()
            .HasDatabaseName("ux_external_identities_provider_subject");
    }
}
