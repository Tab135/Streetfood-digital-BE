using System;
using Microsoft.EntityFrameworkCore.Migrations;
using Npgsql.EntityFrameworkCore.PostgreSQL.Metadata;

#nullable disable

namespace DAL.Migrations
{
    /// <inheritdoc />
    public partial class BranchGhostPin : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "BranchRegisterRequests");

            migrationBuilder.CreateTable(
                name: "BranchRequests",
                columns: table => new
                {
                    BranchRequestId = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    BranchId = table.Column<int>(type: "integer", nullable: false),
                    LicenseUrl = table.Column<string>(type: "text", nullable: true),
                    Type = table.Column<int>(type: "integer", nullable: false),
                    Status = table.Column<int>(type: "integer", nullable: false),
                    RejectReason = table.Column<string>(type: "text", nullable: true),
                    CreatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false, defaultValueSql: "CURRENT_TIMESTAMP"),
                    UpdatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false, defaultValueSql: "CURRENT_TIMESTAMP")
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_BranchRequests", x => x.BranchRequestId);
                    table.ForeignKey(
                        name: "FK_BranchRequests_Branches_BranchId",
                        column: x => x.BranchId,
                        principalTable: "Branches",
                        principalColumn: "BranchId",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.UpdateData(
                table: "DietaryPreferences",
                keyColumn: "dietaryPreferenceId",
                keyValue: 1,
                columns: new[] { "Description", "Name" },
                values: new object[] { "Không th?t", "An chay" });

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
                columns: new[] { "Description", "Name" },
                values: new object[] { "Món an có v? ng?t, ho?c các món tráng mi?ng", "Ng?t" });

            migrationBuilder.UpdateData(
                table: "DietaryPreferences",
                keyColumn: "dietaryPreferenceId",
                keyValue: 4,
                columns: new[] { "Description", "Name" },
                values: new object[] { "Huong v? d?m dà, thích h?p an kèm v?i com", "M?n" });

            migrationBuilder.UpdateData(
                table: "DietaryPreferences",
                keyColumn: "dietaryPreferenceId",
                keyValue: 5,
                columns: new[] { "Description", "Name" },
                values: new object[] { "Bao g?m các lo?i tôm, cua, cá, m?c và d? bi?n khác", "H?i s?n" });

            migrationBuilder.CreateIndex(
                name: "IX_BranchRequests_BranchId",
                table: "BranchRequests",
                column: "BranchId",
                unique: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "BranchRequests");

            migrationBuilder.CreateTable(
                name: "BranchRegisterRequests",
                columns: table => new
                {
                    BranchRegisterRequestId = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    BranchId = table.Column<int>(type: "integer", nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false, defaultValueSql: "CURRENT_TIMESTAMP"),
                    LicenseUrl = table.Column<string>(type: "text", nullable: true),
                    RejectReason = table.Column<string>(type: "text", nullable: true),
                    Status = table.Column<int>(type: "integer", nullable: false),
                    UpdatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false, defaultValueSql: "CURRENT_TIMESTAMP")
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_BranchRegisterRequests", x => x.BranchRegisterRequestId);
                    table.ForeignKey(
                        name: "FK_BranchRegisterRequests_Branches_BranchId",
                        column: x => x.BranchId,
                        principalTable: "Branches",
                        principalColumn: "BranchId",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.UpdateData(
                table: "DietaryPreferences",
                keyColumn: "dietaryPreferenceId",
                keyValue: 1,
                columns: new[] { "Description", "Name" },
                values: new object[] { "Không thịt", "Ăn chay" });

            migrationBuilder.UpdateData(
                table: "DietaryPreferences",
                keyColumn: "dietaryPreferenceId",
                keyValue: 2,
                column: "Description",
                value: "Món ăn có vị cay nồng, sử dụng nhiều ớt hoặc tiêu");

            migrationBuilder.UpdateData(
                table: "DietaryPreferences",
                keyColumn: "dietaryPreferenceId",
                keyValue: 3,
                columns: new[] { "Description", "Name" },
                values: new object[] { "Món ăn có vị ngọt, hoặc các món tráng miệng", "Ngọt" });

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
                values: new object[] { "Bao gồm các loại tôm, cua, cá, mực và đồ biển khác", "Hải sản" });

            migrationBuilder.CreateIndex(
                name: "IX_BranchRegisterRequests_BranchId",
                table: "BranchRegisterRequests",
                column: "BranchId",
                unique: true);
        }
    }
}
