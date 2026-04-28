using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace NodeControl.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class AddHostConnectionChecks : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "host_connection_checks",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    customer_id = table.Column<Guid>(type: "uuid", nullable: false),
                    target_type = table.Column<string>(type: "character varying(40)", maxLength: 40, nullable: false),
                    control_node_id = table.Column<Guid>(type: "uuid", nullable: true),
                    managed_node_id = table.Column<Guid>(type: "uuid", nullable: true),
                    hostname = table.Column<string>(type: "character varying(253)", maxLength: 253, nullable: false),
                    port = table.Column<int>(type: "integer", nullable: false),
                    status = table.Column<string>(type: "character varying(40)", maxLength: 40, nullable: false),
                    requested_by_user_id = table.Column<Guid>(type: "uuid", nullable: true),
                    queued_at_utc = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    started_at_utc = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true),
                    finished_at_utc = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true),
                    duration_ms = table.Column<long>(type: "bigint", nullable: true),
                    result_message = table.Column<string>(type: "character varying(2000)", maxLength: 2000, nullable: true),
                    error_message = table.Column<string>(type: "character varying(4000)", maxLength: 4000, nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_host_connection_checks", x => x.id);
                    table.CheckConstraint("ck_host_connection_checks_port", "port >= 1 AND port <= 65535");
                    table.CheckConstraint("ck_host_connection_checks_target_reference", "((target_type = 'ControlNode' AND control_node_id IS NOT NULL AND managed_node_id IS NULL) OR (target_type = 'ManagedNode' AND managed_node_id IS NOT NULL AND control_node_id IS NULL))");
                    table.ForeignKey(
                        name: "FK_host_connection_checks_control_nodes_control_node_id",
                        column: x => x.control_node_id,
                        principalTable: "control_nodes",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_host_connection_checks_customers_customer_id",
                        column: x => x.customer_id,
                        principalTable: "customers",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_host_connection_checks_managed_nodes_managed_node_id",
                        column: x => x.managed_node_id,
                        principalTable: "managed_nodes",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_host_connection_checks_users_requested_by_user_id",
                        column: x => x.requested_by_user_id,
                        principalTable: "users",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateIndex(
                name: "ix_host_connection_checks_control_node_id",
                table: "host_connection_checks",
                column: "control_node_id");

            migrationBuilder.CreateIndex(
                name: "ix_host_connection_checks_customer_id_queued_at_utc",
                table: "host_connection_checks",
                columns: new[] { "customer_id", "queued_at_utc" });

            migrationBuilder.CreateIndex(
                name: "ix_host_connection_checks_customer_id_target_type",
                table: "host_connection_checks",
                columns: new[] { "customer_id", "target_type" });

            migrationBuilder.CreateIndex(
                name: "ix_host_connection_checks_managed_node_id",
                table: "host_connection_checks",
                column: "managed_node_id");

            migrationBuilder.CreateIndex(
                name: "IX_host_connection_checks_requested_by_user_id",
                table: "host_connection_checks",
                column: "requested_by_user_id");

            migrationBuilder.CreateIndex(
                name: "ix_host_connection_checks_status_queued_at_utc",
                table: "host_connection_checks",
                columns: new[] { "status", "queued_at_utc" });
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "host_connection_checks");
        }
    }
}
