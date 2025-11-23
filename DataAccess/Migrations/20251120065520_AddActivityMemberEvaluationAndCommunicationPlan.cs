using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace DataAccess.Migrations
{
    /// <inheritdoc />
    public partial class AddActivityMemberEvaluationAndCommunicationPlan : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AlterColumn<int>(
                name: "Success",
                table: "ActivityEvaluations",
                type: "int",
                nullable: false,
                oldClrType: typeof(string),
                oldType: "nvarchar(2000)",
                oldMaxLength: 2000);

            migrationBuilder.CreateTable(
                name: "ActivityMemberEvaluations",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    ActivityScheduleAssignmentId = table.Column<int>(type: "int", nullable: false),
                    EvaluatorId = table.Column<int>(type: "int", nullable: false),
                    ResponsibilityScore = table.Column<int>(type: "int", nullable: false),
                    SkillScore = table.Column<int>(type: "int", nullable: false),
                    AttitudeScore = table.Column<int>(type: "int", nullable: false),
                    EffectivenessScore = table.Column<int>(type: "int", nullable: false),
                    AverageScore = table.Column<double>(type: "float", nullable: false),
                    Comments = table.Column<string>(type: "nvarchar(2000)", maxLength: 2000, nullable: true),
                    Strengths = table.Column<string>(type: "nvarchar(1000)", maxLength: 1000, nullable: true),
                    Improvements = table.Column<string>(type: "nvarchar(1000)", maxLength: 1000, nullable: true),
                    CreatedAt = table.Column<DateTime>(type: "datetime2", nullable: false),
                    UpdatedAt = table.Column<DateTime>(type: "datetime2", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ActivityMemberEvaluations", x => x.Id);
                    table.ForeignKey(
                        name: "FK_ActivityMemberEvaluations_ActivityScheduleAssignments_ActivityScheduleAssignmentId",
                        column: x => x.ActivityScheduleAssignmentId,
                        principalTable: "ActivityScheduleAssignments",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_ActivityMemberEvaluations_Users_EvaluatorId",
                        column: x => x.EvaluatorId,
                        principalTable: "Users",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "CommunicationPlans",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    ActivityId = table.Column<int>(type: "int", nullable: false),
                    ClubId = table.Column<int>(type: "int", nullable: false),
                    CreatedById = table.Column<int>(type: "int", nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "datetime2", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_CommunicationPlans", x => x.Id);
                    table.ForeignKey(
                        name: "FK_CommunicationPlans_Activities_ActivityId",
                        column: x => x.ActivityId,
                        principalTable: "Activities",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_CommunicationPlans_Clubs_ClubId",
                        column: x => x.ClubId,
                        principalTable: "Clubs",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_CommunicationPlans_Users_CreatedById",
                        column: x => x.CreatedById,
                        principalTable: "Users",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "CommunicationItems",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    CommunicationPlanId = table.Column<int>(type: "int", nullable: false),
                    Order = table.Column<int>(type: "int", nullable: false),
                    Content = table.Column<string>(type: "nvarchar(500)", maxLength: 500, nullable: false),
                    ScheduledDate = table.Column<DateTime>(type: "datetime2", nullable: false),
                    ResponsiblePerson = table.Column<string>(type: "nvarchar(200)", maxLength: 200, nullable: true),
                    Notes = table.Column<string>(type: "nvarchar(1000)", maxLength: 1000, nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_CommunicationItems", x => x.Id);
                    table.ForeignKey(
                        name: "FK_CommunicationItems_CommunicationPlans_CommunicationPlanId",
                        column: x => x.CommunicationPlanId,
                        principalTable: "CommunicationPlans",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_ActivityMemberEvaluations_ActivityScheduleAssignmentId",
                table: "ActivityMemberEvaluations",
                column: "ActivityScheduleAssignmentId",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_ActivityMemberEvaluations_EvaluatorId",
                table: "ActivityMemberEvaluations",
                column: "EvaluatorId");

            migrationBuilder.CreateIndex(
                name: "IX_CommunicationItems_CommunicationPlanId_Order",
                table: "CommunicationItems",
                columns: new[] { "CommunicationPlanId", "Order" });

            migrationBuilder.CreateIndex(
                name: "IX_CommunicationPlans_ActivityId",
                table: "CommunicationPlans",
                column: "ActivityId",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_CommunicationPlans_ClubId",
                table: "CommunicationPlans",
                column: "ClubId");

            migrationBuilder.CreateIndex(
                name: "IX_CommunicationPlans_CreatedById",
                table: "CommunicationPlans",
                column: "CreatedById");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "ActivityMemberEvaluations");

            migrationBuilder.DropTable(
                name: "CommunicationItems");

            migrationBuilder.DropTable(
                name: "CommunicationPlans");

            migrationBuilder.AlterColumn<string>(
                name: "Success",
                table: "ActivityEvaluations",
                type: "nvarchar(2000)",
                maxLength: 2000,
                nullable: false,
                oldClrType: typeof(int),
                oldType: "int");
        }
    }
}
