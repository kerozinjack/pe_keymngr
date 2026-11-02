using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace PeKeyMngr.Migrations
{
    /// <inheritdoc />
    public partial class AddKeys : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "Keys",
                columns: table => new
                {
                    KeyId = table.Column<Guid>(type: "TEXT", nullable: false),
                    RoomName = table.Column<string>(type: "TEXT", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Keys", x => x.KeyId);
                });

            migrationBuilder.CreateTable(
                name: "KeyRole",
                columns: table => new
                {
                    KeyId = table.Column<Guid>(type: "TEXT", nullable: false),
                    RequiredRolesRoleId = table.Column<Guid>(type: "TEXT", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_KeyRole", x => new { x.KeyId, x.RequiredRolesRoleId });
                    table.ForeignKey(
                        name: "FK_KeyRole_CustomRoles_RequiredRolesRoleId",
                        column: x => x.RequiredRolesRoleId,
                        principalTable: "CustomRoles",
                        principalColumn: "RoleId",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_KeyRole_Keys_KeyId",
                        column: x => x.KeyId,
                        principalTable: "Keys",
                        principalColumn: "KeyId",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_KeyRole_RequiredRolesRoleId",
                table: "KeyRole",
                column: "RequiredRolesRoleId");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "KeyRole");

            migrationBuilder.DropTable(
                name: "Keys");
        }
    }
}
