using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace DataAccess.Migrations
{
    /// <inheritdoc />
    public partial class SyncGpsAttendanceFields : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            // GPS fields already added via SQL script (ADD_GPS_ATTENDANCE_FIELDS.sql)
            // This migration is just to sync the model snapshot
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            // No rollback needed - fields were added via SQL script
        }
    }
}
