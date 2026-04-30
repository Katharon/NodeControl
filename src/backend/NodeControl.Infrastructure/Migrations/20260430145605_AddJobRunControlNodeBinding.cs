using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace NodeControl.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class AddJobRunControlNodeBinding : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<Guid>(
                name: "control_node_id",
                table: "job_runs",
                type: "uuid",
                nullable: true);

            migrationBuilder.Sql("""
                UPDATE job_runs
                SET control_node_id = jobs.control_node_id
                FROM jobs
                WHERE job_runs.job_id = jobs.id
                """);

            migrationBuilder.AlterColumn<Guid>(
                name: "control_node_id",
                table: "job_runs",
                type: "uuid",
                nullable: false,
                oldClrType: typeof(Guid),
                oldType: "uuid",
                oldNullable: true);

            migrationBuilder.CreateIndex(
                name: "IX_job_runs_control_node_id",
                table: "job_runs",
                column: "control_node_id");

            migrationBuilder.CreateIndex(
                name: "ix_job_runs_customer_id_control_node_id_created_at",
                table: "job_runs",
                columns: new[] { "customer_id", "control_node_id", "created_at" });

            migrationBuilder.AddForeignKey(
                name: "FK_job_runs_control_nodes_control_node_id",
                table: "job_runs",
                column: "control_node_id",
                principalTable: "control_nodes",
                principalColumn: "id",
                onDelete: ReferentialAction.Restrict);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_job_runs_control_nodes_control_node_id",
                table: "job_runs");

            migrationBuilder.DropIndex(
                name: "IX_job_runs_control_node_id",
                table: "job_runs");

            migrationBuilder.DropIndex(
                name: "ix_job_runs_customer_id_control_node_id_created_at",
                table: "job_runs");

            migrationBuilder.DropColumn(
                name: "control_node_id",
                table: "job_runs");
        }
    }
}
