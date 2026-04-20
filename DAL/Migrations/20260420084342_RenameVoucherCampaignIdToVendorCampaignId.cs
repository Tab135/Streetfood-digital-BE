using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace DAL.Migrations
{
    /// <inheritdoc />
    public partial class RenameVoucherCampaignIdToVendorCampaignId : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_Vouchers_Campaigns_CampaignId",
                table: "Vouchers");

            migrationBuilder.RenameColumn(
                name: "CampaignId",
                table: "Vouchers",
                newName: "VendorCampaignId");

            migrationBuilder.RenameIndex(
                name: "IX_Vouchers_CampaignId",
                table: "Vouchers",
                newName: "IX_Vouchers_VendorCampaignId");

            migrationBuilder.AddForeignKey(
                name: "FK_Vouchers_Campaigns_VendorCampaignId",
                table: "Vouchers",
                column: "VendorCampaignId",
                principalTable: "Campaigns",
                principalColumn: "CampaignId",
                onDelete: ReferentialAction.SetNull);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_Vouchers_Campaigns_VendorCampaignId",
                table: "Vouchers");

            migrationBuilder.RenameColumn(
                name: "VendorCampaignId",
                table: "Vouchers",
                newName: "CampaignId");

            migrationBuilder.RenameIndex(
                name: "IX_Vouchers_VendorCampaignId",
                table: "Vouchers",
                newName: "IX_Vouchers_CampaignId");

            migrationBuilder.AddForeignKey(
                name: "FK_Vouchers_Campaigns_CampaignId",
                table: "Vouchers",
                column: "CampaignId",
                principalTable: "Campaigns",
                principalColumn: "CampaignId",
                onDelete: ReferentialAction.SetNull);
        }
    }
}
