using LogisticsApi.Data;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace LogisticsApi.Data.Migrations
{
    [Migration("20260909000000_MaintenanceDateReturned")]
    [DbContext(typeof(AppDbContext))]
    /// <inheritdoc />
    public partial class MaintenanceDateReturned : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            // Requested by the HOD Logistics: record when the vehicle came back
            // from the workshop, separately from when the work was completed.
            migrationBuilder.AddColumn<DateOnly>(
                name: "DateReturned", table: "MaintenanceRecords",
                type: "date", nullable: true);

            // Vendor Contact is no longer captured — the vendor name is enough,
            // and the contact number was going stale. Existing values are dropped.
            migrationBuilder.DropColumn(
                name: "VendorContact", table: "MaintenanceRecords");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "DateReturned", table: "MaintenanceRecords");

            migrationBuilder.AddColumn<string>(
                name: "VendorContact", table: "MaintenanceRecords",
                type: "nvarchar(50)", maxLength: 50, nullable: true);
        }
    }
}
