using LogisticsApi.Data;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace LogisticsApi.Data.Migrations
{
    [Migration("20260619000000_VehicleMileageFields")]
    [DbContext(typeof(AppDbContext))]
    /// <inheritdoc />
    public partial class VehicleMileageFields : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            // Mileage at purchase (odometer when company bought the vehicle)
            migrationBuilder.AddColumn<int>(
                name: "MileageAtPurchase",
                table: "Vehicles",
                type: "int",
                nullable: true);

            // Previous mileage at purchase (for second-hand vehicles — prior odometer)
            migrationBuilder.AddColumn<int>(
                name: "PreviousMileageAtPurchase",
                table: "Vehicles",
                type: "int",
                nullable: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(name: "MileageAtPurchase",         table: "Vehicles");
            migrationBuilder.DropColumn(name: "PreviousMileageAtPurchase", table: "Vehicles");
        }
    }
}
