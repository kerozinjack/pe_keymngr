using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace PeKeyMngr.Data.Migrations
{
    /// <inheritdoc />
    public partial class AddReservationNoShow : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<bool>(
                name: "IsNoShow",
                table: "Reservations",
                type: "INTEGER",
                nullable: false,
                defaultValue: false);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "IsNoShow",
                table: "Reservations");
        }
    }
}
