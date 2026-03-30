using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace DAL.Migrations
{
    /// <inheritdoc />
    public partial class AddIsStandaloneAndRemoveVisitTasks : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<bool>(
                name: "IsStandalone",
                table: "Quests",
                type: "boolean",
                nullable: false,
                defaultValue: false);

            // Backfill: quests without a campaign are standalone
            migrationBuilder.Sql(
                "UPDATE \"Quests\" SET \"IsStandalone\" = TRUE WHERE \"CampaignId\" IS NULL;");

            // Remove all VISIT quest tasks and their user progress records.
            // Type is stored as character varying — use string literal, not integer.
            // UserQuestTasks referencing these tasks are deleted first (FK restrict guard).
            migrationBuilder.Sql(
                "DELETE FROM \"UserQuestTasks\" WHERE \"QuestTaskId\" IN (SELECT \"QuestTaskId\" FROM \"QuestTasks\" WHERE \"Type\" = 'VISIT');");
            migrationBuilder.Sql(
                "DELETE FROM \"QuestTasks\" WHERE \"Type\" = 'VISIT';");
            // Note: renumbering the C# enum integer values (SHARE 4→3, CREATE_GHOST_PIN 5→4)
            // requires no DB changes — the column stores string names, not integers.
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "IsStandalone",
                table: "Quests");
        }
    }
}
