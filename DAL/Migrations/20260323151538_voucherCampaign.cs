using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace DAL.Migrations
{
    /// <inheritdoc />
    public partial class voucherCampaign : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<int>(
                name: "CampaignId",
                table: "Vouchers",
                type: "integer",
                nullable: true);

            migrationBuilder.CreateIndex(
                name: "IX_Vouchers_CampaignId",
                table: "Vouchers",
                column: "CampaignId");

            migrationBuilder.AddForeignKey(
                name: "FK_Vouchers_Campaigns_CampaignId",
                table: "Vouchers",
                column: "CampaignId",
                principalTable: "Campaigns",
                principalColumn: "CampaignId",
                onDelete: ReferentialAction.SetNull);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_Vouchers_Campaigns_CampaignId",
                table: "Vouchers");

            migrationBuilder.DropIndex(
                name: "IX_Vouchers_CampaignId",
                table: "Vouchers");

            migrationBuilder.DropColumn(
                name: "CampaignId",
                table: "Vouchers");
        }
    }
}
