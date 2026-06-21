using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace missionService.Migrations
{
    /// <inheritdoc />
    public partial class AddMissionNameUniqueIndex : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateIndex(
                name: "IX_Missions_Name",
                table: "Missions",
                column: "Name",
                unique: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_Missions_Name",
                table: "Missions");
        }
    }
}
