using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace NodeControl.Infrastructure.Migrations;

public partial class AddCustomersAndMemberships : Migration
{
    protected override void Up(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.CreateTable(
            name: "customers",
            columns: table => new
            {
                id = table.Column<Guid>(type: "uuid", nullable: false),
                name = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: false),
                slug = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: false),
                description = table.Column<string>(type: "character varying(1000)", maxLength: 1000, nullable: true),
                status = table.Column<string>(type: "character varying(40)", maxLength: 40, nullable: false),
                created_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                updated_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true),
                archived_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true)
            },
            constraints: table =>
            {
                table.PrimaryKey("pk_customers", x => x.id);
            });

        migrationBuilder.CreateTable(
            name: "customer_memberships",
            columns: table => new
            {
                id = table.Column<Guid>(type: "uuid", nullable: false),
                customer_id = table.Column<Guid>(type: "uuid", nullable: false),
                user_id = table.Column<Guid>(type: "uuid", nullable: false),
                role = table.Column<string>(type: "character varying(40)", maxLength: 40, nullable: false),
                is_active = table.Column<bool>(type: "boolean", nullable: false),
                created_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                updated_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true),
                deactivated_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true)
            },
            constraints: table =>
            {
                table.PrimaryKey("pk_customer_memberships", x => x.id);
                table.ForeignKey(
                    name: "fk_customer_memberships_customers_customer_id",
                    column: x => x.customer_id,
                    principalTable: "customers",
                    principalColumn: "id",
                    onDelete: ReferentialAction.Restrict);
                table.ForeignKey(
                    name: "fk_customer_memberships_users_user_id",
                    column: x => x.user_id,
                    principalTable: "users",
                    principalColumn: "id",
                    onDelete: ReferentialAction.Restrict);
            });

        migrationBuilder.CreateIndex(
            name: "ux_customers_slug",
            table: "customers",
            column: "slug",
            unique: true);

        migrationBuilder.CreateIndex(
            name: "ix_customer_memberships_customer_id",
            table: "customer_memberships",
            column: "customer_id");

        migrationBuilder.CreateIndex(
            name: "ix_customer_memberships_user_id",
            table: "customer_memberships",
            column: "user_id");

        migrationBuilder.CreateIndex(
            name: "ux_customer_memberships_customer_id_user_id",
            table: "customer_memberships",
            columns: new[] { "customer_id", "user_id" },
            unique: true);
    }

    protected override void Down(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.DropTable(name: "customer_memberships");

        migrationBuilder.DropTable(name: "customers");
    }
}
