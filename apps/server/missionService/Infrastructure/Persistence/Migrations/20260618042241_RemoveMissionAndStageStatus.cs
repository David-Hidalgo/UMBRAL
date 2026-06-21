using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace missionService.Migrations
{
    /// <inheritdoc />
    public partial class RemoveMissionAndStageStatus : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "ActiveSessionCount",
                table: "Stages");

            migrationBuilder.DropColumn(
                name: "ActiveSessionCount",
                table: "Missions");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<int>(
                name: "ActiveSessionCount",
                table: "Stages",
                type: "integer",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.AddColumn<int>(
                name: "ActiveSessionCount",
                table: "Missions",
                type: "integer",
                nullable: false,
                defaultValue: 0);
        }
    }
}
