using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace DataAccess.Migrations
{
    /// <inheritdoc />
    public partial class AddFundCollectionTables : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "FundCollectionRequests",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    ClubId = table.Column<int>(type: "int", nullable: false),
                    Title = table.Column<string>(type: "nvarchar(200)", maxLength: 200, nullable: false),
                    Description = table.Column<string>(type: "nvarchar(1000)", maxLength: 1000, nullable: true),
                    AmountPerMember = table.Column<decimal>(type: "decimal(18,2)", nullable: false),
                    DueDate = table.Column<DateTime>(type: "datetime2", nullable: false),
                    Status = table.Column<string>(type: "nvarchar(20)", maxLength: 20, nullable: false),
                    PaymentMethods = table.Column<string>(type: "nvarchar(200)", maxLength: 200, nullable: true),
                    Notes = table.Column<string>(type: "nvarchar(1000)", maxLength: 1000, nullable: true),
                    CreatedById = table.Column<int>(type: "int", nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "datetime2", nullable: false),
                    UpdatedAt = table.Column<DateTime>(type: "datetime2", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_FundCollectionRequests", x => x.Id);
                    table.ForeignKey(
                        name: "FK_FundCollectionRequests_Clubs_ClubId",
                        column: x => x.ClubId,
                        principalTable: "Clubs",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_FundCollectionRequests_Users_CreatedById",
                        column: x => x.CreatedById,
                        principalTable: "Users",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "FundCollectionPayments",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    FundCollectionRequestId = table.Column<int>(type: "int", nullable: false),
                    ClubMemberId = table.Column<int>(type: "int", nullable: false),
                    Amount = table.Column<decimal>(type: "decimal(18,2)", nullable: false),
                    Status = table.Column<string>(type: "nvarchar(20)", maxLength: 20, nullable: false),
                    PaidAt = table.Column<DateTime>(type: "datetime2", nullable: true),
                    PaymentMethod = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: true),
                    PaymentTransactionId = table.Column<int>(type: "int", nullable: true),
                    Notes = table.Column<string>(type: "nvarchar(500)", maxLength: 500, nullable: true),
                    ConfirmedById = table.Column<int>(type: "int", nullable: true),
                    ReminderCount = table.Column<int>(type: "int", nullable: false),
                    LastReminderAt = table.Column<DateTime>(type: "datetime2", nullable: true),
                    CreatedAt = table.Column<DateTime>(type: "datetime2", nullable: false),
                    UpdatedAt = table.Column<DateTime>(type: "datetime2", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_FundCollectionPayments", x => x.Id);
                    table.ForeignKey(
                        name: "FK_FundCollectionPayments_ClubMembers_ClubMemberId",
                        column: x => x.ClubMemberId,
                        principalTable: "ClubMembers",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_FundCollectionPayments_FundCollectionRequests_FundCollectionRequestId",
                        column: x => x.FundCollectionRequestId,
                        principalTable: "FundCollectionRequests",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_FundCollectionPayments_PaymentTransactions_PaymentTransactionId",
                        column: x => x.PaymentTransactionId,
                        principalTable: "PaymentTransactions",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_FundCollectionPayments_Users_ConfirmedById",
                        column: x => x.ConfirmedById,
                        principalTable: "Users",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateIndex(
                name: "IX_FundCollectionPayments_ClubMemberId",
                table: "FundCollectionPayments",
                column: "ClubMemberId");

            migrationBuilder.CreateIndex(
                name: "IX_FundCollectionPayments_ConfirmedById",
                table: "FundCollectionPayments",
                column: "ConfirmedById");

            migrationBuilder.CreateIndex(
                name: "IX_FundCollectionPayments_FundCollectionRequestId_ClubMemberId",
                table: "FundCollectionPayments",
                columns: new[] { "FundCollectionRequestId", "ClubMemberId" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_FundCollectionPayments_PaymentTransactionId",
                table: "FundCollectionPayments",
                column: "PaymentTransactionId");

            migrationBuilder.CreateIndex(
                name: "IX_FundCollectionPayments_Status",
                table: "FundCollectionPayments",
                column: "Status");

            migrationBuilder.CreateIndex(
                name: "IX_FundCollectionRequests_ClubId_Status",
                table: "FundCollectionRequests",
                columns: new[] { "ClubId", "Status" });

            migrationBuilder.CreateIndex(
                name: "IX_FundCollectionRequests_CreatedById",
                table: "FundCollectionRequests",
                column: "CreatedById");

            migrationBuilder.CreateIndex(
                name: "IX_FundCollectionRequests_DueDate",
                table: "FundCollectionRequests",
                column: "DueDate");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "FundCollectionPayments");

            migrationBuilder.DropTable(
                name: "FundCollectionRequests");
        }
    }
}
