using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace missionService.Migrations
{
    /// <inheritdoc />
    public partial class RefactorTreasureHuntSecretCode : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "PenaltyPoints",
                table: "Clues");

            migrationBuilder.AddColumn<string>(
                name: "SecretCode",
                table: "TreasureHuntSubStages",
                type: "character varying(100)",
                maxLength: 100,
                nullable: false,
                defaultValue: "");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "SecretCode",
                table: "TreasureHuntSubStages");

            migrationBuilder.AddColumn<int>(
                name: "PenaltyPoints",
                table: "Clues",
                type: "integer",
                nullable: false,
                defaultValue: 0);
        }
    }
}
