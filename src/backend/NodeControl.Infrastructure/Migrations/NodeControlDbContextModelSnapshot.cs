using System;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Storage.ValueConversion;
using NodeControl.Infrastructure.Persistence;
using Npgsql.EntityFrameworkCore.PostgreSQL.Metadata;

#nullable disable

namespace NodeControl.Infrastructure.Migrations;

[DbContext(typeof(NodeControlDbContext))]
partial class NodeControlDbContextModelSnapshot : ModelSnapshot
{
    protected override void BuildModel(ModelBuilder modelBuilder)
    {
        modelBuilder
            .HasAnnotation("ProductVersion", "10.0.4")
            .HasAnnotation("Relational:MaxIdentifierLength", 63);

        NpgsqlModelBuilderExtensions.UseIdentityByDefaultColumns(modelBuilder);

        modelBuilder.Entity("NodeControl.Domain.Users.ExternalIdentity", b =>
        {
            b.Property<Guid>("Id")
                .ValueGeneratedNever()
                .HasColumnType("uuid")
                .HasColumnName("id");

            b.Property<DateTimeOffset>("CreatedAt")
                .HasColumnType("timestamp with time zone")
                .HasColumnName("created_at");

            b.Property<string>("DisplayNameAtLogin")
                .IsRequired()
                .HasMaxLength(200)
                .HasColumnType("character varying(200)")
                .HasColumnName("display_name_at_login");

            b.Property<string>("EmailAtLogin")
                .IsRequired()
                .HasMaxLength(320)
                .HasColumnType("character varying(320)")
                .HasColumnName("email_at_login");

            b.Property<DateTimeOffset>("LastSeenAt")
                .HasColumnType("timestamp with time zone")
                .HasColumnName("last_seen_at");

            b.Property<string>("Provider")
                .IsRequired()
                .HasMaxLength(100)
                .HasColumnType("character varying(100)")
                .HasColumnName("provider");

            b.Property<string>("Subject")
                .IsRequired()
                .HasMaxLength(300)
                .HasColumnType("character varying(300)")
                .HasColumnName("subject");

            b.Property<Guid>("UserId")
                .HasColumnType("uuid")
                .HasColumnName("user_id");

            b.HasKey("Id")
                .HasName("pk_external_identities");

            b.HasIndex("UserId")
                .HasDatabaseName("ix_external_identities_user_id");

            b.HasIndex("Provider", "Subject")
                .IsUnique()
                .HasDatabaseName("ux_external_identities_provider_subject");

            b.ToTable("external_identities", (string)null);
        });

        modelBuilder.Entity("NodeControl.Domain.Users.User", b =>
        {
            b.Property<Guid>("Id")
                .ValueGeneratedNever()
                .HasColumnType("uuid")
                .HasColumnName("id");

            b.Property<DateTimeOffset>("CreatedAt")
                .HasColumnType("timestamp with time zone")
                .HasColumnName("created_at");

            b.Property<string>("DisplayName")
                .IsRequired()
                .HasMaxLength(200)
                .HasColumnType("character varying(200)")
                .HasColumnName("display_name");

            b.Property<string>("Email")
                .IsRequired()
                .HasMaxLength(320)
                .HasColumnType("character varying(320)")
                .HasColumnName("email");

            b.Property<bool>("IsActive")
                .HasColumnType("boolean")
                .HasColumnName("is_active");

            b.Property<bool>("IsPlatformAdmin")
                .HasColumnType("boolean")
                .HasColumnName("is_platform_admin");

            b.Property<DateTimeOffset?>("LastLoginAt")
                .HasColumnType("timestamp with time zone")
                .HasColumnName("last_login_at");

            b.Property<string>("NormalizedEmail")
                .IsRequired()
                .HasMaxLength(320)
                .HasColumnType("character varying(320)")
                .HasColumnName("normalized_email");

            b.Property<DateTimeOffset?>("UpdatedAt")
                .HasColumnType("timestamp with time zone")
                .HasColumnName("updated_at");

            b.HasKey("Id")
                .HasName("pk_users");

            b.HasIndex("NormalizedEmail")
                .HasDatabaseName("ix_users_normalized_email");

            b.ToTable("users", (string)null);
        });

        modelBuilder.Entity("NodeControl.Domain.Users.ExternalIdentity", b =>
        {
            b.HasOne("NodeControl.Domain.Users.User", "User")
                .WithMany()
                .HasForeignKey("UserId")
                .OnDelete(DeleteBehavior.Restrict)
                .IsRequired()
                .HasConstraintName("fk_external_identities_users_user_id");

            b.Navigation("User");
        });
    }
}
