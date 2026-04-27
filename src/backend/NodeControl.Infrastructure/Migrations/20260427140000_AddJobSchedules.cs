using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace NodeControl.Infrastructure.Migrations;

public partial class AddJobSchedules : Migration
{
    protected override void Up(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.CreateTable(
            name: "job_schedules",
            columns: table => new
            {
                id = table.Column<Guid>(type: "uuid", nullable: false),
                customer_id = table.Column<Guid>(type: "uuid", nullable: false),
                job_id = table.Column<Guid>(type: "uuid", nullable: false),
                name = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: false),
                slug = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: false),
                description = table.Column<string>(type: "character varying(1000)", maxLength: 1000, nullable: true),
                cron_expression = table.Column<string>(type: "character varying(120)", maxLength: 120, nullable: false),
                time_zone_id = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: false),
                status = table.Column<string>(type: "character varying(40)", maxLength: 40, nullable: false),
                next_run_at_utc = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true),
                last_run_at_utc = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true),
                last_job_run_id = table.Column<Guid>(type: "uuid", nullable: true),
                created_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                updated_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true),
                archived_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true)
            },
            constraints: table =>
            {
                table.PrimaryKey("pk_job_schedules", x => x.id);
                table.ForeignKey(
                    name: "fk_job_schedules_customers_customer_id",
                    column: x => x.customer_id,
                    principalTable: "customers",
                    principalColumn: "id",
                    onDelete: ReferentialAction.Restrict);
                table.ForeignKey(
                    name: "fk_job_schedules_job_runs_last_job_run_id",
                    column: x => x.last_job_run_id,
                    principalTable: "job_runs",
                    principalColumn: "id",
                    onDelete: ReferentialAction.Restrict);
                table.ForeignKey(
                    name: "fk_job_schedules_jobs_job_id",
                    column: x => x.job_id,
                    principalTable: "jobs",
                    principalColumn: "id",
                    onDelete: ReferentialAction.Restrict);
            });

        migrationBuilder.CreateIndex(
            name: "ix_job_schedules_job_id",
            table: "job_schedules",
            column: "job_id");

        migrationBuilder.CreateIndex(
            name: "ix_job_schedules_last_job_run_id",
            table: "job_schedules",
            column: "last_job_run_id");

        migrationBuilder.CreateIndex(
            name: "ix_job_schedules_status_next_run_at_utc",
            table: "job_schedules",
            columns: new[] { "status", "next_run_at_utc" });

        migrationBuilder.CreateIndex(
            name: "ux_job_schedules_customer_id_slug",
            table: "job_schedules",
            columns: new[] { "customer_id", "slug" },
            unique: true);
    }

    protected override void Down(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.DropTable(name: "job_schedules");
    }
}
