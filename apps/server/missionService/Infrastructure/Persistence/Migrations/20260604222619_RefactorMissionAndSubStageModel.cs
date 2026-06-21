using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace missionService.Migrations
{
    /// <inheritdoc />
    public partial class RefactorMissionAndSubStageModel : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_Clues_TreasureHuntSubStages_TreasureHuntSubStageId",
                table: "Clues");

            migrationBuilder.RenameColumn(
                name: "Status",
                table: "Missions",
                newName: "State");

            migrationBuilder.RenameColumn(
                name: "TreasureHuntSubStageId",
                table: "Clues",
                newName: "SubStageId");

            migrationBuilder.RenameIndex(
                name: "IX_Clues_TreasureHuntSubStageId",
                table: "Clues",
                newName: "IX_Clues_SubStageId");

            migrationBuilder.AddColumn<int>(
                name: "ActiveSessionCount",
                table: "SubStages",
                type: "integer",
                nullable: false,
                defaultValue: 0);

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

            migrationBuilder.AddColumn<string>(
                name: "DifficultyLevel",
                table: "Missions",
                type: "character varying(50)",
                maxLength: 50,
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddColumn<int>(
                name: "MaxTimeMinutes",
                table: "Missions",
                type: "integer",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.Sql("UPDATE \"Missions\" SET \"DifficultyLevel\" = 'Beginner' WHERE \"DifficultyLevel\" = '';");
            migrationBuilder.Sql("UPDATE \"Missions\" SET \"MaxTimeMinutes\" = 60 WHERE \"MaxTimeMinutes\" = 0;");

            migrationBuilder.AddForeignKey(
                name: "FK_Clues_SubStages_SubStageId",
                table: "Clues",
                column: "SubStageId",
                principalTable: "SubStages",
                principalColumn: "Id",
                onDelete: ReferentialAction.Cascade);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_Clues_SubStages_SubStageId",
                table: "Clues");

            migrationBuilder.DropColumn(
                name: "ActiveSessionCount",
                table: "SubStages");

            migrationBuilder.DropColumn(
                name: "ActiveSessionCount",
                table: "Stages");

            migrationBuilder.DropColumn(
                name: "ActiveSessionCount",
                table: "Missions");

            migrationBuilder.DropColumn(
                name: "DifficultyLevel",
                table: "Missions");

            migrationBuilder.DropColumn(
                name: "MaxTimeMinutes",
                table: "Missions");

            migrationBuilder.RenameColumn(
                name: "State",
                table: "Missions",
                newName: "Status");

            migrationBuilder.RenameColumn(
                name: "SubStageId",
                table: "Clues",
                newName: "TreasureHuntSubStageId");

            migrationBuilder.RenameIndex(
                name: "IX_Clues_SubStageId",
                table: "Clues",
                newName: "IX_Clues_TreasureHuntSubStageId");

            migrationBuilder.AddForeignKey(
                name: "FK_Clues_TreasureHuntSubStages_TreasureHuntSubStageId",
                table: "Clues",
                column: "TreasureHuntSubStageId",
                principalTable: "TreasureHuntSubStages",
                principalColumn: "Id",
                onDelete: ReferentialAction.Cascade);
        }
    }
}
