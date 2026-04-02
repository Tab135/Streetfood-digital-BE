using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace DAL.Migrations
{
    /// <inheritdoc />
    public partial class AddCustomerXPAndTier : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<int>(
                name: "TierId",
                table: "Users",
                type: "integer",
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "XP",
                table: "Users",
                type: "integer",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.AddColumn<int>(
                name: "RewardXP",
                table: "QuestTasks",
                type: "integer",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.CreateIndex(
                name: "IX_Users_TierId",
                table: "Users",
                column: "TierId");

            migrationBuilder.AddForeignKey(
                name: "FK_Users_Tiers_TierId",
                table: "Users",
                column: "TierId",
                principalTable: "Tiers",
                principalColumn: "TierId",
                onDelete: ReferentialAction.SetNull);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_Users_Tiers_TierId",
                table: "Users");

            migrationBuilder.DropIndex(
                name: "IX_Users_TierId",
                table: "Users");

            migrationBuilder.DropColumn(
                name: "TierId",
                table: "Users");

            migrationBuilder.DropColumn(
                name: "XP",
                table: "Users");

            migrationBuilder.DropColumn(
                name: "RewardXP",
                table: "QuestTasks");
        }
    }
}
