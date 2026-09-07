using LogisticsApi.Data;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace LogisticsApi.Data.Migrations
{
    [Migration("20260825000000_TripPersonnelAndFuelPaymentMethod")]
    [DbContext(typeof(AppDbContext))]
    /// <inheritdoc />
    public partial class TripPersonnelAndFuelPaymentMethod : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            // ── Trip request: who is travelling and what goes with them ───────────
            // Requested by the HOD and Director of Logistics so a request records
            // the full picture of a movement rather than just its route.
            migrationBuilder.AddColumn<int>(
                name: "PersonnelCount", table: "TripRequests",
                type: "int", nullable: false, defaultValue: 1);

            migrationBuilder.AddColumn<string>(
                name: "PersonnelNames", table: "TripRequests",
                type: "nvarchar(1000)", maxLength: 1000, nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "PersonnelCategory", table: "TripRequests",
                type: "nvarchar(50)", maxLength: 50, nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "MovementDuration", table: "TripRequests",
                type: "nvarchar(50)", maxLength: 50, nullable: true);

            migrationBuilder.AddColumn<bool>(
                name: "IsDropOff", table: "TripRequests",
                type: "bit", nullable: false, defaultValue: false);

            migrationBuilder.AddColumn<bool>(
                name: "HasMaterials", table: "TripRequests",
                type: "bit", nullable: false, defaultValue: false);

            migrationBuilder.AddColumn<string>(
                name: "MaterialDescription", table: "TripRequests",
                type: "nvarchar(500)", maxLength: 500, nullable: true);

            // ── Fuel: four payment methods instead of a cash yes/no ───────────────
            migrationBuilder.AddColumn<string>(
                name: "PaymentMethod", table: "FuelLogs",
                type: "nvarchar(20)", maxLength: 20, nullable: false, defaultValue: "Card");

            // Carry the existing boolean across so historic records keep their meaning.
            migrationBuilder.Sql(@"
                UPDATE FuelLogs
                SET PaymentMethod = CASE WHEN IsCashPayment = 1 THEN 'Cash' ELSE 'Card' END;
            ");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            foreach (var col in new[]
            {
                "PersonnelCount", "PersonnelNames", "PersonnelCategory",
                "MovementDuration", "IsDropOff", "HasMaterials", "MaterialDescription"
            })
            {
                migrationBuilder.DropColumn(name: col, table: "TripRequests");
            }

            migrationBuilder.DropColumn(name: "PaymentMethod", table: "FuelLogs");
        }
    }
}
