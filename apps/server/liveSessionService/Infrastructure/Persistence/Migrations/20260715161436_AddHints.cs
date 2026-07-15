using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace liveSessionService.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class AddHints : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "HintRequests",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    LiveSessionId = table.Column<Guid>(type: "uuid", nullable: false),
                    TeamId = table.Column<Guid>(type: "uuid", nullable: false),
                    SubStageId = table.Column<Guid>(type: "uuid", nullable: false),
                    Status = table.Column<string>(type: "character varying(20)", maxLength: 20, nullable: false),
                    RequestedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    ResolvedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_HintRequests", x => x.Id);
                    table.ForeignKey(
                        name: "FK_HintRequests_LiveSessions_LiveSessionId",
                        column: x => x.LiveSessionId,
                        principalTable: "LiveSessions",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "ReleasedHints",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    LiveSessionId = table.Column<Guid>(type: "uuid", nullable: false),
                    TeamId = table.Column<Guid>(type: "uuid", nullable: false),
                    SubStageId = table.Column<Guid>(type: "uuid", nullable: false),
                    ClueId = table.Column<Guid>(type: "uuid", nullable: false),
                    ClueText = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: false),
                    ClueOrder = table.Column<int>(type: "integer", nullable: false),
                    ReleasedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ReleasedHints", x => x.Id);
                    table.ForeignKey(
                        name: "FK_ReleasedHints_LiveSessions_LiveSessionId",
                        column: x => x.LiveSessionId,
                        principalTable: "LiveSessions",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_HintRequests_LiveSessionId_TeamId_SubStageId_Status",
                table: "HintRequests",
                columns: new[] { "LiveSessionId", "TeamId", "SubStageId", "Status" });

            migrationBuilder.CreateIndex(
                name: "IX_ReleasedHints_LiveSessionId_TeamId_ClueId",
                table: "ReleasedHints",
                columns: new[] { "LiveSessionId", "TeamId", "ClueId" },
                unique: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "HintRequests");

            migrationBuilder.DropTable(
                name: "ReleasedHints");
        }
    }
}
