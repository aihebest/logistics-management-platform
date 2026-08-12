using LogisticsApi.Data;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace LogisticsApi.Data.Migrations
{
    [Migration("20260811020000_AllowMultipleDriversWithoutEmail")]
    [DbContext(typeof(AppDbContext))]
    /// <inheritdoc />
    public partial class AllowMultipleDriversWithoutEmail : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            // Drivers are registered by a coordinator or manager and typically have
            // no email address, so their record is stored with an empty email.
            // SQL Server's unique index treats every empty string as the same value,
            // so only ONE such driver could ever be saved — the second registration
            // failed on a duplicate key. Replace the plain unique index with a
            // filtered one that only constrains real email addresses.
            migrationBuilder.DropIndex(name: "IX_Users_Email", table: "Users");

            migrationBuilder.CreateIndex(
                name: "IX_Users_Email",
                table: "Users",
                column: "Email",
                unique: true,
                filter: "[Email] IS NOT NULL AND [Email] <> ''");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(name: "IX_Users_Email", table: "Users");

            migrationBuilder.CreateIndex(
                name: "IX_Users_Email",
                table: "Users",
                column: "Email",
                unique: true);
        }
    }
}
