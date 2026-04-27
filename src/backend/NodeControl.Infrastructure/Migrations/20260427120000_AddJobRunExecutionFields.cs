using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace NodeControl.Infrastructure.Migrations;

public partial class AddJobRunExecutionFields : Migration
{
    protected override void Up(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.AddColumn<string>(
            name: "stderr_log_path",
            table: "job_runs",
            type: "character varying(1000)",
            maxLength: 1000,
            nullable: true);

        migrationBuilder.AddColumn<string>(
            name: "stdout_log_path",
            table: "job_runs",
            type: "character varying(1000)",
            maxLength: 1000,
            nullable: true);

        migrationBuilder.AddColumn<string>(
            name: "workspace_path",
            table: "job_runs",
            type: "character varying(1000)",
            maxLength: 1000,
            nullable: true);

        migrationBuilder.CreateIndex(
            name: "ix_job_runs_status_queued_at",
            table: "job_runs",
            columns: new[] { "status", "queued_at" });
    }

    protected override void Down(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.DropIndex(
            name: "ix_job_runs_status_queued_at",
            table: "job_runs");

        migrationBuilder.DropColumn(
            name: "stderr_log_path",
            table: "job_runs");

        migrationBuilder.DropColumn(
            name: "stdout_log_path",
            table: "job_runs");

        migrationBuilder.DropColumn(
            name: "workspace_path",
            table: "job_runs");
    }
}
