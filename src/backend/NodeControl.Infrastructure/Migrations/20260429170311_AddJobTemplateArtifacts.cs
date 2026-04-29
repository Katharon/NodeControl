using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace NodeControl.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class AddJobTemplateArtifacts : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "template_artifacts_json",
                table: "jobs",
                type: "jsonb",
                nullable: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "template_artifacts_json",
                table: "jobs");
        }
    }
}
