using LogisticsApi.Data;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace LogisticsApi.Data.Migrations
{
    [Migration("20260811010000_MaterialTransportUpdatedAt")]
    [DbContext(typeof(AppDbContext))]
    /// <inheritdoc />
    public partial class MaterialTransportUpdatedAt : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            // The MaterialTransportRequest entity declares UpdatedAt, but the
            // original Phase 2 migration created the table without it. Every
            // INSERT therefore failed with SQL error 207 "Invalid column name",
            // which surfaced as "Failed to submit request" in the UI.
            migrationBuilder.AddColumn<DateTime>(
                name: "UpdatedAt",
                table: "MaterialTransportRequests",
                type: "datetime2",
                nullable: false,
                defaultValueSql: "GETUTCDATE()");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(name: "UpdatedAt", table: "MaterialTransportRequests");
        }
    }
}
