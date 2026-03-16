using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace DAL.Migrations
{
    /// <inheritdoc />
    public partial class VendorDishBranchAvailability : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_Dishes_Branches_BranchId",
                table: "Dishes");

            migrationBuilder.AddColumn<int>(
                name: "VendorId",
                table: "Dishes",
                type: "integer",
                nullable: true);

            migrationBuilder.Sql(@"
                UPDATE ""Dishes"" d
                SET ""VendorId"" = b.""VendorId""
                FROM ""Branches"" b
                WHERE d.""BranchId"" = b.""BranchId"";
            ");

            migrationBuilder.CreateTable(
                name: "BranchDishes",
                columns: table => new
                {
                    BranchId = table.Column<int>(type: "integer", nullable: false),
                    DishId = table.Column<int>(type: "integer", nullable: false),
                    IsAvailable = table.Column<bool>(type: "boolean", nullable: false, defaultValue: true),
                    UpdatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_BranchDishes", x => new { x.BranchId, x.DishId });
                    table.ForeignKey(
                        name: "FK_BranchDishes_Branches_BranchId",
                        column: x => x.BranchId,
                        principalTable: "Branches",
                        principalColumn: "BranchId",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_BranchDishes_Dishes_DishId",
                        column: x => x.DishId,
                        principalTable: "Dishes",
                        principalColumn: "DishId",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.Sql(@"
                INSERT INTO ""BranchDishes"" (""BranchId"", ""DishId"", ""IsAvailable"")
                SELECT ""BranchId"", ""DishId"", NOT ""IsSoldOut""
                FROM ""Dishes"";
            ");

            migrationBuilder.AlterColumn<int>(
                name: "VendorId",
                table: "Dishes",
                type: "integer",
                nullable: false,
                oldClrType: typeof(int),
                oldType: "integer",
                oldNullable: true);

            migrationBuilder.DropIndex(
                name: "IX_Dishes_BranchId",
                table: "Dishes");

            migrationBuilder.DropColumn(
                name: "BranchId",
                table: "Dishes");

            migrationBuilder.CreateIndex(
                name: "IX_Dishes_VendorId",
                table: "Dishes",
                column: "VendorId");

            migrationBuilder.CreateIndex(
                name: "IX_BranchDishes_DishId",
                table: "BranchDishes",
                column: "DishId");

            migrationBuilder.AddForeignKey(
                name: "FK_Dishes_Vendors_VendorId",
                table: "Dishes",
                column: "VendorId",
                principalTable: "Vendors",
                principalColumn: "VendorId",
                onDelete: ReferentialAction.Cascade);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_Dishes_Vendors_VendorId",
                table: "Dishes");

            migrationBuilder.AddColumn<int>(
                name: "BranchId",
                table: "Dishes",
                type: "integer",
                nullable: true);

            migrationBuilder.Sql(@"
                UPDATE ""Dishes"" d
                SET ""BranchId"" = bd.""BranchId""
                FROM ""BranchDishes"" bd
                WHERE d.""DishId"" = bd.""DishId"";
            ");

            migrationBuilder.AlterColumn<int>(
                name: "BranchId",
                table: "Dishes",
                type: "integer",
                nullable: false,
                oldClrType: typeof(int),
                oldType: "integer",
                oldNullable: true);

            migrationBuilder.CreateIndex(
                name: "IX_Dishes_BranchId",
                table: "Dishes",
                column: "BranchId");

            migrationBuilder.DropTable(
                name: "BranchDishes");

            migrationBuilder.DropIndex(
                name: "IX_Dishes_VendorId",
                table: "Dishes");

            migrationBuilder.DropColumn(
                name: "VendorId",
                table: "Dishes");

            migrationBuilder.AddForeignKey(
                name: "FK_Dishes_Branches_BranchId",
                table: "Dishes",
                column: "BranchId",
                principalTable: "Branches",
                principalColumn: "BranchId",
                onDelete: ReferentialAction.Cascade);
        }
    }
}
