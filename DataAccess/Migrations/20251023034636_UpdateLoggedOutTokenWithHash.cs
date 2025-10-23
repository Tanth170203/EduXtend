using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace DataAccess.Migrations
{
    /// <inheritdoc />
    public partial class UpdateLoggedOutTokenWithHash : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.RenameColumn(
                name: "Token",
                table: "LoggedOutTokens",
                newName: "TokenHash");

            migrationBuilder.RenameIndex(
                name: "IX_LoggedOutTokens_Token",
                table: "LoggedOutTokens",
                newName: "IX_LoggedOutTokens_TokenHash");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.RenameColumn(
                name: "TokenHash",
                table: "LoggedOutTokens",
                newName: "Token");

            migrationBuilder.RenameIndex(
                name: "IX_LoggedOutTokens_TokenHash",
                table: "LoggedOutTokens",
                newName: "IX_LoggedOutTokens_Token");
        }
    }
}
