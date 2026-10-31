using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace PeKeyMngr.Migrations
{
    /// <inheritdoc />
    public partial class AddCustomFieldToUser : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "ApplicationUserId",
                table: "CustomRoles",
                type: "TEXT",
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "AuthorizationCode",
                table: "AspNetUsers",
                type: "INTEGER",
                nullable: true);

            migrationBuilder.CreateIndex(
                name: "IX_CustomRoles_ApplicationUserId",
                table: "CustomRoles",
                column: "ApplicationUserId");

            migrationBuilder.AddForeignKey(
                name: "FK_CustomRoles_AspNetUsers_ApplicationUserId",
                table: "CustomRoles",
                column: "ApplicationUserId",
                principalTable: "AspNetUsers",
                principalColumn: "Id");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_CustomRoles_AspNetUsers_ApplicationUserId",
                table: "CustomRoles");

            migrationBuilder.DropIndex(
                name: "IX_CustomRoles_ApplicationUserId",
                table: "CustomRoles");

            migrationBuilder.DropColumn(
                name: "ApplicationUserId",
                table: "CustomRoles");

            migrationBuilder.DropColumn(
                name: "AuthorizationCode",
                table: "AspNetUsers");
        }
    }
}
