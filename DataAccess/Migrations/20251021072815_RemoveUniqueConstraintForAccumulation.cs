using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace DataAccess.Migrations
{
    /// <inheritdoc />
    public partial class RemoveUniqueConstraintForAccumulation : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            // Remove unique constraint to allow accumulation of multiple scores for same criterion
            // Check if index exists before dropping
            migrationBuilder.Sql(@"
                IF EXISTS (SELECT * FROM sys.indexes WHERE name = 'IX_MovementRecordDetails_MovementRecordId_CriterionId' AND object_id = OBJECT_ID('MovementRecordDetails'))
                BEGIN
                    DROP INDEX IX_MovementRecordDetails_MovementRecordId_CriterionId ON MovementRecordDetails;
                END
            ");

            // Create non-unique index for performance
            migrationBuilder.CreateIndex(
                name: "IX_MovementRecordDetails_MovementRecordId_CriterionId_NonUnique",
                table: "MovementRecordDetails",
                columns: new[] { "MovementRecordId", "CriterionId" });
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            // Rollback: Remove non-unique index
            migrationBuilder.DropIndex(
                name: "IX_MovementRecordDetails_MovementRecordId_CriterionId_NonUnique",
                table: "MovementRecordDetails");

            // Rollback: Recreate unique constraint
            migrationBuilder.CreateIndex(
                name: "IX_MovementRecordDetails_MovementRecordId_CriterionId",
                table: "MovementRecordDetails",
                columns: new[] { "MovementRecordId", "CriterionId" },
                unique: true);
        }
    }
}
