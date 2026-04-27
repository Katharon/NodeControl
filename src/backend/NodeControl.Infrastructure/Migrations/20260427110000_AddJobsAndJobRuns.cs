using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace NodeControl.Infrastructure.Migrations;

public partial class AddJobsAndJobRuns : Migration
{
    protected override void Up(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.CreateTable(
            name: "jobs",
            columns: table => new
            {
                id = table.Column<Guid>(type: "uuid", nullable: false),
                customer_id = table.Column<Guid>(type: "uuid", nullable: false),
                name = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: false),
                slug = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: false),
                description = table.Column<string>(type: "character varying(1000)", maxLength: 1000, nullable: true),
                control_node_id = table.Column<Guid>(type: "uuid", nullable: false),
                inventory_group_id = table.Column<Guid>(type: "uuid", nullable: false),
                playbook_id = table.Column<Guid>(type: "uuid", nullable: false),
                variable_set_id = table.Column<Guid>(type: "uuid", nullable: true),
                status = table.Column<string>(type: "character varying(40)", maxLength: 40, nullable: false),
                default_timeout_seconds = table.Column<int>(type: "integer", nullable: false),
                created_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                updated_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true),
                archived_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true)
            },
            constraints: table =>
            {
                table.PrimaryKey("pk_jobs", x => x.id);
                table.ForeignKey(
                    name: "fk_jobs_control_nodes_control_node_id",
                    column: x => x.control_node_id,
                    principalTable: "control_nodes",
                    principalColumn: "id",
                    onDelete: ReferentialAction.Restrict);
                table.ForeignKey(
                    name: "fk_jobs_customers_customer_id",
                    column: x => x.customer_id,
                    principalTable: "customers",
                    principalColumn: "id",
                    onDelete: ReferentialAction.Restrict);
                table.ForeignKey(
                    name: "fk_jobs_inventory_groups_inventory_group_id",
                    column: x => x.inventory_group_id,
                    principalTable: "inventory_groups",
                    principalColumn: "id",
                    onDelete: ReferentialAction.Restrict);
                table.ForeignKey(
                    name: "fk_jobs_playbooks_playbook_id",
                    column: x => x.playbook_id,
                    principalTable: "playbooks",
                    principalColumn: "id",
                    onDelete: ReferentialAction.Restrict);
                table.ForeignKey(
                    name: "fk_jobs_variable_sets_variable_set_id",
                    column: x => x.variable_set_id,
                    principalTable: "variable_sets",
                    principalColumn: "id",
                    onDelete: ReferentialAction.Restrict);
            });

        migrationBuilder.CreateTable(
            name: "job_runs",
            columns: table => new
            {
                id = table.Column<Guid>(type: "uuid", nullable: false),
                customer_id = table.Column<Guid>(type: "uuid", nullable: false),
                job_id = table.Column<Guid>(type: "uuid", nullable: false),
                trigger_type = table.Column<string>(type: "character varying(40)", maxLength: 40, nullable: false),
                triggered_by_user_id = table.Column<Guid>(type: "uuid", nullable: true),
                schedule_id = table.Column<Guid>(type: "uuid", nullable: true),
                status = table.Column<string>(type: "character varying(40)", maxLength: 40, nullable: false),
                queued_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                started_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true),
                finished_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true),
                exit_code = table.Column<int>(type: "integer", nullable: true),
                error_message = table.Column<string>(type: "character varying(4000)", maxLength: 4000, nullable: true),
                created_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false)
            },
            constraints: table =>
            {
                table.PrimaryKey("pk_job_runs", x => x.id);
                table.ForeignKey(
                    name: "fk_job_runs_customers_customer_id",
                    column: x => x.customer_id,
                    principalTable: "customers",
                    principalColumn: "id",
                    onDelete: ReferentialAction.Restrict);
                table.ForeignKey(
                    name: "fk_job_runs_jobs_job_id",
                    column: x => x.job_id,
                    principalTable: "jobs",
                    principalColumn: "id",
                    onDelete: ReferentialAction.Restrict);
                table.ForeignKey(
                    name: "fk_job_runs_users_triggered_by_user_id",
                    column: x => x.triggered_by_user_id,
                    principalTable: "users",
                    principalColumn: "id",
                    onDelete: ReferentialAction.Restrict);
            });

        migrationBuilder.CreateIndex(
            name: "ix_job_runs_customer_id_created_at",
            table: "job_runs",
            columns: new[] { "customer_id", "created_at" });

        migrationBuilder.CreateIndex(
            name: "ix_job_runs_job_id",
            table: "job_runs",
            column: "job_id");

        migrationBuilder.CreateIndex(
            name: "ix_job_runs_triggered_by_user_id",
            table: "job_runs",
            column: "triggered_by_user_id");

        migrationBuilder.CreateIndex(
            name: "ix_jobs_control_node_id",
            table: "jobs",
            column: "control_node_id");

        migrationBuilder.CreateIndex(
            name: "ix_jobs_inventory_group_id",
            table: "jobs",
            column: "inventory_group_id");

        migrationBuilder.CreateIndex(
            name: "ix_jobs_playbook_id",
            table: "jobs",
            column: "playbook_id");

        migrationBuilder.CreateIndex(
            name: "ix_jobs_variable_set_id",
            table: "jobs",
            column: "variable_set_id");

        migrationBuilder.CreateIndex(
            name: "ux_jobs_customer_id_slug",
            table: "jobs",
            columns: new[] { "customer_id", "slug" },
            unique: true);
    }

    protected override void Down(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.DropTable(name: "job_runs");

        migrationBuilder.DropTable(name: "jobs");
    }
}
