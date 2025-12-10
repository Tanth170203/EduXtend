using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace DataAccess.Migrations
{
    /// <inheritdoc />
    public partial class AddEditableSectionsToPlans : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "EventMediaUrls",
                table: "Plans",
                type: "nvarchar(max)",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "NextMonthPurposeAndSignificance",
                table: "Plans",
                type: "nvarchar(max)",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "ClubResponsibilities",
                table: "Plans",
                type: "nvarchar(max)",
                nullable: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "EventMediaUrls",
                table: "Plans");

            migrationBuilder.DropColumn(
                name: "NextMonthPurposeAndSignificance",
                table: "Plans");

            migrationBuilder.DropColumn(
                name: "ClubResponsibilities",
                table: "Plans");
        }
    }
}
