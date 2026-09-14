using LogisticsApi.Data;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace LogisticsApi.Data.Migrations
{
    [Migration("20260914020000_FuelMileageAndGaugePosition")]
    [DbContext(typeof(AppDbContext))]
    /// <inheritdoc />
    public partial class FuelMileageAndGaugePosition : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            // ── One "after" reading instead of a From/To pair ─────────────────
            // The form had three odometer boxes doing overlapping jobs. It now
            // asks for the reading before the purchase and the reading after,
            // and calculates the distance between them.
            migrationBuilder.AddColumn<int>(
                name: "OdometerAfterFill", table: "FuelLogs",
                type: "int", nullable: true);

            // ── Gauge as a tank position, not a percentage ────────────────────
            migrationBuilder.AddColumn<string>(
                name: "FuelGaugeBeforePosition", table: "FuelLogs",
                type: "nvarchar(30)", maxLength: 30, nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "FuelGaugeAfterPosition", table: "FuelLogs",
                type: "nvarchar(30)", maxLength: 30, nullable: true);

            // Carry the existing readings across so the 64 UAT entries keep their
            // meaning. OdometerTo was "current reading", which is what the new
            // after-purchase column records.
            migrationBuilder.Sql(@"
                UPDATE FuelLogs
                SET OdometerAfterFill = OdometerTo
                WHERE OdometerTo IS NOT NULL;
            ");

            // Recalculate distance against the new pair, but only where the
            // numbers make sense — a lower 'after' reading means the entry was
            // mistyped, and inventing a negative distance would hide that.
            migrationBuilder.Sql(@"
                UPDATE FuelLogs
                SET MileageCovered = OdometerAfterFill - OdometerAtFill
                WHERE OdometerAfterFill IS NOT NULL
                  AND OdometerAfterFill >= OdometerAtFill;
            ");

            // Band the old percentages onto the nearest position on the dial.
            migrationBuilder.Sql(@"
                UPDATE FuelLogs
                SET FuelGaugeBeforePosition =
                    CASE
                        WHEN FuelGaugeBefore IS NULL     THEN NULL
                        WHEN FuelGaugeBefore <= 5        THEN N'Reserve'
                        WHEN FuelGaugeBefore <  25       THEN N'Below 1/4 tank'
                        WHEN FuelGaugeBefore =  25       THEN N'1/4 tank'
                        WHEN FuelGaugeBefore <  50       THEN N'Below 1/2 tank'
                        WHEN FuelGaugeBefore =  50       THEN N'1/2 tank'
                        WHEN FuelGaugeBefore <  75       THEN N'Above 1/2 tank'
                        WHEN FuelGaugeBefore =  75       THEN N'3/4 tank'
                        WHEN FuelGaugeBefore < 100       THEN N'Above 3/4 tank'
                        ELSE N'Full tank'
                    END,
                    FuelGaugeAfterPosition =
                    CASE
                        WHEN FuelGaugeAfter IS NULL      THEN NULL
                        WHEN FuelGaugeAfter <= 5         THEN N'Reserve'
                        WHEN FuelGaugeAfter <  25        THEN N'Below 1/4 tank'
                        WHEN FuelGaugeAfter =  25        THEN N'1/4 tank'
                        WHEN FuelGaugeAfter <  50        THEN N'Below 1/2 tank'
                        WHEN FuelGaugeAfter =  50        THEN N'1/2 tank'
                        WHEN FuelGaugeAfter <  75        THEN N'Above 1/2 tank'
                        WHEN FuelGaugeAfter =  75        THEN N'3/4 tank'
                        WHEN FuelGaugeAfter < 100        THEN N'Above 3/4 tank'
                        ELSE N'Full tank'
                    END;
            ");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(name: "OdometerAfterFill",       table: "FuelLogs");
            migrationBuilder.DropColumn(name: "FuelGaugeBeforePosition", table: "FuelLogs");
            migrationBuilder.DropColumn(name: "FuelGaugeAfterPosition",  table: "FuelLogs");
        }
    }
}
