using LogisticsApi.Data;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace LogisticsApi.Data.Migrations
{
    [Migration("20260818000000_PinDecimalPrecision")]
    [DbContext(typeof(AppDbContext))]
    /// <inheritdoc />
    public partial class PinDecimalPrecision : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            // Two problems addressed here:
            //
            // 1. Cost, Quantity and the fuel-gauge columns had no explicit precision,
            //    so EF warned on every startup that values could be silently
            //    truncated. These feed reconciliation and audit reports.
            //
            // 2. The model and the database had drifted apart on the FuelLogs money
            //    columns — the model believed they were wider than they actually
            //    were, so a large value would pass validation and then fail at the
            //    database. Widened so the two agree.
            //
            // Every change widens capacity except the fuel gauge fields, which hold
            // percentages (0–100) and cannot overflow decimal(5,2).

            // ── Maintenance cost (NGN) ────────────────────────────────────────────
            migrationBuilder.AlterColumn<decimal>(
                name: "Cost", table: "MaintenanceRecords",
                type: "decimal(14,2)", nullable: true,
                oldClrType: typeof(decimal), oldType: "decimal(10,2)", oldNullable: true);

            // ── Fuel logs ─────────────────────────────────────────────────────────
            migrationBuilder.AlterColumn<decimal>(
                name: "LitresFilled", table: "FuelLogs",
                type: "decimal(10,3)", nullable: false,
                oldClrType: typeof(decimal), oldType: "decimal(8,2)");

            migrationBuilder.AlterColumn<decimal>(
                name: "TotalCost", table: "FuelLogs",
                type: "decimal(14,2)", nullable: false,
                oldClrType: typeof(decimal), oldType: "decimal(10,2)");

            // Gauge readings are percentages (0–100)
            migrationBuilder.AlterColumn<decimal>(
                name: "FuelGaugeBefore", table: "FuelLogs",
                type: "decimal(5,2)", nullable: true,
                oldClrType: typeof(decimal), oldType: "decimal(18,2)", oldNullable: true);

            migrationBuilder.AlterColumn<decimal>(
                name: "FuelGaugeAfter", table: "FuelLogs",
                type: "decimal(5,2)", nullable: true,
                oldClrType: typeof(decimal), oldType: "decimal(18,2)", oldNullable: true);

            // ── Quantities — three decimal places for metres, tonnes, litres ──────
            migrationBuilder.AlterColumn<decimal>(
                name: "Quantity", table: "MaterialTransportItems",
                type: "decimal(12,3)", nullable: false,
                oldClrType: typeof(decimal), oldType: "decimal(18,2)");

            migrationBuilder.AlterColumn<decimal>(
                name: "Quantity", table: "ProjectMaterialTrackings",
                type: "decimal(12,3)", nullable: true,
                oldClrType: typeof(decimal), oldType: "decimal(18,2)", oldNullable: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AlterColumn<decimal>(
                name: "Cost", table: "MaintenanceRecords",
                type: "decimal(10,2)", nullable: true,
                oldClrType: typeof(decimal), oldType: "decimal(14,2)", oldNullable: true);

            migrationBuilder.AlterColumn<decimal>(
                name: "LitresFilled", table: "FuelLogs",
                type: "decimal(8,2)", nullable: false,
                oldClrType: typeof(decimal), oldType: "decimal(10,3)");

            migrationBuilder.AlterColumn<decimal>(
                name: "TotalCost", table: "FuelLogs",
                type: "decimal(10,2)", nullable: false,
                oldClrType: typeof(decimal), oldType: "decimal(14,2)");

            migrationBuilder.AlterColumn<decimal>(
                name: "FuelGaugeBefore", table: "FuelLogs",
                type: "decimal(18,2)", nullable: true,
                oldClrType: typeof(decimal), oldType: "decimal(5,2)", oldNullable: true);

            migrationBuilder.AlterColumn<decimal>(
                name: "FuelGaugeAfter", table: "FuelLogs",
                type: "decimal(18,2)", nullable: true,
                oldClrType: typeof(decimal), oldType: "decimal(5,2)", oldNullable: true);

            migrationBuilder.AlterColumn<decimal>(
                name: "Quantity", table: "MaterialTransportItems",
                type: "decimal(18,2)", nullable: false,
                oldClrType: typeof(decimal), oldType: "decimal(12,3)");

            migrationBuilder.AlterColumn<decimal>(
                name: "Quantity", table: "ProjectMaterialTrackings",
                type: "decimal(18,2)", nullable: true,
                oldClrType: typeof(decimal), oldType: "decimal(12,3)");
        }
    }
}
