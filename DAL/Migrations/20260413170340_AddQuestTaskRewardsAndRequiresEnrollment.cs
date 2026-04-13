using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace DAL.Migrations
{
    /// <inheritdoc />
    public partial class AddQuestTaskRewardsAndRequiresEnrollment : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            // 1. Add RequiresEnrollment to Quest
            migrationBuilder.AddColumn<bool>(
                name: "RequiresEnrollment",
                table: "Quests",
                type: "bit",
                nullable: false,
                defaultValue: true);

            // 2. Create QuestTaskRewards table
            migrationBuilder.CreateTable(
                name: "QuestTaskRewards",
                columns: table => new
                {
                    QuestTaskRewardId = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    QuestTaskId = table.Column<int>(type: "int", nullable: false),
                    RewardType = table.Column<int>(type: "int", nullable: false),
                    RewardValue = table.Column<int>(type: "int", nullable: false),
                    Quantity = table.Column<int>(type: "int", nullable: false, defaultValue: 1)
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

            // 3. Migrate existing single rewards → QuestTaskRewards rows
            migrationBuilder.Sql(@"
                INSERT INTO QuestTaskRewards (QuestTaskId, RewardType, RewardValue, Quantity)
                SELECT QuestTaskId, RewardType, RewardValue, 1
                FROM QuestTasks
                WHERE RewardType IS NOT NULL
            ");

            // 4. Drop old reward columns from QuestTasks
            migrationBuilder.DropColumn(
                name: "RewardType",
                table: "QuestTasks");

            migrationBuilder.DropColumn(
                name: "RewardValue",
                table: "QuestTasks");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            // Restore reward columns
            migrationBuilder.AddColumn<int>(
                name: "RewardType",
                table: "QuestTasks",
                type: "int",
                nullable: false,
                defaultValue: 2); // POINTS as safe default

            migrationBuilder.AddColumn<int>(
                name: "RewardValue",
                table: "QuestTasks",
                type: "int",
                nullable: false,
                defaultValue: 0);

            // Restore first reward per task
            migrationBuilder.Sql(@"
                UPDATE qt
                SET qt.RewardType = r.RewardType, qt.RewardValue = r.RewardValue
                FROM QuestTasks qt
                INNER JOIN (
                    SELECT QuestTaskId, RewardType, RewardValue,
                           ROW_NUMBER() OVER (PARTITION BY QuestTaskId ORDER BY QuestTaskRewardId) AS rn
                    FROM QuestTaskRewards
                ) r ON qt.QuestTaskId = r.QuestTaskId AND r.rn = 1
            ");

            migrationBuilder.DropTable(name: "QuestTaskRewards");

            migrationBuilder.DropColumn(
                name: "RequiresEnrollment",
                table: "Quests");
        }
    }
}
