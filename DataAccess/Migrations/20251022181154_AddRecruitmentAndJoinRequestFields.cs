using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace DataAccess.Migrations
{
    /// <inheritdoc />
    public partial class AddRecruitmentAndJoinRequestFields : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "CvUrl",
                table: "JoinRequests",
                type: "nvarchar(255)",
                maxLength: 255,
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "DepartmentId",
                table: "JoinRequests",
                type: "int",
                nullable: true);

            migrationBuilder.AddColumn<bool>(
                name: "IsRecruitmentOpen",
                table: "Clubs",
                type: "bit",
                nullable: false,
                defaultValue: false);

            migrationBuilder.CreateIndex(
                name: "IX_JoinRequests_DepartmentId",
                table: "JoinRequests",
                column: "DepartmentId");

            migrationBuilder.AddForeignKey(
                name: "FK_JoinRequests_ClubDepartments_DepartmentId",
                table: "JoinRequests",
                column: "DepartmentId",
                principalTable: "ClubDepartments",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_JoinRequests_ClubDepartments_DepartmentId",
                table: "JoinRequests");

            migrationBuilder.DropIndex(
                name: "IX_JoinRequests_DepartmentId",
                table: "JoinRequests");

            migrationBuilder.DropColumn(
                name: "CvUrl",
                table: "JoinRequests");

            migrationBuilder.DropColumn(
                name: "DepartmentId",
                table: "JoinRequests");

            migrationBuilder.DropColumn(
                name: "IsRecruitmentOpen",
                table: "Clubs");
        }
    }
}
