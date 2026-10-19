using LogisticsApi.Data;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace LogisticsApi.Data.Migrations
{
    [Migration("20261020000000_TripCcEmails")]
    [DbContext(typeof(AppDbContext))]
    /// <inheritdoc />
    public partial class TripCcEmails : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            // Optional extra recipients on a trip request — requested by the
            // Transport Manager so site contacts and PAs get the updates too.
            migrationBuilder.AddColumn<string>(
                name: "CcEmails", table: "TripRequests",
                type: "nvarchar(600)", maxLength: 600, nullable: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(name: "CcEmails", table: "TripRequests");
        }
    }
}
