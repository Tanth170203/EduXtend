using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace DataAccess.Migrations
{
    /// <inheritdoc />
    public partial class RemoveCollaborationResponderId : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_Activities_Users_CollaborationResponderId",
                table: "Activities");

            migrationBuilder.DropIndex(
                name: "IX_Activities_CollaborationResponderId",
                table: "Activities");

            migrationBuilder.DropColumn(
                name: "CollaborationResponderId",
                table: "Activities");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<int>(
                name: "CollaborationResponderId",
                table: "Activities",
                type: "int",
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
    }
}
