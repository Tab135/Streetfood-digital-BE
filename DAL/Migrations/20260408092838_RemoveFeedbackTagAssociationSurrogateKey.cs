using Microsoft.EntityFrameworkCore.Migrations;
using Npgsql.EntityFrameworkCore.PostgreSQL.Metadata;

#nullable disable

namespace DAL.Migrations
{
    /// <inheritdoc />
    public partial class RemoveFeedbackTagAssociationSurrogateKey : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropPrimaryKey(
                name: "PK_FeedbackTagAssociations",
                table: "FeedbackTagAssociations");

            migrationBuilder.DropIndex(
                name: "IX_FeedbackTagAssociations_FeedbackId",
                table: "FeedbackTagAssociations");

            migrationBuilder.DropColumn(
                name: "FeedbackTagId",
                table: "FeedbackTagAssociations");

            migrationBuilder.AddPrimaryKey(
                name: "PK_FeedbackTagAssociations",
                table: "FeedbackTagAssociations",
                columns: new[] { "FeedbackId", "TagId" });
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropPrimaryKey(
                name: "PK_FeedbackTagAssociations",
                table: "FeedbackTagAssociations");

            migrationBuilder.AddColumn<int>(
                name: "FeedbackTagId",
                table: "FeedbackTagAssociations",
                type: "integer",
                nullable: false,
                defaultValue: 0)
                .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn);

            migrationBuilder.AddPrimaryKey(
                name: "PK_FeedbackTagAssociations",
                table: "FeedbackTagAssociations",
                column: "FeedbackTagId");

            migrationBuilder.CreateIndex(
                name: "IX_FeedbackTagAssociations_FeedbackId",
                table: "FeedbackTagAssociations",
                column: "FeedbackId");
        }
    }
}
