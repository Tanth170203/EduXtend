using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace DataAccess.Migrations
{
    /// <inheritdoc />
    public partial class Update_EvidenceDocument : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_EvidenceDocuments_ActivityRegistrations_ActivityRegistrationId",
                table: "EvidenceDocuments");

            migrationBuilder.DropIndex(
                name: "IX_EvidenceDocuments_ActivityRegistrationId",
                table: "EvidenceDocuments");

            migrationBuilder.DropColumn(
                name: "ActivityRegistrationId",
                table: "EvidenceDocuments");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<int>(
                name: "ActivityRegistrationId",
                table: "EvidenceDocuments",
                type: "int",
                nullable: true);

            migrationBuilder.CreateIndex(
                name: "IX_EvidenceDocuments_ActivityRegistrationId",
                table: "EvidenceDocuments",
                column: "ActivityRegistrationId");

            migrationBuilder.AddForeignKey(
                name: "FK_EvidenceDocuments_ActivityRegistrations_ActivityRegistrationId",
                table: "EvidenceDocuments",
                column: "ActivityRegistrationId",
                principalTable: "ActivityRegistrations",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);
        }
    }
}
