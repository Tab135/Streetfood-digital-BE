using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace DAL.Migrations
{
    /// <inheritdoc />
    public partial class AddOrderCommissionRate : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<decimal>(
                name: "CommissionRate",
                table: "Orders",
                type: "numeric",
                nullable: true);

            migrationBuilder.Sql("INSERT INTO \"Settings\" (\"Name\", \"Value\") SELECT 'VendorOrderCommissionPercent', '10' WHERE NOT EXISTS (SELECT 1 FROM \"Settings\" WHERE \"Name\" = 'VendorOrderCommissionPercent');");
        }

    protected override void Down(MigrationBuilder migrationBuilder)
        {

            migrationBuilder.DropColumn(
                name: "CommissionRate",
                table: "Orders");
        }
    }
}
