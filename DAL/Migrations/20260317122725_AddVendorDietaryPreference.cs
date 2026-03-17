using Microsoft.EntityFrameworkCore.Migrations;
using Npgsql.EntityFrameworkCore.PostgreSQL.Metadata;

#nullable disable

namespace DAL.Migrations
{
    /// <inheritdoc />
    public partial class AddVendorDietaryPreference : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "DishDietaryPreferences");

            migrationBuilder.CreateTable(
                name: "VendorDietaryPreferences",
                columns: table => new
                {
                    VendorDietaryPreferenceId = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    VendorId = table.Column<int>(type: "integer", nullable: false),
                    DietaryPreferenceId = table.Column<int>(type: "integer", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_VendorDietaryPreferences", x => x.VendorDietaryPreferenceId);
                    table.ForeignKey(
                        name: "FK_VendorDietaryPreferences_DietaryPreferences_DietaryPreferen~",
                        column: x => x.DietaryPreferenceId,
                        principalTable: "DietaryPreferences",
                        principalColumn: "dietaryPreferenceId",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_VendorDietaryPreferences_Vendors_VendorId",
                        column: x => x.VendorId,
                        principalTable: "Vendors",
                        principalColumn: "VendorId",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_VendorDietaryPreferences_DietaryPreferenceId",
                table: "VendorDietaryPreferences",
                column: "DietaryPreferenceId");

            migrationBuilder.CreateIndex(
                name: "IX_VendorDietaryPreferences_VendorId",
                table: "VendorDietaryPreferences",
                column: "VendorId");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "VendorDietaryPreferences");

            migrationBuilder.CreateTable(
                name: "DishDietaryPreferences",
                columns: table => new
                {
                    DishDietaryPreferenceId = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    DietaryPreferenceId = table.Column<int>(type: "integer", nullable: false),
                    DishId = table.Column<int>(type: "integer", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_DishDietaryPreferences", x => x.DishDietaryPreferenceId);
                    table.ForeignKey(
                        name: "FK_DishDietaryPreferences_DietaryPreferences_DietaryPreference~",
                        column: x => x.DietaryPreferenceId,
                        principalTable: "DietaryPreferences",
                        principalColumn: "dietaryPreferenceId",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_DishDietaryPreferences_Dishes_DishId",
                        column: x => x.DishId,
                        principalTable: "Dishes",
                        principalColumn: "DishId",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_DishDietaryPreferences_DietaryPreferenceId",
                table: "DishDietaryPreferences",
                column: "DietaryPreferenceId");

            migrationBuilder.CreateIndex(
                name: "IX_DishDietaryPreferences_DishId",
                table: "DishDietaryPreferences",
                column: "DishId");
        }
    }
}
