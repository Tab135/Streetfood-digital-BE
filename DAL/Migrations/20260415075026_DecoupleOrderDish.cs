using Microsoft.EntityFrameworkCore.Migrations;
using Npgsql.EntityFrameworkCore.PostgreSQL.Metadata;

#nullable disable

namespace DAL.Migrations
{
    /// <inheritdoc />
    public partial class DecoupleOrderDish : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_OrderDishes_BranchDishes_BranchId_DishId",
                table: "OrderDishes");

            migrationBuilder.DropPrimaryKey(
                name: "PK_OrderDishes",
                table: "OrderDishes");

            migrationBuilder.AlterColumn<int>(
                name: "BranchId",
                table: "OrderDishes",
                type: "integer",
                nullable: true,
                oldClrType: typeof(int),
                oldType: "integer");

            migrationBuilder.AlterColumn<int>(
                name: "DishId",
                table: "OrderDishes",
                type: "integer",
                nullable: true,
                oldClrType: typeof(int),
                oldType: "integer");

            migrationBuilder.AddColumn<int>(
                name: "Id",
                table: "OrderDishes",
                type: "integer",
                nullable: false,
                defaultValue: 0)
                .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn);

            migrationBuilder.AddColumn<string>(
                name: "DishName",
                table: "OrderDishes",
                type: "text",
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddColumn<string>(
                name: "ImageUrl",
                table: "OrderDishes",
                type: "text",
                nullable: true);

            migrationBuilder.AddColumn<decimal>(
                name: "Price",
                table: "OrderDishes",
                type: "numeric",
                nullable: false,
                defaultValue: 0m);

            migrationBuilder.AddPrimaryKey(
                name: "PK_OrderDishes",
                table: "OrderDishes",
                column: "Id");

            migrationBuilder.CreateIndex(
                name: "IX_OrderDishes_OrderId",
                table: "OrderDishes",
                column: "OrderId");

            migrationBuilder.AddForeignKey(
                name: "FK_OrderDishes_BranchDishes_BranchId_DishId",
                table: "OrderDishes",
                columns: new[] { "BranchId", "DishId" },
                principalTable: "BranchDishes",
                principalColumns: new[] { "BranchId", "DishId" },
                onDelete: ReferentialAction.SetNull);


                migrationBuilder.Sql(@"
        UPDATE ""OrderDishes"" od
        SET ""DishName"" = d.""Name"",
            ""Price"" = d.""Price"",
            ""ImageUrl"" = d.""ImageUrl""
        FROM ""Dishes"" d
        WHERE od.""DishId"" = d.""DishId"";
    ");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_OrderDishes_BranchDishes_BranchId_DishId",
                table: "OrderDishes");

            migrationBuilder.DropPrimaryKey(
                name: "PK_OrderDishes",
                table: "OrderDishes");

            migrationBuilder.DropIndex(
                name: "IX_OrderDishes_OrderId",
                table: "OrderDishes");

            migrationBuilder.DropColumn(
                name: "Id",
                table: "OrderDishes");

            migrationBuilder.DropColumn(
                name: "DishName",
                table: "OrderDishes");

            migrationBuilder.DropColumn(
                name: "ImageUrl",
                table: "OrderDishes");

            migrationBuilder.DropColumn(
                name: "Price",
                table: "OrderDishes");

            migrationBuilder.AlterColumn<int>(
                name: "DishId",
                table: "OrderDishes",
                type: "integer",
                nullable: false,
                defaultValue: 0,
                oldClrType: typeof(int),
                oldType: "integer",
                oldNullable: true);

            migrationBuilder.AlterColumn<int>(
                name: "BranchId",
                table: "OrderDishes",
                type: "integer",
                nullable: false,
                defaultValue: 0,
                oldClrType: typeof(int),
                oldType: "integer",
                oldNullable: true);

            migrationBuilder.AddPrimaryKey(
                name: "PK_OrderDishes",
                table: "OrderDishes",
                columns: new[] { "OrderId", "DishId" });

            migrationBuilder.AddForeignKey(
                name: "FK_OrderDishes_BranchDishes_BranchId_DishId",
                table: "OrderDishes",
                columns: new[] { "BranchId", "DishId" },
                principalTable: "BranchDishes",
                principalColumns: new[] { "BranchId", "DishId" },
                onDelete: ReferentialAction.Restrict);
        }
    }
}
