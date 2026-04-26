using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace NodeControl.Infrastructure.Migrations;

public partial class AddNodesAndInventoryGroups : Migration
{
    protected override void Up(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.CreateTable(
            name: "control_nodes",
            columns: table => new
            {
                id = table.Column<Guid>(type: "uuid", nullable: false),
                customer_id = table.Column<Guid>(type: "uuid", nullable: false),
                name = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: false),
                hostname = table.Column<string>(type: "character varying(253)", maxLength: 253, nullable: false),
                ssh_port = table.Column<int>(type: "integer", nullable: false),
                description = table.Column<string>(type: "character varying(1000)", maxLength: 1000, nullable: true),
                status = table.Column<string>(type: "character varying(40)", maxLength: 40, nullable: false),
                created_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                updated_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true),
                archived_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true)
            },
            constraints: table =>
            {
                table.PrimaryKey("pk_control_nodes", x => x.id);
                table.ForeignKey(
                    name: "fk_control_nodes_customers_customer_id",
                    column: x => x.customer_id,
                    principalTable: "customers",
                    principalColumn: "id",
                    onDelete: ReferentialAction.Restrict);
            });

        migrationBuilder.CreateTable(
            name: "inventory_groups",
            columns: table => new
            {
                id = table.Column<Guid>(type: "uuid", nullable: false),
                customer_id = table.Column<Guid>(type: "uuid", nullable: false),
                name = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: false),
                description = table.Column<string>(type: "character varying(1000)", maxLength: 1000, nullable: true),
                created_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                updated_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true),
                archived_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true)
            },
            constraints: table =>
            {
                table.PrimaryKey("pk_inventory_groups", x => x.id);
                table.ForeignKey(
                    name: "fk_inventory_groups_customers_customer_id",
                    column: x => x.customer_id,
                    principalTable: "customers",
                    principalColumn: "id",
                    onDelete: ReferentialAction.Restrict);
            });

        migrationBuilder.CreateTable(
            name: "managed_nodes",
            columns: table => new
            {
                id = table.Column<Guid>(type: "uuid", nullable: false),
                customer_id = table.Column<Guid>(type: "uuid", nullable: false),
                name = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: false),
                hostname = table.Column<string>(type: "character varying(253)", maxLength: 253, nullable: false),
                ssh_port = table.Column<int>(type: "integer", nullable: false),
                operating_system = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: true),
                environment = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: true),
                description = table.Column<string>(type: "character varying(1000)", maxLength: 1000, nullable: true),
                status = table.Column<string>(type: "character varying(40)", maxLength: 40, nullable: false),
                created_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                updated_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true),
                archived_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true)
            },
            constraints: table =>
            {
                table.PrimaryKey("pk_managed_nodes", x => x.id);
                table.ForeignKey(
                    name: "fk_managed_nodes_customers_customer_id",
                    column: x => x.customer_id,
                    principalTable: "customers",
                    principalColumn: "id",
                    onDelete: ReferentialAction.Restrict);
            });

        migrationBuilder.CreateTable(
            name: "inventory_group_nodes",
            columns: table => new
            {
                inventory_group_id = table.Column<Guid>(type: "uuid", nullable: false),
                managed_node_id = table.Column<Guid>(type: "uuid", nullable: false),
                created_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false)
            },
            constraints: table =>
            {
                table.PrimaryKey("pk_inventory_group_nodes", x => new { x.inventory_group_id, x.managed_node_id });
                table.ForeignKey(
                    name: "fk_inventory_group_nodes_inventory_groups_inventory_group_id",
                    column: x => x.inventory_group_id,
                    principalTable: "inventory_groups",
                    principalColumn: "id",
                    onDelete: ReferentialAction.Restrict);
                table.ForeignKey(
                    name: "fk_inventory_group_nodes_managed_nodes_managed_node_id",
                    column: x => x.managed_node_id,
                    principalTable: "managed_nodes",
                    principalColumn: "id",
                    onDelete: ReferentialAction.Restrict);
            });

        migrationBuilder.CreateIndex(
            name: "ux_control_nodes_customer_id_name",
            table: "control_nodes",
            columns: new[] { "customer_id", "name" },
            unique: true);

        migrationBuilder.CreateIndex(
            name: "ux_inventory_groups_customer_id_name",
            table: "inventory_groups",
            columns: new[] { "customer_id", "name" },
            unique: true);

        migrationBuilder.CreateIndex(
            name: "ix_inventory_group_nodes_managed_node_id",
            table: "inventory_group_nodes",
            column: "managed_node_id");

        migrationBuilder.CreateIndex(
            name: "ux_managed_nodes_customer_id_name",
            table: "managed_nodes",
            columns: new[] { "customer_id", "name" },
            unique: true);
    }

    protected override void Down(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.DropTable(name: "control_nodes");

        migrationBuilder.DropTable(name: "inventory_group_nodes");

        migrationBuilder.DropTable(name: "inventory_groups");

        migrationBuilder.DropTable(name: "managed_nodes");
    }
}
