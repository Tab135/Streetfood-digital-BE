using System;
using Microsoft.EntityFrameworkCore.Migrations;
using Npgsql.EntityFrameworkCore.PostgreSQL.Metadata;

#nullable disable

namespace DAL.Migrations
{
    /// <inheritdoc />
    public partial class AddCurrentPickInvites : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "CurrentPickInvites",
                columns: table => new
                {
                    CurrentPickInviteId = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    CurrentPickRoomId = table.Column<int>(type: "integer", nullable: false),
                    InvitedUserId = table.Column<int>(type: "integer", nullable: false),
                    InvitedByUserId = table.Column<int>(type: "integer", nullable: false),
                    Status = table.Column<int>(type: "integer", nullable: false, defaultValue: 0),
                    CreatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false, defaultValueSql: "CURRENT_TIMESTAMP"),
                    RespondedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_CurrentPickInvites", x => x.CurrentPickInviteId);
                    table.ForeignKey(
                        name: "FK_CurrentPickInvites_CurrentPickRooms_CurrentPickRoomId",
                        column: x => x.CurrentPickRoomId,
                        principalTable: "CurrentPickRooms",
                        principalColumn: "CurrentPickRoomId",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_CurrentPickInvites_Users_InvitedByUserId",
                        column: x => x.InvitedByUserId,
                        principalTable: "Users",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_CurrentPickInvites_Users_InvitedUserId",
                        column: x => x.InvitedUserId,
                        principalTable: "Users",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateIndex(
                name: "IX_CurrentPickInvites_CurrentPickRoomId_InvitedUserId",
                table: "CurrentPickInvites",
                columns: new[] { "CurrentPickRoomId", "InvitedUserId" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_CurrentPickInvites_InvitedByUserId",
                table: "CurrentPickInvites",
                column: "InvitedByUserId");

            migrationBuilder.CreateIndex(
                name: "IX_CurrentPickInvites_InvitedUserId",
                table: "CurrentPickInvites",
                column: "InvitedUserId");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "CurrentPickInvites");
        }
    }
}
