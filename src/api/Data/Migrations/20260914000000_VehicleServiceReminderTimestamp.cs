using LogisticsApi.Data;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace LogisticsApi.Data.Migrations
{
    [Migration("20260914000000_VehicleServiceReminderTimestamp")]
    [DbContext(typeof(AppDbContext))]
    /// <inheritdoc />
    public partial class VehicleServiceReminderTimestamp : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            // Service-due reminders now key off Vehicles.NextServiceDate rather
            // than a maintenance record's date, so the "already told them today"
            // guard needs to live on the vehicle.
            migrationBuilder.AddColumn<DateTime>(
                name: "LastServiceReminderAt", table: "Vehicles",
                type: "datetime2", nullable: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "LastServiceReminderAt", table: "Vehicles");
        }
    }
}
