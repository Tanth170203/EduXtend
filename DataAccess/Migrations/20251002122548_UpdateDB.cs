using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace DataAccess.Migrations
{
    /// <inheritdoc />
    public partial class UpdateDB : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "ClassMeetings");

            migrationBuilder.DropColumn(
                name: "MaxPointsPerStudent",
                table: "ActivityPointRules");

            migrationBuilder.DropColumn(
                name: "MaxTimesPerStudent",
                table: "ActivityPointRules");

            migrationBuilder.AlterColumn<DateTime>(
                name: "UpdatedAt",
                table: "Clubs",
                type: "datetime2",
                nullable: true,
                oldClrType: typeof(DateTime),
                oldType: "datetime2");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AlterColumn<DateTime>(
                name: "UpdatedAt",
                table: "Clubs",
                type: "datetime2",
                nullable: false,
                defaultValue: new DateTime(1, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified),
                oldClrType: typeof(DateTime),
                oldType: "datetime2",
                oldNullable: true);

            migrationBuilder.AddColumn<int>(
                name: "MaxPointsPerStudent",
                table: "ActivityPointRules",
                type: "int",
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "MaxTimesPerStudent",
                table: "ActivityPointRules",
                type: "int",
                nullable: true);

            migrationBuilder.CreateTable(
                name: "ClassMeetings",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    OrganizedById = table.Column<int>(type: "int", nullable: false),
                    SemesterId = table.Column<int>(type: "int", nullable: false),
                    MeetingDate = table.Column<DateTime>(type: "datetime2", nullable: false),
                    MeetingUrl = table.Column<string>(type: "nvarchar(300)", maxLength: 300, nullable: true),
                    Note = table.Column<string>(type: "nvarchar(500)", maxLength: 500, nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ClassMeetings", x => x.Id);
                    table.ForeignKey(
                        name: "FK_ClassMeetings_Semesters_SemesterId",
                        column: x => x.SemesterId,
                        principalTable: "Semesters",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_ClassMeetings_Users_OrganizedById",
                        column: x => x.OrganizedById,
                        principalTable: "Users",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateIndex(
                name: "IX_ClassMeetings_OrganizedById",
                table: "ClassMeetings",
                column: "OrganizedById");

            migrationBuilder.CreateIndex(
                name: "IX_ClassMeetings_SemesterId",
                table: "ClassMeetings",
                column: "SemesterId");
        }
    }
}
