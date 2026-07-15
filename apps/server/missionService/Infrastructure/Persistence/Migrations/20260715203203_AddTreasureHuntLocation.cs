using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace missionService.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class AddTreasureHuntLocation : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<double>(
                name: "Latitude",
                table: "TreasureHuntSubStages",
                type: "double precision",
                nullable: true);

            migrationBuilder.AddColumn<double>(
                name: "Longitude",
                table: "TreasureHuntSubStages",
                type: "double precision",
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "SearchRadiusMeters",
                table: "TreasureHuntSubStages",
                type: "integer",
                nullable: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "Latitude",
                table: "TreasureHuntSubStages");

            migrationBuilder.DropColumn(
                name: "Longitude",
                table: "TreasureHuntSubStages");

            migrationBuilder.DropColumn(
                name: "SearchRadiusMeters",
                table: "TreasureHuntSubStages");
        }
    }
}
