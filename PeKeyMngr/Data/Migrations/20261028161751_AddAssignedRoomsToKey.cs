using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace PeKeyMngr.Migrations
{
    /// <inheritdoc />
    public partial class AddAssignedRoomsToKey : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.RenameColumn(
                name: "RoomName",
                table: "Keys",
                newName: "KeyName");

            migrationBuilder.CreateTable(
                name: "KeyRoom",
                columns: table => new
                {
                    AssignedRoomsRoomId = table.Column<string>(type: "TEXT", nullable: false),
                    KeyId = table.Column<string>(type: "TEXT", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_KeyRoom", x => new { x.AssignedRoomsRoomId, x.KeyId });
                    table.ForeignKey(
                        name: "FK_KeyRoom_Keys_KeyId",
                        column: x => x.KeyId,
                        principalTable: "Keys",
                        principalColumn: "KeyId",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_KeyRoom_Rooms_AssignedRoomsRoomId",
                        column: x => x.AssignedRoomsRoomId,
                        principalTable: "Rooms",
                        principalColumn: "RoomId",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_KeyRoom_KeyId",
                table: "KeyRoom",
                column: "KeyId");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "KeyRoom");

            migrationBuilder.RenameColumn(
                name: "KeyName",
                table: "Keys",
                newName: "RoomName");
        }
    }
}
