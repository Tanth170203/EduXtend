using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace DataAccess.Migrations
{
    /// <inheritdoc />
    public partial class AddActivityCollaborationFields : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<int>(
                name: "ClubCollaborationId",
                table: "Activities",
                type: "int",
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "CollaborationPoint",
                table: "Activities",
                type: "int",
                nullable: true);

            migrationBuilder.CreateIndex(
                name: "IX_Activities_ClubCollaborationId",
                table: "Activities",
                column: "ClubCollaborationId");

            migrationBuilder.AddForeignKey(
                name: "FK_Activities_Clubs_ClubCollaborationId",
                table: "Activities",
                column: "ClubCollaborationId",
                principalTable: "Clubs",
                principalColumn: "Id",
                onDelete: ReferentialAction.SetNull);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_Activities_Clubs_ClubCollaborationId",
                table: "Activities");

            migrationBuilder.DropIndex(
                name: "IX_Activities_ClubCollaborationId",
                table: "Activities");

            migrationBuilder.DropColumn(
                name: "ClubCollaborationId",
                table: "Activities");

            migrationBuilder.DropColumn(
                name: "CollaborationPoint",
                table: "Activities");
        }
    }
}
