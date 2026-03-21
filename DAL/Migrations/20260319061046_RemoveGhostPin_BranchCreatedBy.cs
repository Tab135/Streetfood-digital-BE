using System;
using Microsoft.EntityFrameworkCore.Migrations;
using Npgsql.EntityFrameworkCore.PostgreSQL.Metadata;

#nullable disable

namespace DAL.Migrations
{
    /// <inheritdoc />
    public partial class RemoveGhostPin_BranchCreatedBy : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "GhostPins");

            migrationBuilder.AddColumn<int>(
                name: "CreatedById",
                table: "Branches",
                type: "integer",
                nullable: true);

            migrationBuilder.CreateIndex(
                name: "IX_Branches_CreatedById",
                table: "Branches",
                column: "CreatedById");

            migrationBuilder.AddForeignKey(
                name: "FK_Branches_Users_CreatedById",
                table: "Branches",
                column: "CreatedById",
                principalTable: "Users",
                principalColumn: "Id");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_Branches_Users_CreatedById",
                table: "Branches");

            migrationBuilder.DropIndex(
                name: "IX_Branches_CreatedById",
                table: "Branches");

            migrationBuilder.DropColumn(
                name: "CreatedById",
                table: "Branches");

            migrationBuilder.CreateTable(
                name: "GhostPins",
                columns: table => new
                {
                    GhostPinId = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    CreatorId = table.Column<int>(type: "integer", nullable: false),
                    LinkedBranchId = table.Column<int>(type: "integer", nullable: true),
                    TierId = table.Column<int>(type: "integer", nullable: false),
                    AddressDetail = table.Column<string>(type: "text", nullable: false),
                    AvgRating = table.Column<double>(type: "double precision", nullable: false),
                    BatchRatingSum = table.Column<int>(type: "integer", nullable: false),
                    BatchReviewCount = table.Column<int>(type: "integer", nullable: false),
                    City = table.Column<string>(type: "character varying(255)", maxLength: 255, nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    IsVerified = table.Column<bool>(type: "boolean", nullable: false),
                    LastTierResetAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    Lat = table.Column<double>(type: "double precision", nullable: false),
                    Long = table.Column<double>(type: "double precision", nullable: false),
                    Name = table.Column<string>(type: "character varying(255)", maxLength: 255, nullable: false),
                    RejectReason = table.Column<string>(type: "text", nullable: true),
                    TotalRatingSum = table.Column<int>(type: "integer", nullable: false),
                    TotalReviewCount = table.Column<int>(type: "integer", nullable: false),
                    UpdatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    Ward = table.Column<string>(type: "character varying(255)", maxLength: 255, nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_GhostPins", x => x.GhostPinId);
                    table.ForeignKey(
                        name: "FK_GhostPins_Branches_LinkedBranchId",
                        column: x => x.LinkedBranchId,
                        principalTable: "Branches",
                        principalColumn: "BranchId");
                    table.ForeignKey(
                        name: "FK_GhostPins_Tiers_TierId",
                        column: x => x.TierId,
                        principalTable: "Tiers",
                        principalColumn: "TierId",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_GhostPins_Users_CreatorId",
                        column: x => x.CreatorId,
                        principalTable: "Users",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_GhostPins_CreatorId",
                table: "GhostPins",
                column: "CreatorId");

            migrationBuilder.CreateIndex(
                name: "IX_GhostPins_LinkedBranchId",
                table: "GhostPins",
                column: "LinkedBranchId");

            migrationBuilder.CreateIndex(
                name: "IX_GhostPins_TierId",
                table: "GhostPins",
                column: "TierId");
        }
    }
}
