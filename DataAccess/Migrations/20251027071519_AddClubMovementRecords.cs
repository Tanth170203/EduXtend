using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace DataAccess.Migrations
{
    /// <inheritdoc />
    public partial class AddClubMovementRecords : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<int>(
                name: "MinScore",
                table: "MovementCriteria",
                type: "int",
                nullable: true);

            migrationBuilder.CreateTable(
                name: "ClubMovementRecords",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    ClubId = table.Column<int>(type: "int", nullable: false),
                    SemesterId = table.Column<int>(type: "int", nullable: false),
                    Month = table.Column<int>(type: "int", nullable: false),
                    ClubMeetingScore = table.Column<double>(type: "float", nullable: false),
                    EventScore = table.Column<double>(type: "float", nullable: false),
                    CompetitionScore = table.Column<double>(type: "float", nullable: false),
                    PlanScore = table.Column<double>(type: "float", nullable: false),
                    CollaborationScore = table.Column<double>(type: "float", nullable: false),
                    TotalScore = table.Column<double>(type: "float", nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "datetime2", nullable: false),
                    LastUpdated = table.Column<DateTime>(type: "datetime2", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ClubMovementRecords", x => x.Id);
                    table.ForeignKey(
                        name: "FK_ClubMovementRecords_Clubs_ClubId",
                        column: x => x.ClubId,
                        principalTable: "Clubs",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_ClubMovementRecords_Semesters_SemesterId",
                        column: x => x.SemesterId,
                        principalTable: "Semesters",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "ClubMovementRecordDetails",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    ClubMovementRecordId = table.Column<int>(type: "int", nullable: false),
                    CriterionId = table.Column<int>(type: "int", nullable: false),
                    ActivityId = table.Column<int>(type: "int", nullable: true),
                    Score = table.Column<double>(type: "float", nullable: false),
                    ScoreType = table.Column<string>(type: "nvarchar(20)", maxLength: 20, nullable: false),
                    Note = table.Column<string>(type: "nvarchar(500)", maxLength: 500, nullable: true),
                    CreatedBy = table.Column<int>(type: "int", nullable: true),
                    AwardedAt = table.Column<DateTime>(type: "datetime2", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ClubMovementRecordDetails", x => x.Id);
                    table.ForeignKey(
                        name: "FK_ClubMovementRecordDetails_Activities_ActivityId",
                        column: x => x.ActivityId,
                        principalTable: "Activities",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.SetNull);
                    table.ForeignKey(
                        name: "FK_ClubMovementRecordDetails_ClubMovementRecords_ClubMovementRecordId",
                        column: x => x.ClubMovementRecordId,
                        principalTable: "ClubMovementRecords",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_ClubMovementRecordDetails_MovementCriteria_CriterionId",
                        column: x => x.CriterionId,
                        principalTable: "MovementCriteria",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_ClubMovementRecordDetails_Users_CreatedBy",
                        column: x => x.CreatedBy,
                        principalTable: "Users",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.SetNull);
                });

            migrationBuilder.CreateIndex(
                name: "IX_ClubMovementRecordDetails_ActivityId",
                table: "ClubMovementRecordDetails",
                column: "ActivityId");

            migrationBuilder.CreateIndex(
                name: "IX_ClubMovementRecordDetails_ClubMovementRecordId_CriterionId",
                table: "ClubMovementRecordDetails",
                columns: new[] { "ClubMovementRecordId", "CriterionId" });

            migrationBuilder.CreateIndex(
                name: "IX_ClubMovementRecordDetails_CreatedBy",
                table: "ClubMovementRecordDetails",
                column: "CreatedBy");

            migrationBuilder.CreateIndex(
                name: "IX_ClubMovementRecordDetails_CriterionId",
                table: "ClubMovementRecordDetails",
                column: "CriterionId");

            migrationBuilder.CreateIndex(
                name: "IX_ClubMovementRecords_ClubId_SemesterId_Month",
                table: "ClubMovementRecords",
                columns: new[] { "ClubId", "SemesterId", "Month" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_ClubMovementRecords_SemesterId",
                table: "ClubMovementRecords",
                column: "SemesterId");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "ClubMovementRecordDetails");

            migrationBuilder.DropTable(
                name: "ClubMovementRecords");

            migrationBuilder.DropColumn(
                name: "MinScore",
                table: "MovementCriteria");
        }
    }
}
