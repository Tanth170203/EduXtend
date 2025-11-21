using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace DataAccess.Migrations
{
    /// <inheritdoc />
    public partial class AddCollaborationInvitationFields : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "CollaborationRejectionReason",
                table: "Activities",
                type: "nvarchar(500)",
                maxLength: 500,
                nullable: true);

            migrationBuilder.AddColumn<DateTime>(
                name: "CollaborationRespondedAt",
                table: "Activities",
                type: "datetime2",
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "CollaborationRespondedBy",
                table: "Activities",
                type: "int",
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "CollaborationResponderId",
                table: "Activities",
                type: "int",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "CollaborationStatus",
                table: "Activities",
                type: "nvarchar(50)",
                maxLength: 50,
                nullable: true);

            migrationBuilder.CreateIndex(
                name: "IX_Activities_CollaborationResponderId",
                table: "Activities",
                column: "CollaborationResponderId");

            migrationBuilder.AddForeignKey(
                name: "FK_Activities_Users_CollaborationResponderId",
                table: "Activities",
                column: "CollaborationResponderId",
                principalTable: "Users",
                principalColumn: "Id");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_Activities_Users_CollaborationResponderId",
                table: "Activities");

            migrationBuilder.DropIndex(
                name: "IX_Activities_CollaborationResponderId",
                table: "Activities");

            migrationBuilder.DropColumn(
                name: "CollaborationRejectionReason",
                table: "Activities");

            migrationBuilder.DropColumn(
                name: "CollaborationRespondedAt",
                table: "Activities");

            migrationBuilder.DropColumn(
                name: "CollaborationRespondedBy",
                table: "Activities");

            migrationBuilder.DropColumn(
                name: "CollaborationResponderId",
                table: "Activities");

            migrationBuilder.DropColumn(
                name: "CollaborationStatus",
                table: "Activities");
        }
    }
}
