using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace DataAccess.Migrations
{
    /// <inheritdoc />
    public partial class AddVnpayAndAttendanceCode : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AlterColumn<string>(
                name: "Content",
                table: "SystemNews",
                type: "nvarchar(max)",
                nullable: true,
                oldClrType: typeof(string),
                oldType: "nvarchar(1000)",
                oldMaxLength: 1000,
                oldNullable: true);

            migrationBuilder.AddColumn<int>(
                name: "VnpayTransactionDetailId",
                table: "FundCollectionPayments",
                type: "int",
                nullable: true);

            // AttendanceCode already exists in database, skip adding it
            // migrationBuilder.AddColumn<string>(
            //     name: "AttendanceCode",
            //     table: "Activities",
            //     type: "nvarchar(6)",
            //     maxLength: 6,
            //     nullable: true);

            migrationBuilder.CreateTable(
                name: "VnpayTransactionDetails",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    FundCollectionPaymentId = table.Column<int>(type: "int", nullable: false),
                    VnpayTransactionId = table.Column<long>(type: "bigint", nullable: false),
                    BankCode = table.Column<string>(type: "nvarchar(20)", maxLength: 20, nullable: true),
                    BankTransactionId = table.Column<string>(type: "nvarchar(255)", maxLength: 255, nullable: true),
                    ResponseCode = table.Column<string>(type: "nvarchar(10)", maxLength: 10, nullable: false),
                    OrderInfo = table.Column<string>(type: "nvarchar(500)", maxLength: 500, nullable: true),
                    Amount = table.Column<decimal>(type: "decimal(18,2)", nullable: false),
                    TransactionDate = table.Column<DateTime>(type: "datetime2", nullable: true),
                    TransactionStatus = table.Column<string>(type: "nvarchar(20)", maxLength: 20, nullable: false),
                    SecureHash = table.Column<string>(type: "nvarchar(500)", maxLength: 500, nullable: true),
                    IpAddress = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: true),
                    CreatedAt = table.Column<DateTime>(type: "datetime2", nullable: false),
                    UpdatedAt = table.Column<DateTime>(type: "datetime2", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_VnpayTransactionDetails", x => x.Id);
                    table.ForeignKey(
                        name: "FK_VnpayTransactionDetails_FundCollectionPayments_FundCollectionPaymentId",
                        column: x => x.FundCollectionPaymentId,
                        principalTable: "FundCollectionPayments",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_VnpayTransactionDetails_FundCollectionPaymentId",
                table: "VnpayTransactionDetails",
                column: "FundCollectionPaymentId",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_VnpayTransactionDetails_TransactionStatus",
                table: "VnpayTransactionDetails",
                column: "TransactionStatus");

            migrationBuilder.CreateIndex(
                name: "IX_VnpayTransactionDetails_VnpayTransactionId",
                table: "VnpayTransactionDetails",
                column: "VnpayTransactionId",
                unique: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "VnpayTransactionDetails");

            migrationBuilder.DropColumn(
                name: "VnpayTransactionDetailId",
                table: "FundCollectionPayments");

            // Don't drop AttendanceCode as it was already there
            // migrationBuilder.DropColumn(
            //     name: "AttendanceCode",
            //     table: "Activities");

            migrationBuilder.AlterColumn<string>(
                name: "Content",
                table: "SystemNews",
                type: "nvarchar(1000)",
                maxLength: 1000,
                nullable: true,
                oldClrType: typeof(string),
                oldType: "nvarchar(max)",
                oldNullable: true);
        }
    }
}
