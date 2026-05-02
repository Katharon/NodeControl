using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace NodeControl.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class AddControlNodeSshRemoteDispatch : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "remote_workspace_root",
                table: "control_nodes",
                type: "character varying(500)",
                maxLength: 500,
                nullable: true);

            migrationBuilder.AddColumn<Guid>(
                name: "ssh_private_key_secret_id",
                table: "control_nodes",
                type: "uuid",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "ssh_username",
                table: "control_nodes",
                type: "character varying(100)",
                maxLength: 100,
                nullable: true);

            migrationBuilder.CreateIndex(
                name: "ix_control_nodes_ssh_private_key_secret_id",
                table: "control_nodes",
                column: "ssh_private_key_secret_id");

            migrationBuilder.AddForeignKey(
                name: "FK_control_nodes_secrets_ssh_private_key_secret_id",
                table: "control_nodes",
                column: "ssh_private_key_secret_id",
                principalTable: "secrets",
                principalColumn: "id",
                onDelete: ReferentialAction.Restrict);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_control_nodes_secrets_ssh_private_key_secret_id",
                table: "control_nodes");

            migrationBuilder.DropIndex(
                name: "ix_control_nodes_ssh_private_key_secret_id",
                table: "control_nodes");

            migrationBuilder.DropColumn(
                name: "remote_workspace_root",
                table: "control_nodes");

            migrationBuilder.DropColumn(
                name: "ssh_private_key_secret_id",
                table: "control_nodes");

            migrationBuilder.DropColumn(
                name: "ssh_username",
                table: "control_nodes");
        }
    }
}
