using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace NodeControl.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class AddManagedNodeJumpHostReference : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<Guid>(
                name: "jump_host_managed_node_id",
                table: "managed_nodes",
                type: "uuid",
                nullable: true);

            migrationBuilder.CreateIndex(
                name: "ix_managed_nodes_jump_host_managed_node_id",
                table: "managed_nodes",
                column: "jump_host_managed_node_id");

            migrationBuilder.AddForeignKey(
                name: "FK_managed_nodes_managed_nodes_jump_host_managed_node_id",
                table: "managed_nodes",
                column: "jump_host_managed_node_id",
                principalTable: "managed_nodes",
                principalColumn: "id",
                onDelete: ReferentialAction.Restrict);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_managed_nodes_managed_nodes_jump_host_managed_node_id",
                table: "managed_nodes");

            migrationBuilder.DropIndex(
                name: "ix_managed_nodes_jump_host_managed_node_id",
                table: "managed_nodes");

            migrationBuilder.DropColumn(
                name: "jump_host_managed_node_id",
                table: "managed_nodes");
        }
    }
}
