using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace DAL.Migrations
{
    /// <inheritdoc />
    public partial class RemoveCreatedByBranchIdFromCampaign : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_Campaigns_Branches_CreatedByBranchId",
                table: "Campaigns");

            migrationBuilder.DropIndex(
                name: "IX_Campaigns_CreatedByBranchId",
                table: "Campaigns");

            migrationBuilder.DropColumn(
                name: "CreatedByBranchId",
                table: "Campaigns");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<int>(
                name: "CreatedByBranchId",
                table: "Campaigns",
                type: "integer",
                nullable: true);

            migrationBuilder.CreateIndex(
                name: "IX_Campaigns_CreatedByBranchId",
                table: "Campaigns",
                column: "CreatedByBranchId");

            migrationBuilder.AddForeignKey(
                name: "FK_Campaigns_Branches_CreatedByBranchId",
                table: "Campaigns",
                column: "CreatedByBranchId",
                principalTable: "Branches",
                principalColumn: "BranchId");
        }
    }
}
