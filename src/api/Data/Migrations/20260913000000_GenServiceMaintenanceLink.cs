using LogisticsApi.Data;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace LogisticsApi.Data.Migrations
{
    [Migration("20260913000000_GenServiceMaintenanceLink")]
    [DbContext(typeof(AppDbContext))]
    /// <inheritdoc />
    public partial class GenServiceMaintenanceLink : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            // Vehicles are ours; the repair happens on the General Service
            // platform. These columns cross-reference the two records and hold
            // the last update GenService pushed to us, so a coordinator can see
            // where a vehicle is without ringing the workshop.

            migrationBuilder.AddColumn<Guid>(
                name: "GenServiceRequestId", table: "MaintenanceRecords",
                type: "uniqueidentifier", nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "GenServiceRequestNumber", table: "MaintenanceRecords",
                type: "nvarchar(40)", maxLength: 40, nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "GenServiceStatus", table: "MaintenanceRecords",
                type: "nvarchar(40)", maxLength: 40, nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "GenServiceFaultIdentified", table: "MaintenanceRecords",
                type: "nvarchar(2000)", maxLength: 2000, nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "GenServiceWorkDone", table: "MaintenanceRecords",
                type: "nvarchar(2000)", maxLength: 2000, nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "GenServiceWorkshopName", table: "MaintenanceRecords",
                type: "nvarchar(200)", maxLength: 200, nullable: true);

            migrationBuilder.AddColumn<DateTime>(
                name: "GenServiceSyncedAt", table: "MaintenanceRecords",
                type: "datetime2", nullable: true);

            // NOT NULL with a default so existing rows are correctly attributed
            // to this platform rather than left ambiguous.
            migrationBuilder.AddColumn<string>(
                name: "SourceSystem", table: "MaintenanceRecords",
                type: "nvarchar(20)", maxLength: 20, nullable: false,
                defaultValue: "Logistics");

            // Looking a record up by the GenService reference is the hot path for
            // every inbound status push.
            migrationBuilder.CreateIndex(
                name: "IX_MaintenanceRecords_GenServiceRequestId",
                table: "MaintenanceRecords",
                column: "GenServiceRequestId");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_MaintenanceRecords_GenServiceRequestId",
                table: "MaintenanceRecords");

            migrationBuilder.DropColumn(name: "GenServiceRequestId",       table: "MaintenanceRecords");
            migrationBuilder.DropColumn(name: "GenServiceRequestNumber",   table: "MaintenanceRecords");
            migrationBuilder.DropColumn(name: "GenServiceStatus",          table: "MaintenanceRecords");
            migrationBuilder.DropColumn(name: "GenServiceFaultIdentified", table: "MaintenanceRecords");
            migrationBuilder.DropColumn(name: "GenServiceWorkDone",        table: "MaintenanceRecords");
            migrationBuilder.DropColumn(name: "GenServiceWorkshopName",    table: "MaintenanceRecords");
            migrationBuilder.DropColumn(name: "GenServiceSyncedAt",        table: "MaintenanceRecords");
            migrationBuilder.DropColumn(name: "SourceSystem",              table: "MaintenanceRecords");
        }
    }
}
