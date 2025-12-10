using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace DataAccess.Migrations
{
    /// <inheritdoc />
    public partial class AddMonthlyReportFieldsToPlan : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            // Drop index only if it exists
            migrationBuilder.Sql(@"
                IF EXISTS (SELECT * FROM sys.indexes WHERE name = 'IX_Plans_ClubId' AND object_id = OBJECT_ID('Plans'))
                BEGIN
                    DROP INDEX [IX_Plans_ClubId] ON [Plans]
                END
            ");

            // Add columns only if they don't exist
            migrationBuilder.Sql(@"
                IF NOT EXISTS (SELECT * FROM sys.columns WHERE object_id = OBJECT_ID('Plans') AND name = 'ReportActivityIds')
                BEGIN
                    ALTER TABLE [Plans] ADD [ReportActivityIds] nvarchar(max) NULL
                END
            ");

            migrationBuilder.Sql(@"
                IF NOT EXISTS (SELECT * FROM sys.columns WHERE object_id = OBJECT_ID('Plans') AND name = 'ReportMonth')
                BEGIN
                    ALTER TABLE [Plans] ADD [ReportMonth] int NULL
                END
            ");

            migrationBuilder.Sql(@"
                IF NOT EXISTS (SELECT * FROM sys.columns WHERE object_id = OBJECT_ID('Plans') AND name = 'ReportSnapshot')
                BEGIN
                    ALTER TABLE [Plans] ADD [ReportSnapshot] nvarchar(max) NULL
                END
            ");

            migrationBuilder.Sql(@"
                IF NOT EXISTS (SELECT * FROM sys.columns WHERE object_id = OBJECT_ID('Plans') AND name = 'ReportType')
                BEGIN
                    ALTER TABLE [Plans] ADD [ReportType] nvarchar(50) NULL
                END
            ");

            migrationBuilder.Sql(@"
                IF NOT EXISTS (SELECT * FROM sys.columns WHERE object_id = OBJECT_ID('Plans') AND name = 'ReportYear')
                BEGIN
                    ALTER TABLE [Plans] ADD [ReportYear] int NULL
                END
            ");

            // Create index only if it doesn't exist
            migrationBuilder.Sql(@"
                IF NOT EXISTS (SELECT * FROM sys.indexes WHERE name = 'IX_Plans_ClubId_ReportMonth_ReportYear_ReportType' AND object_id = OBJECT_ID('Plans'))
                BEGIN
                    CREATE INDEX [IX_Plans_ClubId_ReportMonth_ReportYear_ReportType] ON [Plans] ([ClubId], [ReportMonth], [ReportYear], [ReportType])
                END
            ");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_Plans_ClubId_ReportMonth_ReportYear_ReportType",
                table: "Plans");

            migrationBuilder.DropColumn(
                name: "ReportActivityIds",
                table: "Plans");

            migrationBuilder.DropColumn(
                name: "ReportMonth",
                table: "Plans");

            migrationBuilder.DropColumn(
                name: "ReportSnapshot",
                table: "Plans");

            migrationBuilder.DropColumn(
                name: "ReportType",
                table: "Plans");

            migrationBuilder.DropColumn(
                name: "ReportYear",
                table: "Plans");

            migrationBuilder.CreateIndex(
                name: "IX_Plans_ClubId",
                table: "Plans",
                column: "ClubId");
        }
    }
}
