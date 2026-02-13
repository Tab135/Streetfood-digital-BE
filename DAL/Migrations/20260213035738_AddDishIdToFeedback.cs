using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace DAL.Migrations
{
    /// <inheritdoc />
    public partial class AddDishIdToFeedback : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<int>(
                name: "DishId",
                table: "Feedbacks",
                type: "integer",
                nullable: true);

            migrationBuilder.CreateIndex(
                name: "IX_Feedbacks_DishId",
                table: "Feedbacks",
                column: "DishId");

            migrationBuilder.AddForeignKey(
                name: "FK_Feedbacks_Dishes_DishId",
                table: "Feedbacks",
                column: "DishId",
                principalTable: "Dishes",
                principalColumn: "DishId");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_Feedbacks_Dishes_DishId",
                table: "Feedbacks");

            migrationBuilder.DropIndex(
                name: "IX_Feedbacks_DishId",
                table: "Feedbacks");

            migrationBuilder.DropColumn(
                name: "DishId",
                table: "Feedbacks");
        }
    }
}
