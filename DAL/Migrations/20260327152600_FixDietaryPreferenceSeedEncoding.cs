using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace DAL.Migrations
{
    /// <inheritdoc />
    public partial class FixDietaryPreferenceSeedEncoding : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.UpdateData(
                table: "DietaryPreferences",
                keyColumn: "dietaryPreferenceId",
                keyValue: 1,
                column: "Description",
                value: "Không thịt");

            migrationBuilder.UpdateData(
                table: "DietaryPreferences",
                keyColumn: "dietaryPreferenceId",
                keyValue: 2,
                column: "Description",
                value: "Món an có vị cay nồng, sử dụng nhiều ớt hoặc tiêu");

            migrationBuilder.UpdateData(
                table: "DietaryPreferences",
                keyColumn: "dietaryPreferenceId",
                keyValue: 3,
                columns: new[] { "Description", "Name" },
                values: new object[] { "Món an có vị ngọt, hoặc các món tráng miệng", "Ngọt" });

            migrationBuilder.UpdateData(
                table: "DietaryPreferences",
                keyColumn: "dietaryPreferenceId",
                keyValue: 4,
                columns: new[] { "Description", "Name" },
                values: new object[] { "Hương vị đậm đà, thích hợp ăn kèm với cơm", "Mặn" });

            migrationBuilder.UpdateData(
                table: "DietaryPreferences",
                keyColumn: "dietaryPreferenceId",
                keyValue: 5,
                columns: new[] { "Description", "Name" },
                values: new object[] { "Bao gồm các loại tôm, cua, cá, mực và các món biển khác", "Hải sản" });
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
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
                columns: new[] { "Description", "Name" },
                values: new object[] { "M�n an c� v? ng?t, ho?c c�c m�n tr�ng mi?ng", "Ng?t" });

            migrationBuilder.UpdateData(
                table: "DietaryPreferences",
                keyColumn: "dietaryPreferenceId",
                keyValue: 4,
                columns: new[] { "Description", "Name" },
                values: new object[] { "Huong v? d?m d�, th�ch h?p an k�m v?i com", "M?n" });

            migrationBuilder.UpdateData(
                table: "DietaryPreferences",
                keyColumn: "dietaryPreferenceId",
                keyValue: 5,
                columns: new[] { "Description", "Name" },
                values: new object[] { "Bao g?m c�c lo?i t�m, cua, c�, m?c v� d? bi?n kh�c", "H?i s?n" });
        }
    }
}
