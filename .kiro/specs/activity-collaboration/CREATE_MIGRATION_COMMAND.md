# Create Migration for Collaboration Invitation

## Command to run

```bash
cd DataAccess
dotnet ef migrations add AddCollaborationInvitationFields --context EduXtendDbContext
```

## Expected Migration Content

The migration should add these columns to Activities table:

1. **CollaborationStatus** (nvarchar(50), nullable)
   - Values: "Pending", "Accepted", "Rejected"
   
2. **CollaborationRejectionReason** (nvarchar(500), nullable)
   - Reason why collaboration was rejected
   
3. **CollaborationRespondedAt** (datetime2, nullable)
   - Timestamp when invitation was responded to
   
4. **CollaborationRespondedBy** (int, nullable)
   - Foreign key to Users table
   - User who responded to the invitation

## Manual Migration File

If you prefer to create manually, create this file:

**File:** `DataAccess/Migrations/YYYYMMDDHHMMSS_AddCollaborationInvitationFields.cs`

```csharp
using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace DataAccess.Migrations
{
    /// <inheritdoc />
    public partial class AddCollaborationInvitationFields : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "CollaborationStatus",
                table: "Activities",
                type: "nvarchar(50)",
                maxLength: 50,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "CollaborationRejectionReason",
                table: "Activities",
                type: "nvarchar(500)",
                maxLength: 500,
                nullable: true);

            migrationBuilder.AddColumn<DateTime>(
                name: "CollaborationRespondedAt",
                table: "Activities",
                type: "datetime2",
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "CollaborationRespondedBy",
                table: "Activities",
                type: "int",
                nullable: true);

            migrationBuilder.CreateIndex(
                name: "IX_Activities_CollaborationRespondedBy",
                table: "Activities",
                column: "CollaborationRespondedBy");

            migrationBuilder.AddForeignKey(
                name: "FK_Activities_Users_CollaborationRespondedBy",
                table: "Activities",
                column: "CollaborationRespondedBy",
                principalTable: "Users",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_Activities_Users_CollaborationRespondedBy",
                table: "Activities");

            migrationBuilder.DropIndex(
                name: "IX_Activities_CollaborationRespondedBy",
                table: "Activities");

            migrationBuilder.DropColumn(
                name: "CollaborationStatus",
                table: "Activities");

            migrationBuilder.DropColumn(
                name: "CollaborationRejectionReason",
                table: "Activities");

            migrationBuilder.DropColumn(
                name: "CollaborationRespondedAt",
                table: "Activities");

            migrationBuilder.DropColumn(
                name: "CollaborationRespondedBy",
                table: "Activities");
        }
    }
}
```

## After Creating Migration

1. Update Activity model in `BusinessObject/Models/Activity.cs`
2. Update DbContext if needed
3. Run migration: `dotnet ef database update`
4. Verify columns added to database
