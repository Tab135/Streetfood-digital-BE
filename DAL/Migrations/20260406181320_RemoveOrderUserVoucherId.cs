using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace DAL.Migrations
{
    /// <inheritdoc />
    public partial class RemoveOrderUserVoucherId : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_Orders_UserVouchers_UserVoucherId",
                table: "Orders");

            migrationBuilder.DropIndex(
                name: "IX_Orders_UserVoucherId",
                table: "Orders");

            migrationBuilder.DropColumn(
                name: "UserVoucherId",
                table: "Orders");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<int>(
                name: "UserVoucherId",
                table: "Orders",
                type: "integer",
                nullable: true);

            migrationBuilder.CreateIndex(
                name: "IX_Orders_UserVoucherId",
                table: "Orders",
                column: "UserVoucherId");

            migrationBuilder.AddForeignKey(
                name: "FK_Orders_UserVouchers_UserVoucherId",
                table: "Orders",
                column: "UserVoucherId",
                principalTable: "UserVouchers",
                principalColumn: "UserVoucherId",
                onDelete: ReferentialAction.SetNull);
        }
    }
}
