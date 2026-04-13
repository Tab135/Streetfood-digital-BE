using Microsoft.EntityFrameworkCore.Migrations;
using Npgsql.EntityFrameworkCore.PostgreSQL.Metadata;

#nullable disable

namespace DAL.Migrations
{
    /// <inheritdoc />
    public partial class FixPendingModelChanges : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "RewardType",
                table: "QuestTasks");

            migrationBuilder.DropColumn(
                name: "RewardValue",
                table: "QuestTasks");

            migrationBuilder.AddColumn<bool>(
                name: "RequiresEnrollment",
                table: "Quests",
                type: "boolean",
                nullable: false,
                defaultValue: false);

            migrationBuilder.CreateTable(
                name: "QuestTaskRewards",
                columns: table => new
                {
                    QuestTaskRewardId = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    QuestTaskId = table.Column<int>(type: "integer", nullable: false),
                    RewardType = table.Column<int>(type: "integer", nullable: false),
                    RewardValue = table.Column<int>(type: "integer", nullable: false),
                    Quantity = table.Column<int>(type: "integer", nullable: false, defaultValue: 1)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_QuestTaskRewards", x => x.QuestTaskRewardId);
                    table.ForeignKey(
                        name: "FK_QuestTaskRewards_QuestTasks_QuestTaskId",
                        column: x => x.QuestTaskId,
                        principalTable: "QuestTasks",
                        principalColumn: "QuestTaskId",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_QuestTaskRewards_QuestTaskId",
                table: "QuestTaskRewards",
                column: "QuestTaskId");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "QuestTaskRewards");

            migrationBuilder.DropColumn(
                name: "RequiresEnrollment",
                table: "Quests");

            migrationBuilder.AddColumn<string>(
                name: "RewardType",
                table: "QuestTasks",
                type: "character varying(50)",
                maxLength: 50,
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddColumn<int>(
                name: "RewardValue",
                table: "QuestTasks",
                type: "integer",
                nullable: false,
                defaultValue: 0);
        }
    }
}
