using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace DAL.Migrations
{
    /// <inheritdoc />
    public partial class BranchRequest_BranchId_OneToMany : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_BranchRequests_BranchId",
                table: "BranchRequests");

            migrationBuilder.UpdateData(
                table: "DietaryPreferences",
                keyColumn: "dietaryPreferenceId",
                keyValue: 1,
                column: "Description",
                value: "Kh�ng th?t");

            migrationBuilder.UpdateData(
                table: "DietaryPreferences",
                keyColumn: "dietaryPreferenceId",
                keyValue: 2,
                column: "Description",
                value: "M�n an c� v? cay n?ng, s? d?ng nhi?u ?t ho?c ti�u");

            migrationBuilder.UpdateData(
                table: "DietaryPreferences",
                keyColumn: "dietaryPreferenceId",
                keyValue: 3,
                column: "Description",
                value: "M�n an c� v? ng?t, ho?c c�c m�n tr�ng mi?ng");

            migrationBuilder.UpdateData(
                table: "DietaryPreferences",
                keyColumn: "dietaryPreferenceId",
                keyValue: 4,
                column: "Description",
                value: "Huong v? d?m d�, th�ch h?p an k�m v?i com");

            migrationBuilder.UpdateData(
                table: "DietaryPreferences",
                keyColumn: "dietaryPreferenceId",
                keyValue: 5,
                column: "Description",
                value: "Bao g?m c�c lo?i t�m, cua, c�, m?c v� d? bi?n kh�c");

            migrationBuilder.CreateIndex(
                name: "IX_BranchRequests_BranchId",
                table: "BranchRequests",
                column: "BranchId");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_BranchRequests_BranchId",
                table: "BranchRequests");

            migrationBuilder.UpdateData(
                table: "DietaryPreferences",
                keyColumn: "dietaryPreferenceId",
                keyValue: 1,
                column: "Description",
                value: "Không th?t");

            migrationBuilder.UpdateData(
                table: "DietaryPreferences",
                keyColumn: "dietaryPreferenceId",
                keyValue: 2,
                column: "Description",
                value: "Món an có v? cay n?ng, s? d?ng nhi?u ?t ho?c tiêu");

            migrationBuilder.UpdateData(
                table: "DietaryPreferences",
                keyColumn: "dietaryPreferenceId",
                keyValue: 3,
                column: "Description",
                value: "Món an có v? ng?t, ho?c các món tráng mi?ng");

            migrationBuilder.UpdateData(
                table: "DietaryPreferences",
                keyColumn: "dietaryPreferenceId",
                keyValue: 4,
                column: "Description",
                value: "Huong v? d?m dà, thích h?p an kèm v?i com");

            migrationBuilder.UpdateData(
                table: "DietaryPreferences",
                keyColumn: "dietaryPreferenceId",
                keyValue: 5,
                column: "Description",
                value: "Bao g?m các lo?i tôm, cua, cá, m?c và d? bi?n khác");

            migrationBuilder.CreateIndex(
                name: "IX_BranchRequests_BranchId",
                table: "BranchRequests",
                column: "BranchId",
                unique: true);
        }
    }
}
