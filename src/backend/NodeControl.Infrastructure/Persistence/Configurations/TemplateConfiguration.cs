using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using NodeControl.Domain.Customers;
using NodeControl.Domain.Templates;

namespace NodeControl.Infrastructure.Persistence.Configurations;

public sealed class TemplateConfiguration : IEntityTypeConfiguration<Template>
{
    public void Configure(EntityTypeBuilder<Template> builder)
    {
        builder.ToTable("templates");

        builder.HasKey(template => template.Id);

        builder.Property(template => template.Id)
            .HasColumnName("id")
            .ValueGeneratedNever();

        builder.Property(template => template.CustomerId)
            .HasColumnName("customer_id")
            .IsRequired();

        builder.Property(template => template.Name)
            .HasColumnName("name")
            .HasMaxLength(200)
            .IsRequired();

        builder.Property(template => template.Slug)
            .HasColumnName("slug")
            .HasMaxLength(100)
            .IsRequired();

        builder.Property(template => template.Description)
            .HasColumnName("description")
            .HasMaxLength(1000);

        builder.Property(template => template.TemplateType)
            .HasColumnName("template_type")
            .HasConversion<string>()
            .HasMaxLength(40)
            .IsRequired();

        builder.Property(template => template.Content)
            .HasColumnName("content")
            .HasMaxLength(200000)
            .IsRequired();

        builder.Property(template => template.Language)
            .HasColumnName("language")
            .HasMaxLength(100);

        builder.Property(template => template.Status)
            .HasColumnName("status")
            .HasConversion<string>()
            .HasMaxLength(40)
            .IsRequired();

        builder.Property(template => template.CreatedAt)
            .HasColumnName("created_at")
            .IsRequired();

        builder.Property(template => template.UpdatedAt)
            .HasColumnName("updated_at");

        builder.Property(template => template.ArchivedAt)
            .HasColumnName("archived_at");

        builder.HasOne<Customer>()
            .WithMany()
            .HasForeignKey(template => template.CustomerId)
            .OnDelete(DeleteBehavior.Restrict)
            .IsRequired();

        builder.HasIndex(template => new { template.CustomerId, template.Slug })
            .IsUnique()
            .HasDatabaseName("ux_templates_customer_id_slug");

        builder.HasIndex(template => new { template.CustomerId, template.Status })
            .HasDatabaseName("ix_templates_customer_id_status");

        builder.HasIndex(template => new { template.CustomerId, template.TemplateType })
            .HasDatabaseName("ix_templates_customer_id_template_type");
    }
}
