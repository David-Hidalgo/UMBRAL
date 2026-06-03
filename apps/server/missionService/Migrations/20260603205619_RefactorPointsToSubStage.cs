using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace missionService.Migrations
{
    /// <inheritdoc />
    public partial class RefactorPointsToSubStage : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_Questions_TriviaSubStageId",
                table: "Questions");

            migrationBuilder.DropColumn(
                name: "Points",
                table: "Questions");

            migrationBuilder.AddColumn<int>(
                name: "Points",
                table: "SubStages",
                type: "integer",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.CreateIndex(
                name: "IX_Questions_TriviaSubStageId",
                table: "Questions",
                column: "TriviaSubStageId",
                unique: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_Questions_TriviaSubStageId",
                table: "Questions");

            migrationBuilder.DropColumn(
                name: "Points",
                table: "SubStages");

            migrationBuilder.AddColumn<int>(
                name: "Points",
                table: "Questions",
                type: "integer",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.CreateIndex(
                name: "IX_Questions_TriviaSubStageId",
                table: "Questions",
                column: "TriviaSubStageId");
        }
    }
}
