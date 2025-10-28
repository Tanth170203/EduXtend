using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace DataAccess.Migrations
{
    /// <inheritdoc />
    public partial class AddAuditFieldsToMovementRecordDetails : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<int>(
                name: "ActivityId",
                table: "MovementRecordDetails",
                type: "int",
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "CreatedBy",
                table: "MovementRecordDetails",
                type: "int",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "Note",
                table: "MovementRecordDetails",
                type: "nvarchar(max)",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "ScoreType",
                table: "MovementRecordDetails",
                type: "nvarchar(max)",
                nullable: false,
                defaultValue: "");

            migrationBuilder.CreateIndex(
                name: "IX_MovementRecordDetails_ActivityId",
                table: "MovementRecordDetails",
                column: "ActivityId");

            migrationBuilder.CreateIndex(
                name: "IX_MovementRecordDetails_CreatedBy",
                table: "MovementRecordDetails",
                column: "CreatedBy");

            migrationBuilder.CreateIndex(
                name: "IX_MovementRecordDetails_MovementRecordId_CriterionId_ActivityId",
                table: "MovementRecordDetails",
                columns: new[] { "MovementRecordId", "CriterionId", "ActivityId" });

            migrationBuilder.AddForeignKey(
                name: "FK_MovementRecordDetails_Activities_ActivityId",
                table: "MovementRecordDetails",
                column: "ActivityId",
                principalTable: "Activities",
                principalColumn: "Id");

            migrationBuilder.AddForeignKey(
                name: "FK_MovementRecordDetails_Users_CreatedBy",
                table: "MovementRecordDetails",
                column: "CreatedBy",
                principalTable: "Users",
                principalColumn: "Id",
                onDelete: ReferentialAction.SetNull);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_MovementRecordDetails_Activities_ActivityId",
                table: "MovementRecordDetails");

            migrationBuilder.DropForeignKey(
                name: "FK_MovementRecordDetails_Users_CreatedBy",
                table: "MovementRecordDetails");

            migrationBuilder.DropIndex(
                name: "IX_MovementRecordDetails_ActivityId",
                table: "MovementRecordDetails");

            migrationBuilder.DropIndex(
                name: "IX_MovementRecordDetails_CreatedBy",
                table: "MovementRecordDetails");

            migrationBuilder.DropIndex(
                name: "IX_MovementRecordDetails_MovementRecordId_CriterionId_ActivityId",
                table: "MovementRecordDetails");

            migrationBuilder.DropColumn(
                name: "ActivityId",
                table: "MovementRecordDetails");

            migrationBuilder.DropColumn(
                name: "CreatedBy",
                table: "MovementRecordDetails");

            migrationBuilder.DropColumn(
                name: "Note",
                table: "MovementRecordDetails");

            migrationBuilder.DropColumn(
                name: "ScoreType",
                table: "MovementRecordDetails");
        }
    }
}
