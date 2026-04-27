using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace NodeControl.Infrastructure.Migrations;

public partial class AddJobRunOperationalControls : Migration
{
    protected override void Up(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.AddColumn<DateTimeOffset>(
            name: "cancellation_requested_at_utc",
            table: "job_runs",
            type: "timestamp with time zone",
            nullable: true);

        migrationBuilder.AddColumn<Guid>(
            name: "cancellation_requested_by_user_id",
            table: "job_runs",
            type: "uuid",
            nullable: true);

        migrationBuilder.AddColumn<string>(
            name: "cancellation_reason",
            table: "job_runs",
            type: "character varying(1000)",
            maxLength: 1000,
            nullable: true);

        migrationBuilder.AddColumn<Guid>(
            name: "retried_from_job_run_id",
            table: "job_runs",
            type: "uuid",
            nullable: true);

        migrationBuilder.AddColumn<int>(
            name: "retry_attempt",
            table: "job_runs",
            type: "integer",
            nullable: false,
            defaultValue: 0);

        migrationBuilder.CreateIndex(
            name: "ix_job_runs_cancellation_requested_by_user_id",
            table: "job_runs",
            column: "cancellation_requested_by_user_id");

        migrationBuilder.CreateIndex(
            name: "ix_job_runs_retried_from_job_run_id",
            table: "job_runs",
            column: "retried_from_job_run_id");

        migrationBuilder.AddForeignKey(
            name: "fk_job_runs_job_runs_retried_from_job_run_id",
            table: "job_runs",
            column: "retried_from_job_run_id",
            principalTable: "job_runs",
            principalColumn: "id",
            onDelete: ReferentialAction.Restrict);

        migrationBuilder.AddForeignKey(
            name: "fk_job_runs_users_cancellation_requested_by_user_id",
            table: "job_runs",
            column: "cancellation_requested_by_user_id",
            principalTable: "users",
            principalColumn: "id",
            onDelete: ReferentialAction.Restrict);
    }

    protected override void Down(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.DropForeignKey(
            name: "fk_job_runs_job_runs_retried_from_job_run_id",
            table: "job_runs");

        migrationBuilder.DropForeignKey(
            name: "fk_job_runs_users_cancellation_requested_by_user_id",
            table: "job_runs");

        migrationBuilder.DropIndex(
            name: "ix_job_runs_cancellation_requested_by_user_id",
            table: "job_runs");

        migrationBuilder.DropIndex(
            name: "ix_job_runs_retried_from_job_run_id",
            table: "job_runs");

        migrationBuilder.DropColumn(
            name: "cancellation_requested_at_utc",
            table: "job_runs");

        migrationBuilder.DropColumn(
            name: "cancellation_requested_by_user_id",
            table: "job_runs");

        migrationBuilder.DropColumn(
            name: "cancellation_reason",
            table: "job_runs");

        migrationBuilder.DropColumn(
            name: "retried_from_job_run_id",
            table: "job_runs");

        migrationBuilder.DropColumn(
            name: "retry_attempt",
            table: "job_runs");
    }
}
