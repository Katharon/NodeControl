using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace NodeControl.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class AddPlaybookArtifactFiles : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "artifact_files_json",
                table: "playbooks",
                type: "character varying(1000000)",
                maxLength: 1000000,
                nullable: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "artifact_files_json",
                table: "playbooks");
        }
    }
}
