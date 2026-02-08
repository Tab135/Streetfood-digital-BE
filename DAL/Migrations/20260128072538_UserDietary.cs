using Microsoft.EntityFrameworkCore.Migrations;
using Npgsql.EntityFrameworkCore.PostgreSQL.Metadata;

#nullable disable

#pragma warning disable CA1814 // Prefer jagged arrays over multidimensional

namespace DAL.Migrations
{
    /// <inheritdoc />
    public partial class UserDietary : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AlterColumn<string>(
                name: "LastName",
                table: "Users",
                type: "text",
                nullable: false,
                defaultValue: "",
                oldClrType: typeof(string),
                oldType: "text",
                oldNullable: true);

            migrationBuilder.AlterColumn<string>(
                name: "FirstName",
                table: "Users",
                type: "text",
                nullable: false,
                defaultValue: "",
                oldClrType: typeof(string),
                oldType: "text",
                oldNullable: true);

            migrationBuilder.CreateTable(
                name: "DietaryPreferences",
                columns: table => new
                {
                    dietaryPreferenceId = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    Name = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: false),
                    Description = table.Column<string>(type: "character varying(255)", maxLength: 255, nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_DietaryPreferences", x => x.dietaryPreferenceId);
                });

            migrationBuilder.CreateTable(
                name: "UserDietaryPreferences",
                columns: table => new
                {
                    userDietaryPreferencesId = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    UserId = table.Column<int>(type: "integer", nullable: false),
                    dietaryPreferenceId = table.Column<int>(type: "integer", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_UserDietaryPreferences", x => x.userDietaryPreferencesId);
                    table.ForeignKey(
                        name: "FK_UserDietaryPreferences_DietaryPreferences_dietaryPreference~",
                        column: x => x.dietaryPreferenceId,
                        principalTable: "DietaryPreferences",
                        principalColumn: "dietaryPreferenceId",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_UserDietaryPreferences_Users_UserId",
                        column: x => x.UserId,
                        principalTable: "Users",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.InsertData(
                table: "DietaryPreferences",
                columns: new[] { "dietaryPreferenceId", "Description", "Name" },
                values: new object[,]
                {
                    { 1, "Không thịt", "Ăn chay" },
                    { 2, "Món ăn có vị cay nồng, sử dụng nhiều ớt hoặc tiêu", "Cay" },
                    { 3, "Món ăn có vị ngọt, hoặc các món tráng miệng", "Ngọt" },
                    { 4, "Hương vị đậm đà, thích hợp ăn kèm với cơm", "Mặn" },
                    { 5, "Bao gồm các loại tôm, cua, cá, mực và đồ biển khác", "Hải sản" }
                });

            migrationBuilder.CreateIndex(
                name: "IX_UserDietaryPreferences_dietaryPreferenceId",
                table: "UserDietaryPreferences",
                column: "dietaryPreferenceId");

            migrationBuilder.CreateIndex(
                name: "IX_UserDietaryPreferences_UserId",
                table: "UserDietaryPreferences",
                column: "UserId");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "UserDietaryPreferences");

            migrationBuilder.DropTable(
                name: "DietaryPreferences");

            migrationBuilder.AlterColumn<string>(
                name: "LastName",
                table: "Users",
                type: "text",
                nullable: true,
                oldClrType: typeof(string),
                oldType: "text");

            migrationBuilder.AlterColumn<string>(
                name: "FirstName",
                table: "Users",
                type: "text",
                nullable: true,
                oldClrType: typeof(string),
                oldType: "text");
        }
    }
}
