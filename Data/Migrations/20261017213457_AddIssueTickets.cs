using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace PeKeyMngr.Migrations
{
    /// <inheritdoc />
    public partial class AddIssueTickets : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "IssueTickets",
                columns: table => new
                {
                    TicketId = table.Column<Guid>(type: "TEXT", nullable: false),
                    Timestamp = table.Column<DateTime>(type: "TEXT", nullable: false),
                    ReportedById = table.Column<string>(type: "TEXT", nullable: true),
                    AffectedRoomRoomId = table.Column<Guid>(type: "TEXT", nullable: false),
                    IssueDescription = table.Column<string>(type: "TEXT", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_IssueTickets", x => x.TicketId);
                    table.ForeignKey(
                        name: "FK_IssueTickets_AspNetUsers_ReportedById",
                        column: x => x.ReportedById,
                        principalTable: "AspNetUsers",
                        principalColumn: "Id");
                    table.ForeignKey(
                        name: "FK_IssueTickets_Rooms_AffectedRoomRoomId",
                        column: x => x.AffectedRoomRoomId,
                        principalTable: "Rooms",
                        principalColumn: "RoomId",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_IssueTickets_AffectedRoomRoomId",
                table: "IssueTickets",
                column: "AffectedRoomRoomId");

            migrationBuilder.CreateIndex(
                name: "IX_IssueTickets_ReportedById",
                table: "IssueTickets",
                column: "ReportedById");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "IssueTickets");
        }
    }
}
