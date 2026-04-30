using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using NodeControl.Domain.Customers;
using NodeControl.Domain.GitRepositories;

namespace NodeControl.Infrastructure.Persistence.Configurations;

public sealed class GitRepositoryConfiguration : IEntityTypeConfiguration<GitRepository>
{
    public void Configure(EntityTypeBuilder<GitRepository> builder)
    {
        builder.ToTable("git_repositories");

        builder.HasKey(repository => repository.Id);

        builder.Property(repository => repository.Id)
            .HasColumnName("id")
            .ValueGeneratedNever();

        builder.Property(repository => repository.CustomerId)
            .HasColumnName("customer_id")
            .IsRequired();

        builder.Property(repository => repository.Name)
            .HasColumnName("name")
            .HasMaxLength(200)
            .IsRequired();

        builder.Property(repository => repository.RepositoryUrl)
            .HasColumnName("repository_url")
            .HasMaxLength(1000)
            .IsRequired();

        builder.Property(repository => repository.Branch)
            .HasColumnName("branch")
            .HasMaxLength(200);

        builder.Property(repository => repository.Revision)
            .HasColumnName("revision")
            .HasMaxLength(200);

        builder.Property(repository => repository.Subpath)
            .HasColumnName("subpath")
            .HasMaxLength(500);

        builder.Property(repository => repository.Status)
            .HasColumnName("status")
            .HasConversion<string>()
            .HasMaxLength(40)
            .IsRequired();

        builder.Property(repository => repository.CreatedAt)
            .HasColumnName("created_at")
            .IsRequired();

        builder.Property(repository => repository.UpdatedAt)
            .HasColumnName("updated_at");

        builder.Property(repository => repository.ArchivedAt)
            .HasColumnName("archived_at");

        builder.HasOne<Customer>()
            .WithMany()
            .HasForeignKey(repository => repository.CustomerId)
            .OnDelete(DeleteBehavior.Restrict)
            .IsRequired();

        builder.HasIndex(repository => new { repository.CustomerId, repository.Status })
            .HasDatabaseName("ix_git_repositories_customer_id_status");
    }
}
