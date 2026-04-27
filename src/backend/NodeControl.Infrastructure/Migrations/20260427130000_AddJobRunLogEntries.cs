using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace NodeControl.Infrastructure.Migrations;

public partial class AddJobRunLogEntries : Migration
{
    protected override void Up(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.CreateTable(
            name: "job_run_log_entries",
            columns: table => new
            {
                id = table.Column<Guid>(type: "uuid", nullable: false),
                job_run_id = table.Column<Guid>(type: "uuid", nullable: false),
                sequence = table.Column<long>(type: "bigint", nullable: false),
                timestamp_utc = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                stream = table.Column<string>(type: "character varying(40)", maxLength: 40, nullable: false),
                level = table.Column<string>(type: "character varying(40)", maxLength: 40, nullable: false),
                message = table.Column<string>(type: "character varying(16000)", maxLength: 16000, nullable: false)
            },
            constraints: table =>
            {
                table.PrimaryKey("pk_job_run_log_entries", x => x.id);
                table.ForeignKey(
                    name: "fk_job_run_log_entries_job_runs_job_run_id",
                    column: x => x.job_run_id,
                    principalTable: "job_runs",
                    principalColumn: "id",
                    onDelete: ReferentialAction.Cascade);
            });

        migrationBuilder.CreateIndex(
            name: "ux_job_run_log_entries_job_run_id_sequence",
            table: "job_run_log_entries",
            columns: new[] { "job_run_id", "sequence" },
            unique: true);
    }

    protected override void Down(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.DropTable(name: "job_run_log_entries");
    }
}
