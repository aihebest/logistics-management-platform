using LogisticsApi.Data;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace LogisticsApi.Data.Migrations
{
    [Migration("20260928000000_UserAppRoles")]
    [DbContext(typeof(AppDbContext))]
    /// <inheritdoc />
    public partial class UserAppRoles : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            // Every Entra app role a person holds, so someone with two roles —
            // the head of Logistics is both Manager and Management — receives
            // the notifications for both. Filled in on each person's next sign-in.
            migrationBuilder.AddColumn<string>(
                name: "AppRoles", table: "Users",
                type: "nvarchar(200)", maxLength: 200, nullable: true);

            // Seed from the existing single role so nobody drops off a mailing
            // list in the gap before their next sign-in.
            migrationBuilder.Sql(@"
                UPDATE Users
                SET AppRoles = ',' + Role + ','
                WHERE Role IS NOT NULL AND Role <> '' AND Role <> 'Staff';
            ");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(name: "AppRoles", table: "Users");
        }
    }
}
