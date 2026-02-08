using System;
using Microsoft.EntityFrameworkCore.Migrations;
using Npgsql.EntityFrameworkCore.PostgreSQL.Metadata;

#nullable disable

namespace DAL.Migrations
{
    /// <inheritdoc />
    public partial class AddBranchSystem : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_DayOffs_Vendors_VendorId",
                table: "DayOffs");

            migrationBuilder.DropForeignKey(
                name: "FK_WorkSchedules_Vendors_VendorId",
                table: "WorkSchedules");

            migrationBuilder.DropColumn(
                name: "AddressDetail",
                table: "Vendors");

            migrationBuilder.DropColumn(
                name: "AvgRating",
                table: "Vendors");

            migrationBuilder.DropColumn(
                name: "BuildingName",
                table: "Vendors");

            migrationBuilder.DropColumn(
                name: "City",
                table: "Vendors");

            migrationBuilder.DropColumn(
                name: "Email",
                table: "Vendors");

            migrationBuilder.DropColumn(
                name: "IsSubscribed",
                table: "Vendors");

            migrationBuilder.DropColumn(
                name: "IsVerified",
                table: "Vendors");

            migrationBuilder.DropColumn(
                name: "Lat",
                table: "Vendors");

            migrationBuilder.DropColumn(
                name: "Long",
                table: "Vendors");

            migrationBuilder.DropColumn(
                name: "PhoneNumber",
                table: "Vendors");

            migrationBuilder.DropColumn(
                name: "Ward",
                table: "Vendors");

            migrationBuilder.RenameColumn(
                name: "VendorId",
                table: "WorkSchedules",
                newName: "BranchId");

            migrationBuilder.RenameIndex(
                name: "IX_WorkSchedules_VendorId",
                table: "WorkSchedules",
                newName: "IX_WorkSchedules_BranchId");

            migrationBuilder.RenameColumn(
                name: "VendorId",
                table: "DayOffs",
                newName: "BranchId");

            migrationBuilder.RenameIndex(
                name: "IX_DayOffs_VendorId",
                table: "DayOffs",
                newName: "IX_DayOffs_BranchId");

            migrationBuilder.CreateTable(
                name: "Branches",
                columns: table => new
                {
                    BranchId = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    VendorId = table.Column<int>(type: "integer", nullable: false),
                    UserId = table.Column<int>(type: "integer", nullable: true),
                    Name = table.Column<string>(type: "character varying(255)", maxLength: 255, nullable: false),
                    PhoneNumber = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: false),
                    Email = table.Column<string>(type: "character varying(255)", maxLength: 255, nullable: false),
                    AddressDetail = table.Column<string>(type: "text", nullable: false),
                    BuildingName = table.Column<string>(type: "character varying(255)", maxLength: 255, nullable: true),
                    Ward = table.Column<string>(type: "character varying(255)", maxLength: 255, nullable: true),
                    City = table.Column<string>(type: "character varying(255)", maxLength: 255, nullable: false),
                    Lat = table.Column<double>(type: "double precision", nullable: false),
                    Long = table.Column<double>(type: "double precision", nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false, defaultValueSql: "CURRENT_TIMESTAMP"),
                    UpdatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    IsVerified = table.Column<bool>(type: "boolean", nullable: false, defaultValue: false),
                    AvgRating = table.Column<double>(type: "double precision", nullable: false),
                    IsActive = table.Column<bool>(type: "boolean", nullable: false, defaultValue: true),
                    IsSubscribed = table.Column<bool>(type: "boolean", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Branches", x => x.BranchId);
                    table.ForeignKey(
                        name: "FK_Branches_Users_UserId",
                        column: x => x.UserId,
                        principalTable: "Users",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_Branches_Vendors_VendorId",
                        column: x => x.VendorId,
                        principalTable: "Vendors",
                        principalColumn: "VendorId",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "BranchImages",
                columns: table => new
                {
                    BranchImageId = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    BranchId = table.Column<int>(type: "integer", nullable: false),
                    ImageUrl = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_BranchImages", x => x.BranchImageId);
                    table.ForeignKey(
                        name: "FK_BranchImages_Branches_BranchId",
                        column: x => x.BranchId,
                        principalTable: "Branches",
                        principalColumn: "BranchId",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_Branches_UserId",
                table: "Branches",
                column: "UserId");

            migrationBuilder.CreateIndex(
                name: "IX_Branches_VendorId",
                table: "Branches",
                column: "VendorId");

            migrationBuilder.CreateIndex(
                name: "IX_BranchImages_BranchId",
                table: "BranchImages",
                column: "BranchId");

            migrationBuilder.AddForeignKey(
                name: "FK_DayOffs_Branches_BranchId",
                table: "DayOffs",
                column: "BranchId",
                principalTable: "Branches",
                principalColumn: "BranchId",
                onDelete: ReferentialAction.Cascade);

            migrationBuilder.AddForeignKey(
                name: "FK_WorkSchedules_Branches_BranchId",
                table: "WorkSchedules",
                column: "BranchId",
                principalTable: "Branches",
                principalColumn: "BranchId",
                onDelete: ReferentialAction.Cascade);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_DayOffs_Branches_BranchId",
                table: "DayOffs");

            migrationBuilder.DropForeignKey(
                name: "FK_WorkSchedules_Branches_BranchId",
                table: "WorkSchedules");

            migrationBuilder.DropTable(
                name: "BranchImages");

            migrationBuilder.DropTable(
                name: "Branches");

            migrationBuilder.RenameColumn(
                name: "BranchId",
                table: "WorkSchedules",
                newName: "VendorId");

            migrationBuilder.RenameIndex(
                name: "IX_WorkSchedules_BranchId",
                table: "WorkSchedules",
                newName: "IX_WorkSchedules_VendorId");

            migrationBuilder.RenameColumn(
                name: "BranchId",
                table: "DayOffs",
                newName: "VendorId");

            migrationBuilder.RenameIndex(
                name: "IX_DayOffs_BranchId",
                table: "DayOffs",
                newName: "IX_DayOffs_VendorId");

            migrationBuilder.AddColumn<string>(
                name: "AddressDetail",
                table: "Vendors",
                type: "text",
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddColumn<double>(
                name: "AvgRating",
                table: "Vendors",
                type: "double precision",
                nullable: false,
                defaultValue: 0.0);

            migrationBuilder.AddColumn<string>(
                name: "BuildingName",
                table: "Vendors",
                type: "text",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "City",
                table: "Vendors",
                type: "text",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "Email",
                table: "Vendors",
                type: "character varying(255)",
                maxLength: 255,
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddColumn<bool>(
                name: "IsSubscribed",
                table: "Vendors",
                type: "boolean",
                nullable: false,
                defaultValue: false);

            migrationBuilder.AddColumn<bool>(
                name: "IsVerified",
                table: "Vendors",
                type: "boolean",
                nullable: false,
                defaultValue: false);

            migrationBuilder.AddColumn<double>(
                name: "Lat",
                table: "Vendors",
                type: "double precision",
                nullable: false,
                defaultValue: 0.0);

            migrationBuilder.AddColumn<double>(
                name: "Long",
                table: "Vendors",
                type: "double precision",
                nullable: false,
                defaultValue: 0.0);

            migrationBuilder.AddColumn<string>(
                name: "PhoneNumber",
                table: "Vendors",
                type: "character varying(50)",
                maxLength: 50,
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddColumn<string>(
                name: "Ward",
                table: "Vendors",
                type: "text",
                nullable: true);

            migrationBuilder.AddForeignKey(
                name: "FK_DayOffs_Vendors_VendorId",
                table: "DayOffs",
                column: "VendorId",
                principalTable: "Vendors",
                principalColumn: "VendorId",
                onDelete: ReferentialAction.Cascade);

            migrationBuilder.AddForeignKey(
                name: "FK_WorkSchedules_Vendors_VendorId",
                table: "WorkSchedules",
                column: "VendorId",
                principalTable: "Vendors",
                principalColumn: "VendorId",
                onDelete: ReferentialAction.Cascade);
        }
    }
}
