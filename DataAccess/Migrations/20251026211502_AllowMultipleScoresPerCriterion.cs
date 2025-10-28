using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace DataAccess.Migrations
{
    /// <inheritdoc />
    public partial class AllowMultipleScoresPerCriterion : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            // Drop UserRoles table if exists (might be already dropped)
            migrationBuilder.Sql(@"
                IF OBJECT_ID('UserRoles', 'U') IS NOT NULL
                BEGIN
                    DROP TABLE UserRoles;
                END
            ");

            // Drop unique index if exists
            migrationBuilder.Sql(@"
                IF EXISTS (
                    SELECT 1 
                    FROM sys.indexes 
                    WHERE name = 'IX_MovementRecordDetails_MovementRecordId_CriterionId' 
                    AND object_id = OBJECT_ID('MovementRecordDetails')
                )
                BEGIN
                    DROP INDEX IX_MovementRecordDetails_MovementRecordId_CriterionId ON MovementRecordDetails;
                END
            ");

            // Add RoleId column if not exists
            migrationBuilder.Sql(@"
                IF NOT EXISTS (SELECT 1 FROM sys.columns WHERE object_id = OBJECT_ID('Users') AND name = 'RoleId')
                BEGIN
                    ALTER TABLE Users ADD RoleId int NOT NULL DEFAULT 0;
                END
            ");

            // Alter TokenHash column if needed
            migrationBuilder.Sql(@"
                IF EXISTS (
                    SELECT 1 FROM sys.columns 
                    WHERE object_id = OBJECT_ID('LoggedOutTokens') 
                    AND name = 'TokenHash' 
                    AND max_length = 4000
                )
                BEGIN
                    ALTER TABLE LoggedOutTokens ALTER COLUMN TokenHash nvarchar(64) NOT NULL;
                END
            ");

            // Create FK index if not exists
            migrationBuilder.Sql(@"
                IF NOT EXISTS (
                    SELECT 1 FROM sys.indexes 
                    WHERE name = 'IX_Users_RoleId' 
                    AND object_id = OBJECT_ID('Users')
                )
                BEGIN
                    CREATE INDEX IX_Users_RoleId ON Users(RoleId);
                END
            ");

            // Create non-unique index for performance
            migrationBuilder.Sql(@"
                IF NOT EXISTS (
                    SELECT 1 FROM sys.indexes 
                    WHERE name = 'IX_MovementRecordDetails_MovementRecordId_CriterionId' 
                    AND object_id = OBJECT_ID('MovementRecordDetails')
                )
                BEGIN
                    CREATE INDEX IX_MovementRecordDetails_MovementRecordId_CriterionId 
                    ON MovementRecordDetails(MovementRecordId, CriterionId);
                END
            ");

            // Create FK if not exists
            migrationBuilder.Sql(@"
                IF NOT EXISTS (
                    SELECT 1 FROM sys.foreign_keys 
                    WHERE name = 'FK_Users_Roles_RoleId'
                    AND parent_object_id = OBJECT_ID('Users')
                )
                BEGIN
                    ALTER TABLE Users
                    ADD CONSTRAINT FK_Users_Roles_RoleId
                    FOREIGN KEY (RoleId) REFERENCES Roles(Id);
                END
            ");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_Users_Roles_RoleId",
                table: "Users");

            migrationBuilder.DropIndex(
                name: "IX_Users_RoleId",
                table: "Users");

            migrationBuilder.DropIndex(
                name: "IX_MovementRecordDetails_MovementRecordId_CriterionId",
                table: "MovementRecordDetails");

            migrationBuilder.DropColumn(
                name: "RoleId",
                table: "Users");

            migrationBuilder.AlterColumn<string>(
                name: "TokenHash",
                table: "LoggedOutTokens",
                type: "nvarchar(2000)",
                maxLength: 2000,
                nullable: false,
                oldClrType: typeof(string),
                oldType: "nvarchar(64)",
                oldMaxLength: 64);

            migrationBuilder.CreateTable(
                name: "UserRoles",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    RoleId = table.Column<int>(type: "int", nullable: false),
                    UserId = table.Column<int>(type: "int", nullable: false),
                    AssignedAt = table.Column<DateTime>(type: "datetime2", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_UserRoles", x => x.Id);
                    table.ForeignKey(
                        name: "FK_UserRoles_Roles_RoleId",
                        column: x => x.RoleId,
                        principalTable: "Roles",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_UserRoles_Users_UserId",
                        column: x => x.UserId,
                        principalTable: "Users",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_MovementRecordDetails_MovementRecordId_CriterionId",
                table: "MovementRecordDetails",
                columns: new[] { "MovementRecordId", "CriterionId" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_UserRoles_RoleId",
                table: "UserRoles",
                column: "RoleId");

            migrationBuilder.CreateIndex(
                name: "IX_UserRoles_UserId_RoleId",
                table: "UserRoles",
                columns: new[] { "UserId", "RoleId" },
                unique: true);
        }
    }
}
