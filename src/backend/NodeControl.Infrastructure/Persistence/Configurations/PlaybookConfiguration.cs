using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using NodeControl.Domain.Customers;
using NodeControl.Domain.Playbooks;

namespace NodeControl.Infrastructure.Persistence.Configurations;

public sealed class PlaybookConfiguration : IEntityTypeConfiguration<Playbook>
{
    public void Configure(EntityTypeBuilder<Playbook> builder)
    {
        builder.ToTable("playbooks");

        builder.HasKey(playbook => playbook.Id);

        builder.Property(playbook => playbook.Id)
            .HasColumnName("id")
            .ValueGeneratedNever();

        builder.Property(playbook => playbook.CustomerId)
            .HasColumnName("customer_id")
            .IsRequired();

        builder.Property(playbook => playbook.Name)
            .HasColumnName("name")
            .HasMaxLength(200)
            .IsRequired();

        builder.Property(playbook => playbook.Slug)
            .HasColumnName("slug")
            .HasMaxLength(100)
            .IsRequired();

        builder.Property(playbook => playbook.Description)
            .HasColumnName("description")
            .HasMaxLength(1000);

        builder.Property(playbook => playbook.SourceType)
            .HasColumnName("source_type")
            .HasConversion<string>()
            .HasMaxLength(40)
            .IsRequired();

        builder.Property(playbook => playbook.Status)
            .HasColumnName("status")
            .HasConversion<string>()
            .HasMaxLength(40)
            .IsRequired();

        builder.Property(playbook => playbook.InlineContent)
            .HasColumnName("inline_content")
            .HasMaxLength(200000);

        builder.Property(playbook => playbook.EntryFilePath)
            .HasColumnName("entry_file_path")
            .HasMaxLength(500);

        builder.Property(playbook => playbook.CreatedAt)
            .HasColumnName("created_at")
            .IsRequired();

        builder.Property(playbook => playbook.UpdatedAt)
            .HasColumnName("updated_at");

        builder.Property(playbook => playbook.ArchivedAt)
            .HasColumnName("archived_at");

        builder.HasOne<Customer>()
            .WithMany()
            .HasForeignKey(playbook => playbook.CustomerId)
            .OnDelete(DeleteBehavior.Restrict)
            .IsRequired();

        builder.HasIndex(playbook => new { playbook.CustomerId, playbook.Slug })
            .IsUnique()
            .HasDatabaseName("ux_playbooks_customer_id_slug");
    }
}
