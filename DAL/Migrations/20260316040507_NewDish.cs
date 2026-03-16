using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace DAL.Migrations
{
    /// <inheritdoc />
    public partial class NewDish : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "IsAvailable",
                table: "BranchDishes");

            migrationBuilder.AddColumn<bool>(
                name: "IsSoldOut",
                table: "BranchDishes",
                type: "boolean",
                nullable: false,
                defaultValue: false);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "IsSoldOut",
                table: "BranchDishes");

            migrationBuilder.AddColumn<bool>(
                name: "IsAvailable",
                table: "BranchDishes",
                type: "boolean",
                nullable: false,
                defaultValue: true);
        }
    }
}
