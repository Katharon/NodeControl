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

        modelBuilder.Entity("NodeControl.Domain.Audit.AuditLogEntry", b =>
        {
            b.Property<Guid>("Id")
                .ValueGeneratedNever()
                .HasColumnType("uuid")
                .HasColumnName("id");

            b.Property<string>("Action")
                .IsRequired()
                .HasMaxLength(200)
                .HasColumnType("character varying(200)")
                .HasColumnName("action");

            b.Property<string>("ActorDisplayName")
                .HasMaxLength(300)
                .HasColumnType("character varying(300)")
                .HasColumnName("actor_display_name");

            b.Property<string>("ActorType")
                .IsRequired()
                .HasMaxLength(40)
                .HasColumnType("character varying(40)")
                .HasColumnName("actor_type");

            b.Property<Guid?>("ActorUserId")
                .HasColumnType("uuid")
                .HasColumnName("actor_user_id");

            b.Property<DateTimeOffset>("CreatedAtUtc")
                .HasColumnType("timestamp with time zone")
                .HasColumnName("created_at_utc");

            b.Property<Guid?>("CustomerId")
                .HasColumnType("uuid")
                .HasColumnName("customer_id");

            b.Property<string>("EntityDisplayName")
                .HasMaxLength(300)
                .HasColumnType("character varying(300)")
                .HasColumnName("entity_display_name");

            b.Property<Guid?>("EntityId")
                .HasColumnType("uuid")
                .HasColumnName("entity_id");

            b.Property<string>("EntityType")
                .IsRequired()
                .HasMaxLength(100)
                .HasColumnType("character varying(100)")
                .HasColumnName("entity_type");

            b.Property<string>("IpAddress")
                .HasMaxLength(100)
                .HasColumnType("character varying(100)")
                .HasColumnName("ip_address");

            b.Property<string>("Message")
                .IsRequired()
                .HasMaxLength(1000)
                .HasColumnType("character varying(1000)")
                .HasColumnName("message");

            b.Property<string>("MetadataJson")
                .HasMaxLength(8000)
                .HasColumnType("character varying(8000)")
                .HasColumnName("metadata_json");

            b.Property<string>("Outcome")
                .IsRequired()
                .HasMaxLength(40)
                .HasColumnType("character varying(40)")
                .HasColumnName("outcome");

            b.Property<string>("UserAgent")
                .HasMaxLength(500)
                .HasColumnType("character varying(500)")
                .HasColumnName("user_agent");

            b.HasKey("Id")
                .HasName("pk_audit_log_entries");

            b.HasIndex("ActorUserId", "CreatedAtUtc")
                .HasDatabaseName("ix_audit_log_entries_actor_user_id_created_at_utc");

            b.HasIndex("CustomerId", "Action")
                .HasDatabaseName("ix_audit_log_entries_customer_id_action");

            b.HasIndex("CustomerId", "CreatedAtUtc")
                .HasDatabaseName("ix_audit_log_entries_customer_id_created_at_utc");

            b.HasIndex("CustomerId", "EntityType", "EntityId")
                .HasDatabaseName("ix_audit_log_entries_customer_id_entity");

            b.ToTable("audit_log_entries", (string)null);
        });

        modelBuilder.Entity("NodeControl.Domain.Customers.Customer", b =>
        {
            b.Property<Guid>("Id")
                .ValueGeneratedNever()
                .HasColumnType("uuid")
                .HasColumnName("id");

            b.Property<DateTimeOffset?>("ArchivedAt")
                .HasColumnType("timestamp with time zone")
                .HasColumnName("archived_at");

            b.Property<DateTimeOffset>("CreatedAt")
                .HasColumnType("timestamp with time zone")
                .HasColumnName("created_at");

            b.Property<string>("Description")
                .HasMaxLength(1000)
                .HasColumnType("character varying(1000)")
                .HasColumnName("description");

            b.Property<string>("Name")
                .IsRequired()
                .HasMaxLength(200)
                .HasColumnType("character varying(200)")
                .HasColumnName("name");

            b.Property<string>("Slug")
                .IsRequired()
                .HasMaxLength(100)
                .HasColumnType("character varying(100)")
                .HasColumnName("slug");

            b.Property<string>("Status")
                .IsRequired()
                .HasMaxLength(40)
                .HasColumnType("character varying(40)")
                .HasColumnName("status");

            b.Property<DateTimeOffset?>("UpdatedAt")
                .HasColumnType("timestamp with time zone")
                .HasColumnName("updated_at");

            b.HasKey("Id")
                .HasName("pk_customers");

            b.HasIndex("Slug")
                .IsUnique()
                .HasDatabaseName("ux_customers_slug");

            b.ToTable("customers", (string)null);
        });

        modelBuilder.Entity("NodeControl.Domain.Customers.CustomerMembership", b =>
        {
            b.Property<Guid>("Id")
                .ValueGeneratedNever()
                .HasColumnType("uuid")
                .HasColumnName("id");

            b.Property<Guid>("CustomerId")
                .HasColumnType("uuid")
                .HasColumnName("customer_id");

            b.Property<DateTimeOffset>("CreatedAt")
                .HasColumnType("timestamp with time zone")
                .HasColumnName("created_at");

            b.Property<DateTimeOffset?>("DeactivatedAt")
                .HasColumnType("timestamp with time zone")
                .HasColumnName("deactivated_at");

            b.Property<bool>("IsActive")
                .HasColumnType("boolean")
                .HasColumnName("is_active");

            b.Property<string>("Role")
                .IsRequired()
                .HasMaxLength(40)
                .HasColumnType("character varying(40)")
                .HasColumnName("role");

            b.Property<DateTimeOffset?>("UpdatedAt")
                .HasColumnType("timestamp with time zone")
                .HasColumnName("updated_at");

            b.Property<Guid>("UserId")
                .HasColumnType("uuid")
                .HasColumnName("user_id");

            b.HasKey("Id")
                .HasName("pk_customer_memberships");

            b.HasIndex("CustomerId")
                .HasDatabaseName("ix_customer_memberships_customer_id");

            b.HasIndex("UserId")
                .HasDatabaseName("ix_customer_memberships_user_id");

            b.HasIndex("CustomerId", "UserId")
                .IsUnique()
                .HasDatabaseName("ux_customer_memberships_customer_id_user_id");

            b.ToTable("customer_memberships", (string)null);
        });

        modelBuilder.Entity("NodeControl.Domain.Inventories.InventoryGroup", b =>
        {
            b.Property<Guid>("Id")
                .ValueGeneratedNever()
                .HasColumnType("uuid")
                .HasColumnName("id");

            b.Property<DateTimeOffset?>("ArchivedAt")
                .HasColumnType("timestamp with time zone")
                .HasColumnName("archived_at");

            b.Property<Guid>("CustomerId")
                .HasColumnType("uuid")
                .HasColumnName("customer_id");

            b.Property<DateTimeOffset>("CreatedAt")
                .HasColumnType("timestamp with time zone")
                .HasColumnName("created_at");

            b.Property<string>("Description")
                .HasMaxLength(1000)
                .HasColumnType("character varying(1000)")
                .HasColumnName("description");

            b.Property<string>("Name")
                .IsRequired()
                .HasMaxLength(100)
                .HasColumnType("character varying(100)")
                .HasColumnName("name");

            b.Property<DateTimeOffset?>("UpdatedAt")
                .HasColumnType("timestamp with time zone")
                .HasColumnName("updated_at");

            b.HasKey("Id")
                .HasName("pk_inventory_groups");

            b.HasIndex("CustomerId", "Name")
                .IsUnique()
                .HasDatabaseName("ux_inventory_groups_customer_id_name");

            b.ToTable("inventory_groups", (string)null);
        });

        modelBuilder.Entity("NodeControl.Domain.Inventories.InventoryGroupNode", b =>
        {
            b.Property<Guid>("InventoryGroupId")
                .HasColumnType("uuid")
                .HasColumnName("inventory_group_id");

            b.Property<Guid>("ManagedNodeId")
                .HasColumnType("uuid")
                .HasColumnName("managed_node_id");

            b.Property<DateTimeOffset>("CreatedAt")
                .HasColumnType("timestamp with time zone")
                .HasColumnName("created_at");

            b.HasKey("InventoryGroupId", "ManagedNodeId")
                .HasName("pk_inventory_group_nodes");

            b.HasIndex("ManagedNodeId")
                .HasDatabaseName("ix_inventory_group_nodes_managed_node_id");

            b.ToTable("inventory_group_nodes", (string)null);
        });

        modelBuilder.Entity("NodeControl.Domain.Jobs.Job", b =>
        {
            b.Property<Guid>("Id")
                .ValueGeneratedNever()
                .HasColumnType("uuid")
                .HasColumnName("id");

            b.Property<DateTimeOffset?>("ArchivedAt")
                .HasColumnType("timestamp with time zone")
                .HasColumnName("archived_at");

            b.Property<Guid>("ControlNodeId")
                .HasColumnType("uuid")
                .HasColumnName("control_node_id");

            b.Property<DateTimeOffset>("CreatedAt")
                .HasColumnType("timestamp with time zone")
                .HasColumnName("created_at");

            b.Property<DateTimeOffset?>("CancellationRequestedAtUtc")
                .HasColumnType("timestamp with time zone")
                .HasColumnName("cancellation_requested_at_utc");

            b.Property<Guid?>("CancellationRequestedByUserId")
                .HasColumnType("uuid")
                .HasColumnName("cancellation_requested_by_user_id");

            b.Property<string>("CancellationReason")
                .HasMaxLength(1000)
                .HasColumnType("character varying(1000)")
                .HasColumnName("cancellation_reason");

            b.Property<Guid>("CustomerId")
                .HasColumnType("uuid")
                .HasColumnName("customer_id");

            b.Property<int>("DefaultTimeoutSeconds")
                .HasColumnType("integer")
                .HasColumnName("default_timeout_seconds");

            b.Property<string>("Description")
                .HasMaxLength(1000)
                .HasColumnType("character varying(1000)")
                .HasColumnName("description");

            b.Property<Guid>("InventoryGroupId")
                .HasColumnType("uuid")
                .HasColumnName("inventory_group_id");

            b.Property<string>("Name")
                .IsRequired()
                .HasMaxLength(200)
                .HasColumnType("character varying(200)")
                .HasColumnName("name");

            b.Property<Guid>("PlaybookId")
                .HasColumnType("uuid")
                .HasColumnName("playbook_id");

            b.Property<string>("Slug")
                .IsRequired()
                .HasMaxLength(100)
                .HasColumnType("character varying(100)")
                .HasColumnName("slug");

            b.Property<string>("Status")
                .IsRequired()
                .HasMaxLength(40)
                .HasColumnType("character varying(40)")
                .HasColumnName("status");

            b.Property<DateTimeOffset?>("UpdatedAt")
                .HasColumnType("timestamp with time zone")
                .HasColumnName("updated_at");

            b.Property<Guid?>("VariableSetId")
                .HasColumnType("uuid")
                .HasColumnName("variable_set_id");

            b.HasKey("Id")
                .HasName("pk_jobs");

            b.HasIndex("ControlNodeId")
                .HasDatabaseName("ix_jobs_control_node_id");

            b.HasIndex("InventoryGroupId")
                .HasDatabaseName("ix_jobs_inventory_group_id");

            b.HasIndex("PlaybookId")
                .HasDatabaseName("ix_jobs_playbook_id");

            b.HasIndex("VariableSetId")
                .HasDatabaseName("ix_jobs_variable_set_id");

            b.HasIndex("CustomerId", "Slug")
                .IsUnique()
                .HasDatabaseName("ux_jobs_customer_id_slug");

            b.ToTable("jobs", (string)null);
        });

        modelBuilder.Entity("NodeControl.Domain.Jobs.JobSchedule", b =>
        {
            b.Property<Guid>("Id")
                .ValueGeneratedNever()
                .HasColumnType("uuid")
                .HasColumnName("id");

            b.Property<DateTimeOffset?>("ArchivedAt")
                .HasColumnType("timestamp with time zone")
                .HasColumnName("archived_at");

            b.Property<string>("CronExpression")
                .IsRequired()
                .HasMaxLength(120)
                .HasColumnType("character varying(120)")
                .HasColumnName("cron_expression");

            b.Property<DateTimeOffset>("CreatedAt")
                .HasColumnType("timestamp with time zone")
                .HasColumnName("created_at");

            b.Property<Guid>("CustomerId")
                .HasColumnType("uuid")
                .HasColumnName("customer_id");

            b.Property<string>("Description")
                .HasMaxLength(1000)
                .HasColumnType("character varying(1000)")
                .HasColumnName("description");

            b.Property<Guid>("JobId")
                .HasColumnType("uuid")
                .HasColumnName("job_id");

            b.Property<Guid?>("LastJobRunId")
                .HasColumnType("uuid")
                .HasColumnName("last_job_run_id");

            b.Property<DateTimeOffset?>("LastRunAtUtc")
                .HasColumnType("timestamp with time zone")
                .HasColumnName("last_run_at_utc");

            b.Property<string>("Name")
                .IsRequired()
                .HasMaxLength(200)
                .HasColumnType("character varying(200)")
                .HasColumnName("name");

            b.Property<DateTimeOffset?>("NextRunAtUtc")
                .HasColumnType("timestamp with time zone")
                .HasColumnName("next_run_at_utc");

            b.Property<string>("Slug")
                .IsRequired()
                .HasMaxLength(100)
                .HasColumnType("character varying(100)")
                .HasColumnName("slug");

            b.Property<string>("Status")
                .IsRequired()
                .HasMaxLength(40)
                .HasColumnType("character varying(40)")
                .HasColumnName("status");

            b.Property<string>("TimeZoneId")
                .IsRequired()
                .HasMaxLength(100)
                .HasColumnType("character varying(100)")
                .HasColumnName("time_zone_id");

            b.Property<DateTimeOffset?>("UpdatedAt")
                .HasColumnType("timestamp with time zone")
                .HasColumnName("updated_at");

            b.HasKey("Id")
                .HasName("pk_job_schedules");

            b.HasIndex("JobId")
                .HasDatabaseName("ix_job_schedules_job_id");

            b.HasIndex("LastJobRunId")
                .HasDatabaseName("ix_job_schedules_last_job_run_id");

            b.HasIndex("Status", "NextRunAtUtc")
                .HasDatabaseName("ix_job_schedules_status_next_run_at_utc");

            b.HasIndex("CustomerId", "Slug")
                .IsUnique()
                .HasDatabaseName("ux_job_schedules_customer_id_slug");

            b.ToTable("job_schedules", (string)null);
        });

        modelBuilder.Entity("NodeControl.Domain.Jobs.JobRun", b =>
        {
            b.Property<Guid>("Id")
                .ValueGeneratedNever()
                .HasColumnType("uuid")
                .HasColumnName("id");

            b.Property<DateTimeOffset>("CreatedAt")
                .HasColumnType("timestamp with time zone")
                .HasColumnName("created_at");

            b.Property<Guid>("CustomerId")
                .HasColumnType("uuid")
                .HasColumnName("customer_id");

            b.Property<string>("ErrorMessage")
                .HasMaxLength(4000)
                .HasColumnType("character varying(4000)")
                .HasColumnName("error_message");

            b.Property<int?>("ExitCode")
                .HasColumnType("integer")
                .HasColumnName("exit_code");

            b.Property<DateTimeOffset?>("FinishedAt")
                .HasColumnType("timestamp with time zone")
                .HasColumnName("finished_at");

            b.Property<Guid>("JobId")
                .HasColumnType("uuid")
                .HasColumnName("job_id");

            b.Property<DateTimeOffset>("QueuedAt")
                .HasColumnType("timestamp with time zone")
                .HasColumnName("queued_at");

            b.Property<Guid?>("RetriedFromJobRunId")
                .HasColumnType("uuid")
                .HasColumnName("retried_from_job_run_id");

            b.Property<int>("RetryAttempt")
                .HasColumnType("integer")
                .HasColumnName("retry_attempt");

            b.Property<Guid?>("ScheduleId")
                .HasColumnType("uuid")
                .HasColumnName("schedule_id");

            b.Property<DateTimeOffset?>("StartedAt")
                .HasColumnType("timestamp with time zone")
                .HasColumnName("started_at");

            b.Property<string>("StderrLogPath")
                .HasMaxLength(1000)
                .HasColumnType("character varying(1000)")
                .HasColumnName("stderr_log_path");

            b.Property<string>("StdoutLogPath")
                .HasMaxLength(1000)
                .HasColumnType("character varying(1000)")
                .HasColumnName("stdout_log_path");

            b.Property<string>("Status")
                .IsRequired()
                .HasMaxLength(40)
                .HasColumnType("character varying(40)")
                .HasColumnName("status");

            b.Property<Guid?>("TriggeredByUserId")
                .HasColumnType("uuid")
                .HasColumnName("triggered_by_user_id");

            b.Property<string>("TriggerType")
                .IsRequired()
                .HasMaxLength(40)
                .HasColumnType("character varying(40)")
                .HasColumnName("trigger_type");

            b.Property<string>("WorkspacePath")
                .HasMaxLength(1000)
                .HasColumnType("character varying(1000)")
                .HasColumnName("workspace_path");

            b.HasKey("Id")
                .HasName("pk_job_runs");

            b.HasIndex("CancellationRequestedByUserId")
                .HasDatabaseName("ix_job_runs_cancellation_requested_by_user_id");

            b.HasIndex("JobId")
                .HasDatabaseName("ix_job_runs_job_id");

            b.HasIndex("RetriedFromJobRunId")
                .HasDatabaseName("ix_job_runs_retried_from_job_run_id");

            b.HasIndex("Status", "QueuedAt")
                .HasDatabaseName("ix_job_runs_status_queued_at");

            b.HasIndex("TriggeredByUserId")
                .HasDatabaseName("ix_job_runs_triggered_by_user_id");

            b.HasIndex("CustomerId", "CreatedAt")
                .HasDatabaseName("ix_job_runs_customer_id_created_at");

            b.ToTable("job_runs", (string)null);
        });

        modelBuilder.Entity("NodeControl.Domain.Jobs.JobRunLogEntry", b =>
        {
            b.Property<Guid>("Id")
                .ValueGeneratedNever()
                .HasColumnType("uuid")
                .HasColumnName("id");

            b.Property<Guid>("JobRunId")
                .HasColumnType("uuid")
                .HasColumnName("job_run_id");

            b.Property<string>("Level")
                .IsRequired()
                .HasMaxLength(40)
                .HasColumnType("character varying(40)")
                .HasColumnName("level");

            b.Property<string>("Message")
                .IsRequired()
                .HasMaxLength(16000)
                .HasColumnType("character varying(16000)")
                .HasColumnName("message");

            b.Property<long>("Sequence")
                .HasColumnType("bigint")
                .HasColumnName("sequence");

            b.Property<string>("Stream")
                .IsRequired()
                .HasMaxLength(40)
                .HasColumnType("character varying(40)")
                .HasColumnName("stream");

            b.Property<DateTimeOffset>("TimestampUtc")
                .HasColumnType("timestamp with time zone")
                .HasColumnName("timestamp_utc");

            b.HasKey("Id")
                .HasName("pk_job_run_log_entries");

            b.HasIndex("JobRunId", "Sequence")
                .IsUnique()
                .HasDatabaseName("ux_job_run_log_entries_job_run_id_sequence");

            b.ToTable("job_run_log_entries", (string)null);
        });

        modelBuilder.Entity("NodeControl.Domain.Nodes.ControlNode", b =>
        {
            b.Property<Guid>("Id")
                .ValueGeneratedNever()
                .HasColumnType("uuid")
                .HasColumnName("id");

            b.Property<DateTimeOffset?>("ArchivedAt")
                .HasColumnType("timestamp with time zone")
                .HasColumnName("archived_at");

            b.Property<DateTimeOffset>("CreatedAt")
                .HasColumnType("timestamp with time zone")
                .HasColumnName("created_at");

            b.Property<Guid>("CustomerId")
                .HasColumnType("uuid")
                .HasColumnName("customer_id");

            b.Property<string>("Description")
                .HasMaxLength(1000)
                .HasColumnType("character varying(1000)")
                .HasColumnName("description");

            b.Property<string>("Hostname")
                .IsRequired()
                .HasMaxLength(253)
                .HasColumnType("character varying(253)")
                .HasColumnName("hostname");

            b.Property<string>("Name")
                .IsRequired()
                .HasMaxLength(200)
                .HasColumnType("character varying(200)")
                .HasColumnName("name");

            b.Property<int>("SshPort")
                .HasColumnType("integer")
                .HasColumnName("ssh_port");

            b.Property<string>("Status")
                .IsRequired()
                .HasMaxLength(40)
                .HasColumnType("character varying(40)")
                .HasColumnName("status");

            b.Property<DateTimeOffset?>("UpdatedAt")
                .HasColumnType("timestamp with time zone")
                .HasColumnName("updated_at");

            b.HasKey("Id")
                .HasName("pk_control_nodes");

            b.HasIndex("CustomerId", "Name")
                .IsUnique()
                .HasDatabaseName("ux_control_nodes_customer_id_name");

            b.ToTable("control_nodes", (string)null);
        });

        modelBuilder.Entity("NodeControl.Domain.Nodes.ManagedNode", b =>
        {
            b.Property<Guid>("Id")
                .ValueGeneratedNever()
                .HasColumnType("uuid")
                .HasColumnName("id");

            b.Property<DateTimeOffset?>("ArchivedAt")
                .HasColumnType("timestamp with time zone")
                .HasColumnName("archived_at");

            b.Property<DateTimeOffset>("CreatedAt")
                .HasColumnType("timestamp with time zone")
                .HasColumnName("created_at");

            b.Property<Guid>("CustomerId")
                .HasColumnType("uuid")
                .HasColumnName("customer_id");

            b.Property<string>("Description")
                .HasMaxLength(1000)
                .HasColumnType("character varying(1000)")
                .HasColumnName("description");

            b.Property<string>("Environment")
                .HasMaxLength(100)
                .HasColumnType("character varying(100)")
                .HasColumnName("environment");

            b.Property<string>("Hostname")
                .IsRequired()
                .HasMaxLength(253)
                .HasColumnType("character varying(253)")
                .HasColumnName("hostname");

            b.Property<string>("Name")
                .IsRequired()
                .HasMaxLength(200)
                .HasColumnType("character varying(200)")
                .HasColumnName("name");

            b.Property<string>("OperatingSystem")
                .HasMaxLength(100)
                .HasColumnType("character varying(100)")
                .HasColumnName("operating_system");

            b.Property<int>("SshPort")
                .HasColumnType("integer")
                .HasColumnName("ssh_port");

            b.Property<string>("Status")
                .IsRequired()
                .HasMaxLength(40)
                .HasColumnType("character varying(40)")
                .HasColumnName("status");

            b.Property<DateTimeOffset?>("UpdatedAt")
                .HasColumnType("timestamp with time zone")
                .HasColumnName("updated_at");

            b.HasKey("Id")
                .HasName("pk_managed_nodes");

            b.HasIndex("CustomerId", "Name")
                .IsUnique()
                .HasDatabaseName("ux_managed_nodes_customer_id_name");

            b.ToTable("managed_nodes", (string)null);
        });

        modelBuilder.Entity("NodeControl.Domain.Playbooks.Playbook", b =>
        {
            b.Property<Guid>("Id")
                .ValueGeneratedNever()
                .HasColumnType("uuid")
                .HasColumnName("id");

            b.Property<DateTimeOffset?>("ArchivedAt")
                .HasColumnType("timestamp with time zone")
                .HasColumnName("archived_at");

            b.Property<DateTimeOffset>("CreatedAt")
                .HasColumnType("timestamp with time zone")
                .HasColumnName("created_at");

            b.Property<Guid>("CustomerId")
                .HasColumnType("uuid")
                .HasColumnName("customer_id");

            b.Property<string>("Description")
                .HasMaxLength(1000)
                .HasColumnType("character varying(1000)")
                .HasColumnName("description");

            b.Property<string>("EntryFilePath")
                .HasMaxLength(500)
                .HasColumnType("character varying(500)")
                .HasColumnName("entry_file_path");

            b.Property<string>("InlineContent")
                .HasMaxLength(200000)
                .HasColumnType("character varying(200000)")
                .HasColumnName("inline_content");

            b.Property<string>("Name")
                .IsRequired()
                .HasMaxLength(200)
                .HasColumnType("character varying(200)")
                .HasColumnName("name");

            b.Property<string>("Slug")
                .IsRequired()
                .HasMaxLength(100)
                .HasColumnType("character varying(100)")
                .HasColumnName("slug");

            b.Property<string>("SourceType")
                .IsRequired()
                .HasMaxLength(40)
                .HasColumnType("character varying(40)")
                .HasColumnName("source_type");

            b.Property<string>("Status")
                .IsRequired()
                .HasMaxLength(40)
                .HasColumnType("character varying(40)")
                .HasColumnName("status");

            b.Property<DateTimeOffset?>("UpdatedAt")
                .HasColumnType("timestamp with time zone")
                .HasColumnName("updated_at");

            b.HasKey("Id")
                .HasName("pk_playbooks");

            b.HasIndex("CustomerId", "Slug")
                .IsUnique()
                .HasDatabaseName("ux_playbooks_customer_id_slug");

            b.ToTable("playbooks", (string)null);
        });

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

        modelBuilder.Entity("NodeControl.Domain.VariableSets.VariableSet", b =>
        {
            b.Property<Guid>("Id")
                .ValueGeneratedNever()
                .HasColumnType("uuid")
                .HasColumnName("id");

            b.Property<bool>("ContainsSensitiveValues")
                .HasColumnType("boolean")
                .HasColumnName("contains_sensitive_values");

            b.Property<string>("Content")
                .IsRequired()
                .HasMaxLength(200000)
                .HasColumnType("character varying(200000)")
                .HasColumnName("content");

            b.Property<DateTimeOffset>("CreatedAt")
                .HasColumnType("timestamp with time zone")
                .HasColumnName("created_at");

            b.Property<Guid>("CustomerId")
                .HasColumnType("uuid")
                .HasColumnName("customer_id");

            b.Property<string>("Description")
                .HasMaxLength(1000)
                .HasColumnType("character varying(1000)")
                .HasColumnName("description");

            b.Property<string>("Format")
                .IsRequired()
                .HasMaxLength(40)
                .HasColumnType("character varying(40)")
                .HasColumnName("format");

            b.Property<string>("Name")
                .IsRequired()
                .HasMaxLength(200)
                .HasColumnType("character varying(200)")
                .HasColumnName("name");

            b.Property<string>("Slug")
                .IsRequired()
                .HasMaxLength(100)
                .HasColumnType("character varying(100)")
                .HasColumnName("slug");

            b.Property<string>("Status")
                .IsRequired()
                .HasMaxLength(40)
                .HasColumnType("character varying(40)")
                .HasColumnName("status");

            b.Property<DateTimeOffset?>("ArchivedAt")
                .HasColumnType("timestamp with time zone")
                .HasColumnName("archived_at");

            b.Property<DateTimeOffset?>("UpdatedAt")
                .HasColumnType("timestamp with time zone")
                .HasColumnName("updated_at");

            b.HasKey("Id")
                .HasName("pk_variable_sets");

            b.HasIndex("CustomerId", "Slug")
                .IsUnique()
                .HasDatabaseName("ux_variable_sets_customer_id_slug");

            b.ToTable("variable_sets", (string)null);
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

        modelBuilder.Entity("NodeControl.Domain.Customers.CustomerMembership", b =>
        {
            b.HasOne("NodeControl.Domain.Customers.Customer", "Customer")
                .WithMany()
                .HasForeignKey("CustomerId")
                .OnDelete(DeleteBehavior.Restrict)
                .IsRequired()
                .HasConstraintName("fk_customer_memberships_customers_customer_id");

            b.HasOne("NodeControl.Domain.Users.User", "User")
                .WithMany()
                .HasForeignKey("UserId")
                .OnDelete(DeleteBehavior.Restrict)
                .IsRequired()
                .HasConstraintName("fk_customer_memberships_users_user_id");

            b.Navigation("Customer");

            b.Navigation("User");
        });

        modelBuilder.Entity("NodeControl.Domain.Inventories.InventoryGroup", b =>
        {
            b.HasOne("NodeControl.Domain.Customers.Customer", null)
                .WithMany()
                .HasForeignKey("CustomerId")
                .OnDelete(DeleteBehavior.Restrict)
                .IsRequired()
                .HasConstraintName("fk_inventory_groups_customers_customer_id");
        });

        modelBuilder.Entity("NodeControl.Domain.Inventories.InventoryGroupNode", b =>
        {
            b.HasOne("NodeControl.Domain.Inventories.InventoryGroup", null)
                .WithMany()
                .HasForeignKey("InventoryGroupId")
                .OnDelete(DeleteBehavior.Restrict)
                .IsRequired()
                .HasConstraintName("fk_inventory_group_nodes_inventory_groups_inventory_group_id");

            b.HasOne("NodeControl.Domain.Nodes.ManagedNode", null)
                .WithMany()
                .HasForeignKey("ManagedNodeId")
                .OnDelete(DeleteBehavior.Restrict)
                .IsRequired()
                .HasConstraintName("fk_inventory_group_nodes_managed_nodes_managed_node_id");
        });

        modelBuilder.Entity("NodeControl.Domain.Jobs.Job", b =>
        {
            b.HasOne("NodeControl.Domain.Nodes.ControlNode", null)
                .WithMany()
                .HasForeignKey("ControlNodeId")
                .OnDelete(DeleteBehavior.Restrict)
                .IsRequired()
                .HasConstraintName("fk_jobs_control_nodes_control_node_id");

            b.HasOne("NodeControl.Domain.Customers.Customer", null)
                .WithMany()
                .HasForeignKey("CustomerId")
                .OnDelete(DeleteBehavior.Restrict)
                .IsRequired()
                .HasConstraintName("fk_jobs_customers_customer_id");

            b.HasOne("NodeControl.Domain.Inventories.InventoryGroup", null)
                .WithMany()
                .HasForeignKey("InventoryGroupId")
                .OnDelete(DeleteBehavior.Restrict)
                .IsRequired()
                .HasConstraintName("fk_jobs_inventory_groups_inventory_group_id");

            b.HasOne("NodeControl.Domain.Playbooks.Playbook", null)
                .WithMany()
                .HasForeignKey("PlaybookId")
                .OnDelete(DeleteBehavior.Restrict)
                .IsRequired()
                .HasConstraintName("fk_jobs_playbooks_playbook_id");

            b.HasOne("NodeControl.Domain.VariableSets.VariableSet", null)
                .WithMany()
                .HasForeignKey("VariableSetId")
                .OnDelete(DeleteBehavior.Restrict)
                .HasConstraintName("fk_jobs_variable_sets_variable_set_id");
        });

        modelBuilder.Entity("NodeControl.Domain.Jobs.JobRun", b =>
        {
            b.HasOne("NodeControl.Domain.Customers.Customer", null)
                .WithMany()
                .HasForeignKey("CustomerId")
                .OnDelete(DeleteBehavior.Restrict)
                .IsRequired()
                .HasConstraintName("fk_job_runs_customers_customer_id");

            b.HasOne("NodeControl.Domain.Jobs.Job", null)
                .WithMany()
                .HasForeignKey("JobId")
                .OnDelete(DeleteBehavior.Restrict)
                .IsRequired()
                .HasConstraintName("fk_job_runs_jobs_job_id");

            b.HasOne("NodeControl.Domain.Jobs.JobRun", null)
                .WithMany()
                .HasForeignKey("RetriedFromJobRunId")
                .OnDelete(DeleteBehavior.Restrict)
                .HasConstraintName("fk_job_runs_job_runs_retried_from_job_run_id");

            b.HasOne("NodeControl.Domain.Users.User", null)
                .WithMany()
                .HasForeignKey("CancellationRequestedByUserId")
                .OnDelete(DeleteBehavior.Restrict)
                .HasConstraintName("fk_job_runs_users_cancellation_requested_by_user_id");

            b.HasOne("NodeControl.Domain.Users.User", null)
                .WithMany()
                .HasForeignKey("TriggeredByUserId")
                .OnDelete(DeleteBehavior.Restrict)
                .HasConstraintName("fk_job_runs_users_triggered_by_user_id");
        });

        modelBuilder.Entity("NodeControl.Domain.Jobs.JobSchedule", b =>
        {
            b.HasOne("NodeControl.Domain.Customers.Customer", null)
                .WithMany()
                .HasForeignKey("CustomerId")
                .OnDelete(DeleteBehavior.Restrict)
                .IsRequired()
                .HasConstraintName("fk_job_schedules_customers_customer_id");

            b.HasOne("NodeControl.Domain.Jobs.JobRun", null)
                .WithMany()
                .HasForeignKey("LastJobRunId")
                .OnDelete(DeleteBehavior.Restrict)
                .HasConstraintName("fk_job_schedules_job_runs_last_job_run_id");

            b.HasOne("NodeControl.Domain.Jobs.Job", "Job")
                .WithMany()
                .HasForeignKey("JobId")
                .OnDelete(DeleteBehavior.Restrict)
                .IsRequired()
                .HasConstraintName("fk_job_schedules_jobs_job_id");

            b.Navigation("Job");
        });

        modelBuilder.Entity("NodeControl.Domain.Jobs.JobRunLogEntry", b =>
        {
            b.HasOne("NodeControl.Domain.Jobs.JobRun", "JobRun")
                .WithMany()
                .HasForeignKey("JobRunId")
                .OnDelete(DeleteBehavior.Cascade)
                .IsRequired()
                .HasConstraintName("fk_job_run_log_entries_job_runs_job_run_id");

            b.Navigation("JobRun");
        });

        modelBuilder.Entity("NodeControl.Domain.Nodes.ControlNode", b =>
        {
            b.HasOne("NodeControl.Domain.Customers.Customer", null)
                .WithMany()
                .HasForeignKey("CustomerId")
                .OnDelete(DeleteBehavior.Restrict)
                .IsRequired()
                .HasConstraintName("fk_control_nodes_customers_customer_id");
        });

        modelBuilder.Entity("NodeControl.Domain.Nodes.ManagedNode", b =>
        {
            b.HasOne("NodeControl.Domain.Customers.Customer", null)
                .WithMany()
                .HasForeignKey("CustomerId")
                .OnDelete(DeleteBehavior.Restrict)
                .IsRequired()
                .HasConstraintName("fk_managed_nodes_customers_customer_id");
        });

        modelBuilder.Entity("NodeControl.Domain.Playbooks.Playbook", b =>
        {
            b.HasOne("NodeControl.Domain.Customers.Customer", null)
                .WithMany()
                .HasForeignKey("CustomerId")
                .OnDelete(DeleteBehavior.Restrict)
                .IsRequired()
                .HasConstraintName("fk_playbooks_customers_customer_id");
        });

        modelBuilder.Entity("NodeControl.Domain.VariableSets.VariableSet", b =>
        {
            b.HasOne("NodeControl.Domain.Customers.Customer", null)
                .WithMany()
                .HasForeignKey("CustomerId")
                .OnDelete(DeleteBehavior.Restrict)
                .IsRequired()
                .HasConstraintName("fk_variable_sets_customers_customer_id");
        });
    }
}
