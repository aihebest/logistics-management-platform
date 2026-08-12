using LogisticsApi.Data;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace LogisticsApi.Data.Migrations
{
    [Migration("20260811030000_ProjectMaterialIsoAuditFields")]
    [DbContext(typeof(AppDbContext))]
    /// <inheritdoc />
    public partial class ProjectMaterialIsoAuditFields : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            // Columns requested by the auditor during the ISO audit so the
            // delivery-date chain and shipping documents are fully traceable.

            // Delivery date chain
            migrationBuilder.AddColumn<DateOnly>(
                name: "ExpectedDeliveryDateProjectTeam", table: "ProjectMaterialTrackings",
                type: "date", nullable: true);

            migrationBuilder.AddColumn<DateOnly>(
                name: "StoreNotificationDate", table: "ProjectMaterialTrackings",
                type: "date", nullable: true);

            migrationBuilder.AddColumn<DateOnly>(
                name: "ExpectedDeliveryDateStoreTeam", table: "ProjectMaterialTrackings",
                type: "date", nullable: true);

            migrationBuilder.AddColumn<DateOnly>(
                name: "ExpectedDeliveryDateAgreed", table: "ProjectMaterialTrackings",
                type: "date", nullable: true);

            // Shipping documents — BL and AWB recorded separately
            migrationBuilder.AddColumn<string>(
                name: "PaarNumber", table: "ProjectMaterialTrackings",
                type: "nvarchar(100)", maxLength: 100, nullable: true);

            migrationBuilder.AddColumn<DateOnly>(
                name: "PaarDate", table: "ProjectMaterialTrackings",
                type: "date", nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "BlNumber", table: "ProjectMaterialTrackings",
                type: "nvarchar(100)", maxLength: 100, nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "AwbNumber", table: "ProjectMaterialTrackings",
                type: "nvarchar(100)", maxLength: 100, nullable: true);

            // Carry any existing combined BL/AWB value into the matching new
            // column based on the recorded mode of transport.
            migrationBuilder.Sql(@"
                UPDATE ProjectMaterialTrackings
                SET BlNumber = BlAwbNumber
                WHERE BlAwbNumber IS NOT NULL
                  AND (ModeOfTransport IS NULL OR ModeOfTransport <> 'Air');

                UPDATE ProjectMaterialTrackings
                SET AwbNumber = BlAwbNumber
                WHERE BlAwbNumber IS NOT NULL
                  AND ModeOfTransport = 'Air';
            ");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            foreach (var col in new[]
            {
                "ExpectedDeliveryDateProjectTeam", "StoreNotificationDate",
                "ExpectedDeliveryDateStoreTeam", "ExpectedDeliveryDateAgreed",
                "PaarNumber", "PaarDate", "BlNumber", "AwbNumber"
            })
            {
                migrationBuilder.DropColumn(name: col, table: "ProjectMaterialTrackings");
            }
        }
    }
}
