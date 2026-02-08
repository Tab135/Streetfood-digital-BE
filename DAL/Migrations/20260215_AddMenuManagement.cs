using Microsoft.EntityFrameworkCore.Migrations;
using Npgsql.EntityFrameworkCore.PostgreSQL.Metadata;

#nullable disable

namespace DAL.Migrations
{
    /// <inheritdoc />
    public partial class AddMenuManagement : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            // Create Category table
            migrationBuilder.CreateTable(
                name: "category",
                columns: table => new
                {
                    category_id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    name = table.Column<string>(type: "character varying(255)", maxLength: 255, nullable: false),
                    description = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: true),
                    image_url = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_category", x => x.category_id);
                });

            // Create Taste table
            migrationBuilder.CreateTable(
                name: "taste",
                columns: table => new
                {
                    taste_id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    name = table.Column<string>(type: "character varying(255)", maxLength: 255, nullable: false),
                    description = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_taste", x => x.taste_id);
                });

            // Create Dish table
            migrationBuilder.CreateTable(
                name: "dish",
                columns: table => new
                {
                    dish_id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    name = table.Column<string>(type: "character varying(255)", maxLength: 255, nullable: false),
                    price = table.Column<decimal>(type: "numeric(18,2)", nullable: false),
                    description = table.Column<string>(type: "character varying(1000)", maxLength: 1000, nullable: true),
                    image_url = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: true),
                    is_sold_out = table.Column<bool>(type: "boolean", nullable: false, defaultValue: false),
                    is_active = table.Column<bool>(type: "boolean", nullable: false, defaultValue: true),
                    created_at = table.Column<DateTime>(type: "timestamp without time zone", 
                        nullable: false, defaultValueSql: "CURRENT_TIMESTAMP"),
                    updated_at = table.Column<DateTime>(type: "timestamp without time zone", nullable: true),
                    branch_id = table.Column<int>(type: "integer", nullable: false),
                    category_id = table.Column<int>(type: "integer", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_dish", x => x.dish_id);
                    table.ForeignKey(
                        name: "fk_dish_branch",
                        column: x => x.branch_id,
                        principalTable: "branch",
                        principalColumn: "branch_id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "fk_dish_category",
                        column: x => x.category_id,
                        principalTable: "category",
                        principalColumn: "category_id",
                        onDelete: ReferentialAction.Restrict);
                });

            // Create DishTaste table
            migrationBuilder.CreateTable(
                name: "dish_taste",
                columns: table => new
                {
                    dish_taste_id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    dish_id = table.Column<int>(type: "integer", nullable: false),
                    taste_id = table.Column<int>(type: "integer", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_dish_taste", x => x.dish_taste_id);
                    table.ForeignKey(
                        name: "fk_dish_taste_dish",
                        column: x => x.dish_id,
                        principalTable: "dish",
                        principalColumn: "dish_id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "fk_dish_taste_taste",
                        column: x => x.taste_id,
                        principalTable: "taste",
                        principalColumn: "taste_id",
                        onDelete: ReferentialAction.Cascade);
                });

            // Create DishDietaryPreference table
            migrationBuilder.CreateTable(
                name: "dish_dietary_preference",
                columns: table => new
                {
                    dish_dietary_preference_id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    dietary_preference_id = table.Column<int>(type: "integer", nullable: false),
                    dish_id = table.Column<int>(type: "integer", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_dish_dietary_preference", x => x.dish_dietary_preference_id);
                    table.ForeignKey(
                        name: "fk_dish_dietary_preference_dish",
                        column: x => x.dish_id,
                        principalTable: "dish",
                        principalColumn: "dish_id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "fk_dish_dietary_preference_dietary",
                        column: x => x.dietary_preference_id,
                        principalTable: "dietary_preferences",
                        principalColumn: "dietary_preference_id",
                        onDelete: ReferentialAction.Cascade);
                });

            // Create indexes
            migrationBuilder.CreateIndex(
                name: "ix_dish_branch_id",
                table: "dish",
                column: "branch_id");

            migrationBuilder.CreateIndex(
                name: "ix_dish_category_id",
                table: "dish",
                column: "category_id");

            migrationBuilder.CreateIndex(
                name: "ix_dish_taste_dish_id",
                table: "dish_taste",
                column: "dish_id");

            migrationBuilder.CreateIndex(
                name: "ix_dish_taste_taste_id",
                table: "dish_taste",
                column: "taste_id");

            migrationBuilder.CreateIndex(
                name: "ix_dish_dietary_preference_dish_id",
                table: "dish_dietary_preference",
                column: "dish_id");

            migrationBuilder.CreateIndex(
                name: "ix_dish_dietary_preference_dietary_preference_id",
                table: "dish_dietary_preference",
                column: "dietary_preference_id");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "dish_dietary_preference");

            migrationBuilder.DropTable(
                name: "dish_taste");

            migrationBuilder.DropTable(
                name: "dish");

            migrationBuilder.DropTable(
                name: "taste");

            migrationBuilder.DropTable(
                name: "category");
        }
    }
}
