using LogisticsApi.Data;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace LogisticsApi.Data.Migrations
{
    [Migration("20260914000000_MaintenanceReminderDedup")]
    [DbContext(typeof(AppDbContext))]
    /// <inheritdoc />
    public partial class MaintenanceReminderDedup : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            // The reminder job had no record of what it had already sent, so a
            // scheduling fault made it re-mail the same vehicle every couple of
            // minutes. Persisting the last send makes the job idempotent even
            // across restarts and multiple instances.
            migrationBuilder.AddColumn<DateTime>(
                name: "LastReminderSentAt", table: "MaintenanceRecords",
                type: "datetime2", nullable: true);

            migrationBuilder.AddColumn<DateTime>(
                name: "LastOverdueNoticeAt", table: "MaintenanceRecords",
                type: "datetime2", nullable: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(name: "LastReminderSentAt",  table: "MaintenanceRecords");
            migrationBuilder.DropColumn(name: "LastOverdueNoticeAt", table: "MaintenanceRecords");
        }
    }
}
