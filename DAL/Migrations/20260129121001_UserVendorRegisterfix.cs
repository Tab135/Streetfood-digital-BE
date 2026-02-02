using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace DAL.Migrations
{
    /// <inheritdoc />
    public partial class UserVendorRegisterfix : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_VendorRegisterRequests_Users_processedById",
                table: "VendorRegisterRequests");

            migrationBuilder.AlterColumn<int>(
                name: "processedById",
                table: "VendorRegisterRequests",
                type: "integer",
                nullable: true,
                oldClrType: typeof(int),
                oldType: "integer");

            migrationBuilder.AddForeignKey(
                name: "FK_VendorRegisterRequests_Users_processedById",
                table: "VendorRegisterRequests",
                column: "processedById",
                principalTable: "Users",
                principalColumn: "Id");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_VendorRegisterRequests_Users_processedById",
                table: "VendorRegisterRequests");

            migrationBuilder.AlterColumn<int>(
                name: "processedById",
                table: "VendorRegisterRequests",
                type: "integer",
                nullable: false,
                defaultValue: 0,
                oldClrType: typeof(int),
                oldType: "integer",
                oldNullable: true);

            migrationBuilder.AddForeignKey(
                name: "FK_VendorRegisterRequests_Users_processedById",
                table: "VendorRegisterRequests",
                column: "processedById",
                principalTable: "Users",
                principalColumn: "Id",
                onDelete: ReferentialAction.Cascade);
        }
    }
}
