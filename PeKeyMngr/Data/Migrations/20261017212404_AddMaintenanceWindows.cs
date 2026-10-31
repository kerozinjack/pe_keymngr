using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace PeKeyMngr.Migrations
{
    /// <inheritdoc />
    public partial class AddMaintenanceWindows : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "MaintenanceWindows",
                columns: table => new
                {
                    MaintenanceWindowId = table.Column<Guid>(type: "TEXT", nullable: false),
                    StartTime = table.Column<DateTime>(type: "TEXT", nullable: false),
                    EndTime = table.Column<DateTime>(type: "TEXT", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_MaintenanceWindows", x => x.MaintenanceWindowId);
                });

            migrationBuilder.CreateTable(
                name: "MaintenanceRoom",
                columns: table => new
                {
                    AffectedRoomsRoomId = table.Column<Guid>(type: "TEXT", nullable: false),
                    MaintenanceWindowId = table.Column<Guid>(type: "TEXT", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_MaintenanceRoom", x => new { x.AffectedRoomsRoomId, x.MaintenanceWindowId });
                    table.ForeignKey(
                        name: "FK_MaintenanceRoom_MaintenanceWindows_MaintenanceWindowId",
                        column: x => x.MaintenanceWindowId,
                        principalTable: "MaintenanceWindows",
                        principalColumn: "MaintenanceWindowId",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_MaintenanceRoom_Rooms_AffectedRoomsRoomId",
                        column: x => x.AffectedRoomsRoomId,
                        principalTable: "Rooms",
                        principalColumn: "RoomId",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_MaintenanceRoom_MaintenanceWindowId",
                table: "MaintenanceRoom",
                column: "MaintenanceWindowId");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "MaintenanceRoom");

            migrationBuilder.DropTable(
                name: "MaintenanceWindows");
        }
    }
}
