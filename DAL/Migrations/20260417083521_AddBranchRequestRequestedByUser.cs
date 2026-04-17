using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace DAL.Migrations
{
    /// <inheritdoc />
    public partial class AddBranchRequestRequestedByUser : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<int>(
                name: "RequestedByUserId",
                table: "BranchRequests",
                type: "integer",
                nullable: true);

            migrationBuilder.CreateIndex(
                name: "IX_BranchRequests_RequestedByUserId",
                table: "BranchRequests",
                column: "RequestedByUserId");

            migrationBuilder.AddForeignKey(
                name: "FK_BranchRequests_Users_RequestedByUserId",
                table: "BranchRequests",
                column: "RequestedByUserId",
                principalTable: "Users",
                principalColumn: "Id",
                onDelete: ReferentialAction.SetNull);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_BranchRequests_Users_RequestedByUserId",
                table: "BranchRequests");

            migrationBuilder.DropIndex(
                name: "IX_BranchRequests_RequestedByUserId",
                table: "BranchRequests");

            migrationBuilder.DropColumn(
                name: "RequestedByUserId",
                table: "BranchRequests");
        }
    }
}
