using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace DAL.Migrations
{
    /// <inheritdoc />
    public partial class AddOrderAppliedVoucherTracking : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<int>(
                name: "AppliedVoucherId",
                table: "Orders",
                type: "integer",
                nullable: true);

            migrationBuilder.CreateIndex(
                name: "IX_Orders_AppliedVoucherId",
                table: "Orders",
                column: "AppliedVoucherId");

            migrationBuilder.AddForeignKey(
                name: "FK_Orders_Vouchers_AppliedVoucherId",
                table: "Orders",
                column: "AppliedVoucherId",
                principalTable: "Vouchers",
                principalColumn: "VoucherId",
                onDelete: ReferentialAction.SetNull);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_Orders_Vouchers_AppliedVoucherId",
                table: "Orders");

            migrationBuilder.DropIndex(
                name: "IX_Orders_AppliedVoucherId",
                table: "Orders");

            migrationBuilder.DropColumn(
                name: "AppliedVoucherId",
                table: "Orders");
        }
    }
}
