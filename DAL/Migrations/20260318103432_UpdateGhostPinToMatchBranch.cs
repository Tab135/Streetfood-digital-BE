using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace DAL.Migrations
{
    /// <inheritdoc />
    public partial class UpdateGhostPinToMatchBranch : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(name: "Status", table: "GhostPins"); migrationBuilder.AddColumn<int>(name: "TotalReviewCount", table: "GhostPins", type: "integer", nullable: false, defaultValue: 0);

            migrationBuilder.AddColumn<double>(
                name: "AvgRating",
                table: "GhostPins",
                type: "double precision",
                nullable: false,
                defaultValue: 0.0);

            migrationBuilder.AddColumn<int>(
                name: "BatchRatingSum",
                table: "GhostPins",
                type: "integer",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.AddColumn<int>(
                name: "BatchReviewCount",
                table: "GhostPins",
                type: "integer",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.AddColumn<bool>(
                name: "IsVerified",
                table: "GhostPins",
                type: "boolean",
                nullable: false,
                defaultValue: false);

            migrationBuilder.AddColumn<DateTime>(
                name: "LastTierResetAt",
                table: "GhostPins",
                type: "timestamp with time zone",
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "TierId", table: "GhostPins", type: "integer", nullable: false, defaultValue: 2);

            migrationBuilder.AddColumn<int>(
                name: "TotalRatingSum",
                table: "GhostPins",
                type: "integer",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.CreateIndex(
                name: "IX_GhostPins_TierId",
                table: "GhostPins",
                column: "TierId");

            migrationBuilder.AddForeignKey(
                name: "FK_GhostPins_Tiers_TierId",
                table: "GhostPins",
                column: "TierId",
                principalTable: "Tiers",
                principalColumn: "TierId",
                onDelete: ReferentialAction.Cascade);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_GhostPins_Tiers_TierId",
                table: "GhostPins");

            migrationBuilder.DropIndex(
                name: "IX_GhostPins_TierId",
                table: "GhostPins");

            migrationBuilder.DropColumn(
                name: "AvgRating",
                table: "GhostPins");

            migrationBuilder.DropColumn(
                name: "BatchRatingSum",
                table: "GhostPins");

            migrationBuilder.DropColumn(
                name: "BatchReviewCount",
                table: "GhostPins");

            migrationBuilder.DropColumn(
                name: "IsVerified",
                table: "GhostPins");

            migrationBuilder.DropColumn(
                name: "LastTierResetAt",
                table: "GhostPins");

            migrationBuilder.DropColumn(
                name: "TierId",
                table: "GhostPins");

            migrationBuilder.DropColumn(
                name: "TotalRatingSum",
                table: "GhostPins");

            migrationBuilder.DropColumn(name: "TotalReviewCount", table: "GhostPins"); migrationBuilder.AddColumn<int>(name: "Status", table: "GhostPins", type: "integer", nullable: false, defaultValue: 0);
        }
    }
}



