using Microsoft.EntityFrameworkCore.Migrations;
using Npgsql.EntityFrameworkCore.PostgreSQL.Metadata;

#nullable disable

#pragma warning disable CA1814 // Prefer jagged arrays over multidimensional

namespace DAL.Migrations
{
    /// <inheritdoc />
    public partial class AddTierEntity : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<int>(
                name: "BatchRatingSum",
                table: "Branches",
                type: "integer",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.AddColumn<int>(
                name: "BatchReviewCount",
                table: "Branches",
                type: "integer",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.AddColumn<int>(
                name: "TierId",
                table: "Branches",
                type: "integer",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.CreateTable(
                name: "Tiers",
                columns: table => new
                {
                    TierId = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    Name = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: false),
                    Weight = table.Column<double>(type: "double precision", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Tiers", x => x.TierId);
                });

            migrationBuilder.InsertData(
                table: "Tiers",
                columns: new[] { "TierId", "Name", "Weight" },
                values: new object[,]
                {
                    { 1, "Warning", 0.5 },
                    { 2, "Silver", 1.0 },
                    { 3, "Gold", 1.5 },
                    { 4, "Diamond", 2.0 }
                });

            migrationBuilder.CreateIndex(
                name: "IX_Branches_TierId",
                table: "Branches",
                column: "TierId");

            migrationBuilder.AddForeignKey(
                name: "FK_Branches_Tiers_TierId",
                table: "Branches",
                column: "TierId",
                principalTable: "Tiers",
                principalColumn: "TierId",
                onDelete: ReferentialAction.Restrict);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_Branches_Tiers_TierId",
                table: "Branches");

            migrationBuilder.DropTable(
                name: "Tiers");

            migrationBuilder.DropIndex(
                name: "IX_Branches_TierId",
                table: "Branches");

            migrationBuilder.DropColumn(
                name: "BatchRatingSum",
                table: "Branches");

            migrationBuilder.DropColumn(
                name: "BatchReviewCount",
                table: "Branches");

            migrationBuilder.DropColumn(
                name: "TierId",
                table: "Branches");
        }
    }
}
