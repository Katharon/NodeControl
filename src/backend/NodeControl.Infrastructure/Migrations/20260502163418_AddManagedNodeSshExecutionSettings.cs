using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace NodeControl.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class AddManagedNodeSshExecutionSettings : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<Guid>(
                name: "ssh_private_key_secret_id",
                table: "managed_nodes",
                type: "uuid",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "ssh_username",
                table: "managed_nodes",
                type: "character varying(100)",
                maxLength: 100,
                nullable: true);

            migrationBuilder.CreateIndex(
                name: "ix_managed_nodes_ssh_private_key_secret_id",
                table: "managed_nodes",
                column: "ssh_private_key_secret_id");

            migrationBuilder.AddForeignKey(
                name: "FK_managed_nodes_secrets_ssh_private_key_secret_id",
                table: "managed_nodes",
                column: "ssh_private_key_secret_id",
                principalTable: "secrets",
                principalColumn: "id",
                onDelete: ReferentialAction.Restrict);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_managed_nodes_secrets_ssh_private_key_secret_id",
                table: "managed_nodes");

            migrationBuilder.DropIndex(
                name: "ix_managed_nodes_ssh_private_key_secret_id",
                table: "managed_nodes");

            migrationBuilder.DropColumn(
                name: "ssh_private_key_secret_id",
                table: "managed_nodes");

            migrationBuilder.DropColumn(
                name: "ssh_username",
                table: "managed_nodes");
        }
    }
}
