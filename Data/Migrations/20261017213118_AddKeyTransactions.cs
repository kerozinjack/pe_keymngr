using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace PeKeyMngr.Migrations
{
    /// <inheritdoc />
    public partial class AddKeyTransactions : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "KeyTransactions",
                columns: table => new
                {
                    KeyTransactionId = table.Column<Guid>(type: "TEXT", nullable: false),
                    Timestamp = table.Column<DateTime>(type: "TEXT", nullable: false),
                    Direction = table.Column<int>(type: "INTEGER", nullable: false),
                    KeyId = table.Column<Guid>(type: "TEXT", nullable: false),
                    AuthorizedById = table.Column<string>(type: "TEXT", nullable: true),
                    ParticipantId = table.Column<string>(type: "TEXT", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_KeyTransactions", x => x.KeyTransactionId);
                    table.ForeignKey(
                        name: "FK_KeyTransactions_AspNetUsers_AuthorizedById",
                        column: x => x.AuthorizedById,
                        principalTable: "AspNetUsers",
                        principalColumn: "Id");
                    table.ForeignKey(
                        name: "FK_KeyTransactions_AspNetUsers_ParticipantId",
                        column: x => x.ParticipantId,
                        principalTable: "AspNetUsers",
                        principalColumn: "Id");
                    table.ForeignKey(
                        name: "FK_KeyTransactions_Keys_KeyId",
                        column: x => x.KeyId,
                        principalTable: "Keys",
                        principalColumn: "KeyId",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_KeyTransactions_AuthorizedById",
                table: "KeyTransactions",
                column: "AuthorizedById");

            migrationBuilder.CreateIndex(
                name: "IX_KeyTransactions_KeyId",
                table: "KeyTransactions",
                column: "KeyId");

            migrationBuilder.CreateIndex(
                name: "IX_KeyTransactions_ParticipantId",
                table: "KeyTransactions",
                column: "ParticipantId");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "KeyTransactions");
        }
    }
}
