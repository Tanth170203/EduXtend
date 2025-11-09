using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace DataAccess.Migrations
{
    /// <inheritdoc />
    public partial class AddSemesterToFinancials : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            // Step 1: Add columns as NULLABLE first
            migrationBuilder.AddColumn<int>(
                name: "SemesterId",
                table: "PaymentTransactions",
                type: "int",
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "SemesterId",
                table: "FundCollectionRequests",
                type: "int",
                nullable: true);

            // Step 2: Update existing data to use active semester (Fall2025 - Id = 5)
            // Get active semester or fallback to Id = 5
            migrationBuilder.Sql(@"
                DECLARE @ActiveSemesterId INT;
                SELECT @ActiveSemesterId = Id FROM Semesters WHERE IsActive = 1;
                
                IF @ActiveSemesterId IS NULL
                    SET @ActiveSemesterId = 5; -- Fall2025 as default
                
                -- Update FundCollectionRequests
                UPDATE FundCollectionRequests 
                SET SemesterId = @ActiveSemesterId 
                WHERE SemesterId IS NULL;
                
                -- Update PaymentTransactions based on TransactionDate
                -- Match with semester date range if possible
                UPDATE pt
                SET pt.SemesterId = s.Id
                FROM PaymentTransactions pt
                CROSS APPLY (
                    SELECT TOP 1 Id 
                    FROM Semesters s
                    WHERE pt.TransactionDate >= s.StartDate 
                    AND pt.TransactionDate <= s.EndDate
                    ORDER BY s.StartDate DESC
                ) s
                WHERE pt.SemesterId IS NULL;
                
                -- For transactions outside any semester, set to NULL (will stay NULL)
            ");

            // Step 3: Alter FundCollectionRequests.SemesterId to NOT NULL
            migrationBuilder.AlterColumn<int>(
                name: "SemesterId",
                table: "FundCollectionRequests",
                type: "int",
                nullable: false,
                oldNullable: true);

            // Step 4: Create indexes
            migrationBuilder.CreateIndex(
                name: "IX_PaymentTransactions_SemesterId_Type_Status",
                table: "PaymentTransactions",
                columns: new[] { "SemesterId", "Type", "Status" });

            migrationBuilder.CreateIndex(
                name: "IX_FundCollectionRequests_SemesterId_Status",
                table: "FundCollectionRequests",
                columns: new[] { "SemesterId", "Status" });

            // Step 5: Add foreign key constraints
            migrationBuilder.AddForeignKey(
                name: "FK_FundCollectionRequests_Semesters_SemesterId",
                table: "FundCollectionRequests",
                column: "SemesterId",
                principalTable: "Semesters",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "FK_PaymentTransactions_Semesters_SemesterId",
                table: "PaymentTransactions",
                column: "SemesterId",
                principalTable: "Semesters",
                principalColumn: "Id",
                onDelete: ReferentialAction.SetNull);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_FundCollectionRequests_Semesters_SemesterId",
                table: "FundCollectionRequests");

            migrationBuilder.DropForeignKey(
                name: "FK_PaymentTransactions_Semesters_SemesterId",
                table: "PaymentTransactions");

            migrationBuilder.DropIndex(
                name: "IX_PaymentTransactions_SemesterId_Type_Status",
                table: "PaymentTransactions");

            migrationBuilder.DropIndex(
                name: "IX_FundCollectionRequests_SemesterId_Status",
                table: "FundCollectionRequests");

            migrationBuilder.DropColumn(
                name: "SemesterId",
                table: "PaymentTransactions");

            migrationBuilder.DropColumn(
                name: "SemesterId",
                table: "FundCollectionRequests");
        }
    }
}
