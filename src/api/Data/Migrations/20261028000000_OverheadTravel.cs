using LogisticsApi.Data;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace LogisticsApi.Data.Migrations
{
    [Migration("20261028000000_OverheadTravel")]
    [DbContext(typeof(AppDbContext))]
    /// <inheritdoc />
    public partial class OverheadTravel : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            // Requested by Cost Control and Logistics: travel for people outside
            // the company is charged to Overhead and approved by the DMD.
            migrationBuilder.AddColumn<bool>(
                name: "IsOverhead", table: "Departments",
                type: "bit", nullable: false, defaultValue: false);

            migrationBuilder.AddColumn<string>(
                name: "TravellerOrganisation", table: "TravelRequests",
                type: "nvarchar(200)", maxLength: 200, nullable: true);

            // The DMD is already recorded as the deputy of Executive Management,
            // so he becomes the Overhead approver without anyone having to look
            // up his account. Can be changed later on the Departments screen.
            migrationBuilder.Sql(@"
                DECLARE @dmd uniqueidentifier =
                    (SELECT TOP 1 DeputyHodUserId FROM Departments WHERE IsExecutive = 1);

                IF NOT EXISTS (SELECT 1 FROM Departments WHERE Name = N'Overhead')
                    INSERT INTO Departments (Name, IsOverhead, HodUserId) VALUES (N'Overhead', 1, @dmd);
                ELSE
                    UPDATE Departments
                       SET IsOverhead = 1, IsActive = 1, HodUserId = COALESCE(HodUserId, @dmd)
                     WHERE Name = N'Overhead';
            ");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(name: "TravellerOrganisation", table: "TravelRequests");
            migrationBuilder.DropColumn(name: "IsOverhead", table: "Departments");
        }
    }
}
