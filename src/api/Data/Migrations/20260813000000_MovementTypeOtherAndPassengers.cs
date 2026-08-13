using LogisticsApi.Data;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace LogisticsApi.Data.Migrations
{
    [Migration("20260813000000_MovementTypeOtherAndPassengers")]
    [DbContext(typeof(AppDbContext))]
    /// <inheritdoc />
    public partial class MovementTypeOtherAndPassengers : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            // Free-text detail for movements logged as "Other" — the fixed list
            // doesn't cover every case the gate records.
            migrationBuilder.AddColumn<string>(
                name: "MovementTypeOther",
                table: "MovementRegisters",
                type: "nvarchar(100)",
                maxLength: 100,
                nullable: true);

            // Names of people carried, needed for staff movements and pick-ups.
            migrationBuilder.AddColumn<string>(
                name: "Passengers",
                table: "MovementRegisters",
                type: "nvarchar(500)",
                maxLength: 500,
                nullable: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(name: "MovementTypeOther", table: "MovementRegisters");
            migrationBuilder.DropColumn(name: "Passengers",        table: "MovementRegisters");
        }
    }
}
