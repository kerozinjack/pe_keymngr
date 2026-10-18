using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace PeKeyMngr.Migrations
{
    /// <inheritdoc />
    public partial class AddRooms : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "Rooms",
                columns: table => new
                {
                    RoomId = table.Column<Guid>(type: "TEXT", nullable: false),
                    RoomName = table.Column<string>(type: "TEXT", nullable: false),
                    Building = table.Column<string>(type: "TEXT", nullable: false),
                    Floor = table.Column<string>(type: "TEXT", nullable: false),
                    Capacity = table.Column<int>(type: "INTEGER", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Rooms", x => x.RoomId);
                });

            migrationBuilder.CreateTable(
                name: "RoomEquipment",
                columns: table => new
                {
                    EquipmentsEquipmentId = table.Column<Guid>(type: "TEXT", nullable: false),
                    RoomId = table.Column<Guid>(type: "TEXT", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_RoomEquipment", x => new { x.EquipmentsEquipmentId, x.RoomId });
                    table.ForeignKey(
                        name: "FK_RoomEquipment_Equipments_EquipmentsEquipmentId",
                        column: x => x.EquipmentsEquipmentId,
                        principalTable: "Equipments",
                        principalColumn: "EquipmentId",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_RoomEquipment_Rooms_RoomId",
                        column: x => x.RoomId,
                        principalTable: "Rooms",
                        principalColumn: "RoomId",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_RoomEquipment_RoomId",
                table: "RoomEquipment",
                column: "RoomId");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "RoomEquipment");

            migrationBuilder.DropTable(
                name: "Rooms");
        }
    }
}
