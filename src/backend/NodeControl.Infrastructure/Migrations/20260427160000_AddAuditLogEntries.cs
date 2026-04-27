using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace NodeControl.Infrastructure.Migrations;

public partial class AddAuditLogEntries : Migration
{
    protected override void Up(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.CreateTable(
            name: "audit_log_entries",
            columns: table => new
            {
                id = table.Column<Guid>(type: "uuid", nullable: false),
                customer_id = table.Column<Guid>(type: "uuid", nullable: true),
                actor_user_id = table.Column<Guid>(type: "uuid", nullable: true),
                actor_display_name = table.Column<string>(type: "character varying(300)", maxLength: 300, nullable: true),
                actor_type = table.Column<string>(type: "character varying(40)", maxLength: 40, nullable: false),
                action = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: false),
                entity_type = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: false),
                entity_id = table.Column<Guid>(type: "uuid", nullable: true),
                entity_display_name = table.Column<string>(type: "character varying(300)", maxLength: 300, nullable: true),
                outcome = table.Column<string>(type: "character varying(40)", maxLength: 40, nullable: false),
                message = table.Column<string>(type: "character varying(1000)", maxLength: 1000, nullable: false),
                metadata_json = table.Column<string>(type: "character varying(8000)", maxLength: 8000, nullable: true),
                ip_address = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: true),
                user_agent = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: true),
                created_at_utc = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false)
            },
            constraints: table =>
            {
                table.PrimaryKey("pk_audit_log_entries", x => x.id);
            });

        migrationBuilder.CreateIndex(
            name: "ix_audit_log_entries_actor_user_id_created_at_utc",
            table: "audit_log_entries",
            columns: new[] { "actor_user_id", "created_at_utc" });

        migrationBuilder.CreateIndex(
            name: "ix_audit_log_entries_customer_id_action",
            table: "audit_log_entries",
            columns: new[] { "customer_id", "action" });

        migrationBuilder.CreateIndex(
            name: "ix_audit_log_entries_customer_id_created_at_utc",
            table: "audit_log_entries",
            columns: new[] { "customer_id", "created_at_utc" });

        migrationBuilder.CreateIndex(
            name: "ix_audit_log_entries_customer_id_entity",
            table: "audit_log_entries",
            columns: new[] { "customer_id", "entity_type", "entity_id" });
    }

    protected override void Down(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.DropTable(
            name: "audit_log_entries");
    }
}
