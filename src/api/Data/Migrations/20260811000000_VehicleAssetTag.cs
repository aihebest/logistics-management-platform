using LogisticsApi.Data;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace LogisticsApi.Data.Migrations
{
    [Migration("20260811000000_VehicleAssetTag")]
    [DbContext(typeof(AppDbContext))]
    /// <inheritdoc />
    public partial class VehicleAssetTag : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            // Fixed-asset tag from the company asset register, so platform
            // vehicles can be reconciled with the Repairs & Maintenance Register.
            migrationBuilder.AddColumn<string>(
                name: "AssetTagNo",
                table: "Vehicles",
                type: "nvarchar(40)",
                maxLength: 40,
                nullable: true);

            migrationBuilder.CreateIndex(
                name: "IX_Vehicles_AssetTagNo",
                table: "Vehicles",
                column: "AssetTagNo");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(name: "IX_Vehicles_AssetTagNo", table: "Vehicles");
            migrationBuilder.DropColumn(name: "AssetTagNo", table: "Vehicles");
        }
    }
}
