using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace DAL.Migrations
{
    /// <inheritdoc />
    public partial class UpdateCampaignVendorAndTier : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_Campaigns_Tiers_RequiredTierId",
                table: "Campaigns");

            migrationBuilder.RenameColumn(
                name: "RequiredTierId",
                table: "Campaigns",
                newName: "CreatedByVendorId");

            migrationBuilder.RenameIndex(
                name: "IX_Campaigns_RequiredTierId",
                table: "Campaigns",
                newName: "IX_Campaigns_CreatedByVendorId");

            migrationBuilder.AddForeignKey(
                name: "FK_Campaigns_Vendors_CreatedByVendorId",
                table: "Campaigns",
                column: "CreatedByVendorId",
                principalTable: "Vendors",
                principalColumn: "VendorId");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_Campaigns_Vendors_CreatedByVendorId",
                table: "Campaigns");

            migrationBuilder.RenameColumn(
                name: "CreatedByVendorId",
                table: "Campaigns",
                newName: "RequiredTierId");

            migrationBuilder.RenameIndex(
                name: "IX_Campaigns_CreatedByVendorId",
                table: "Campaigns",
                newName: "IX_Campaigns_RequiredTierId");

            migrationBuilder.AddForeignKey(
                name: "FK_Campaigns_Tiers_RequiredTierId",
                table: "Campaigns",
                column: "RequiredTierId",
                principalTable: "Tiers",
                principalColumn: "TierId");
        }
    }
}
