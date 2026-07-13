using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace liveSessionService.Migrations
{
    /// <inheritdoc />
    public partial class AddSessionTimer : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<DateTime>(
                name: "EndsAt",
                table: "LiveSessions",
                type: "timestamp with time zone",
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "MaxTimeMinutes",
                table: "LiveSessions",
                type: "integer",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.AddColumn<int>(
                name: "RemainingSecondsOnPause",
                table: "LiveSessions",
                type: "integer",
                nullable: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "EndsAt",
                table: "LiveSessions");

            migrationBuilder.DropColumn(
                name: "MaxTimeMinutes",
                table: "LiveSessions");

            migrationBuilder.DropColumn(
                name: "RemainingSecondsOnPause",
                table: "LiveSessions");
        }
    }
}
