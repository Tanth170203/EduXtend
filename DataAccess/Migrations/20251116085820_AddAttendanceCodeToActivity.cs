using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace DataAccess.Migrations
{
    /// <inheritdoc />
    public partial class AddAttendanceCodeToActivity : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "AttendanceCode",
                table: "Activities",
                type: "nvarchar(6)",
                maxLength: 6,
                nullable: true);

            migrationBuilder.CreateIndex(
                name: "IX_Activities_AttendanceCode",
                table: "Activities",
                column: "AttendanceCode");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_Activities_AttendanceCode",
                table: "Activities");

            migrationBuilder.DropColumn(
                name: "AttendanceCode",
                table: "Activities");
        }
    }
}
