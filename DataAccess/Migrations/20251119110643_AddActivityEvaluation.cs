using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace DataAccess.Migrations
{
    /// <inheritdoc />
    public partial class AddActivityEvaluation : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "ActivityEvaluations",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    ActivityId = table.Column<int>(type: "int", nullable: false),
                    ExpectedParticipants = table.Column<int>(type: "int", nullable: false),
                    ActualParticipants = table.Column<int>(type: "int", nullable: false),
                    Reason = table.Column<string>(type: "nvarchar(1000)", maxLength: 1000, nullable: true),
                    CommunicationScore = table.Column<int>(type: "int", nullable: false),
                    OrganizationScore = table.Column<int>(type: "int", nullable: false),
                    HostScore = table.Column<int>(type: "int", nullable: false),
                    SpeakerScore = table.Column<int>(type: "int", nullable: false),
                    Success = table.Column<string>(type: "nvarchar(2000)", maxLength: 2000, nullable: false),
                    Limitations = table.Column<string>(type: "nvarchar(2000)", maxLength: 2000, nullable: true),
                    ImprovementMeasures = table.Column<string>(type: "nvarchar(2000)", maxLength: 2000, nullable: true),
                    AverageScore = table.Column<double>(type: "float", nullable: false),
                    CreatedById = table.Column<int>(type: "int", nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "datetime2", nullable: false),
                    UpdatedAt = table.Column<DateTime>(type: "datetime2", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ActivityEvaluations", x => x.Id);
                    table.ForeignKey(
                        name: "FK_ActivityEvaluations_Activities_ActivityId",
                        column: x => x.ActivityId,
                        principalTable: "Activities",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_ActivityEvaluations_Users_CreatedById",
                        column: x => x.CreatedById,
                        principalTable: "Users",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateIndex(
                name: "IX_ActivityEvaluations_ActivityId",
                table: "ActivityEvaluations",
                column: "ActivityId",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_ActivityEvaluations_CreatedById",
                table: "ActivityEvaluations",
                column: "CreatedById");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "ActivityEvaluations");
        }
    }
}
