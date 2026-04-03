using System;
using Microsoft.EntityFrameworkCore.Migrations;
using Npgsql.EntityFrameworkCore.PostgreSQL.Metadata;

#nullable disable

namespace DAL.Migrations
{
    /// <inheritdoc />
    public partial class AddCurrentPickFeature : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "CurrentPickRooms",
                columns: table => new
                {
                    CurrentPickRoomId = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    RoomCode = table.Column<string>(type: "character varying(12)", maxLength: 12, nullable: false),
                    Title = table.Column<string>(type: "character varying(255)", maxLength: 255, nullable: true),
                    HostUserId = table.Column<int>(type: "integer", nullable: false),
                    IsFinalized = table.Column<bool>(type: "boolean", nullable: false, defaultValue: false),
                    FinalizedBranchId = table.Column<int>(type: "integer", nullable: true),
                    CreatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false, defaultValueSql: "CURRENT_TIMESTAMP"),
                    FinalizedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    UpdatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    IsActive = table.Column<bool>(type: "boolean", nullable: false, defaultValue: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_CurrentPickRooms", x => x.CurrentPickRoomId);
                    table.ForeignKey(
                        name: "FK_CurrentPickRooms_Branches_FinalizedBranchId",
                        column: x => x.FinalizedBranchId,
                        principalTable: "Branches",
                        principalColumn: "BranchId",
                        onDelete: ReferentialAction.SetNull);
                    table.ForeignKey(
                        name: "FK_CurrentPickRooms_Users_HostUserId",
                        column: x => x.HostUserId,
                        principalTable: "Users",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "CurrentPickBranches",
                columns: table => new
                {
                    CurrentPickBranchId = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    CurrentPickRoomId = table.Column<int>(type: "integer", nullable: false),
                    BranchId = table.Column<int>(type: "integer", nullable: false),
                    AddedByUserId = table.Column<int>(type: "integer", nullable: false),
                    AddedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false, defaultValueSql: "CURRENT_TIMESTAMP")
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_CurrentPickBranches", x => x.CurrentPickBranchId);
                    table.ForeignKey(
                        name: "FK_CurrentPickBranches_Branches_BranchId",
                        column: x => x.BranchId,
                        principalTable: "Branches",
                        principalColumn: "BranchId",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_CurrentPickBranches_CurrentPickRooms_CurrentPickRoomId",
                        column: x => x.CurrentPickRoomId,
                        principalTable: "CurrentPickRooms",
                        principalColumn: "CurrentPickRoomId",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_CurrentPickBranches_Users_AddedByUserId",
                        column: x => x.AddedByUserId,
                        principalTable: "Users",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "CurrentPickMembers",
                columns: table => new
                {
                    CurrentPickMemberId = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    CurrentPickRoomId = table.Column<int>(type: "integer", nullable: false),
                    UserId = table.Column<int>(type: "integer", nullable: false),
                    IsHost = table.Column<bool>(type: "boolean", nullable: false),
                    JoinedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false, defaultValueSql: "CURRENT_TIMESTAMP")
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_CurrentPickMembers", x => x.CurrentPickMemberId);
                    table.ForeignKey(
                        name: "FK_CurrentPickMembers_CurrentPickRooms_CurrentPickRoomId",
                        column: x => x.CurrentPickRoomId,
                        principalTable: "CurrentPickRooms",
                        principalColumn: "CurrentPickRoomId",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_CurrentPickMembers_Users_UserId",
                        column: x => x.UserId,
                        principalTable: "Users",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "CurrentPickVotes",
                columns: table => new
                {
                    CurrentPickVoteId = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    CurrentPickRoomId = table.Column<int>(type: "integer", nullable: false),
                    BranchId = table.Column<int>(type: "integer", nullable: false),
                    UserId = table.Column<int>(type: "integer", nullable: false),
                    VotedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false, defaultValueSql: "CURRENT_TIMESTAMP")
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_CurrentPickVotes", x => x.CurrentPickVoteId);
                    table.ForeignKey(
                        name: "FK_CurrentPickVotes_Branches_BranchId",
                        column: x => x.BranchId,
                        principalTable: "Branches",
                        principalColumn: "BranchId",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_CurrentPickVotes_CurrentPickRooms_CurrentPickRoomId",
                        column: x => x.CurrentPickRoomId,
                        principalTable: "CurrentPickRooms",
                        principalColumn: "CurrentPickRoomId",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_CurrentPickVotes_Users_UserId",
                        column: x => x.UserId,
                        principalTable: "Users",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateIndex(
                name: "IX_CurrentPickBranches_AddedByUserId",
                table: "CurrentPickBranches",
                column: "AddedByUserId");

            migrationBuilder.CreateIndex(
                name: "IX_CurrentPickBranches_BranchId",
                table: "CurrentPickBranches",
                column: "BranchId");

            migrationBuilder.CreateIndex(
                name: "IX_CurrentPickBranches_CurrentPickRoomId_BranchId",
                table: "CurrentPickBranches",
                columns: new[] { "CurrentPickRoomId", "BranchId" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_CurrentPickMembers_CurrentPickRoomId_UserId",
                table: "CurrentPickMembers",
                columns: new[] { "CurrentPickRoomId", "UserId" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_CurrentPickMembers_UserId",
                table: "CurrentPickMembers",
                column: "UserId");

            migrationBuilder.CreateIndex(
                name: "IX_CurrentPickRooms_FinalizedBranchId",
                table: "CurrentPickRooms",
                column: "FinalizedBranchId");

            migrationBuilder.CreateIndex(
                name: "IX_CurrentPickRooms_HostUserId",
                table: "CurrentPickRooms",
                column: "HostUserId");

            migrationBuilder.CreateIndex(
                name: "IX_CurrentPickRooms_RoomCode",
                table: "CurrentPickRooms",
                column: "RoomCode",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_CurrentPickVotes_BranchId",
                table: "CurrentPickVotes",
                column: "BranchId");

            migrationBuilder.CreateIndex(
                name: "IX_CurrentPickVotes_CurrentPickRoomId_BranchId",
                table: "CurrentPickVotes",
                columns: new[] { "CurrentPickRoomId", "BranchId" });

            migrationBuilder.CreateIndex(
                name: "IX_CurrentPickVotes_CurrentPickRoomId_UserId",
                table: "CurrentPickVotes",
                columns: new[] { "CurrentPickRoomId", "UserId" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_CurrentPickVotes_UserId",
                table: "CurrentPickVotes",
                column: "UserId");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "CurrentPickBranches");

            migrationBuilder.DropTable(
                name: "CurrentPickMembers");

            migrationBuilder.DropTable(
                name: "CurrentPickVotes");

            migrationBuilder.DropTable(
                name: "CurrentPickRooms");
        }
    }
}
