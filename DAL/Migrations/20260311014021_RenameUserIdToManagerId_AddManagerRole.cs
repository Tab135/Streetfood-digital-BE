using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace DAL.Migrations
{
    /// <inheritdoc />
    public partial class RenameUserIdToManagerId_AddManagerRole : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_Branches_Users_UserId",
                table: "Branches");

            migrationBuilder.RenameColumn(
                name: "UserId",
                table: "Branches",
                newName: "ManagerId");

            migrationBuilder.RenameIndex(
                name: "IX_Branches_UserId",
                table: "Branches",
                newName: "IX_Branches_ManagerId");

            migrationBuilder.AddForeignKey(
                name: "FK_Branches_Users_ManagerId",
                table: "Branches",
                column: "ManagerId",
                principalTable: "Users",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_Branches_Users_ManagerId",
                table: "Branches");

            migrationBuilder.RenameColumn(
                name: "ManagerId",
                table: "Branches",
                newName: "UserId");

            migrationBuilder.RenameIndex(
                name: "IX_Branches_ManagerId",
                table: "Branches",
                newName: "IX_Branches_UserId");

            migrationBuilder.AddForeignKey(
                name: "FK_Branches_Users_UserId",
                table: "Branches",
                column: "UserId",
                principalTable: "Users",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);
        }
    }
}
