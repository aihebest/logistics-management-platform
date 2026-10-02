using LogisticsApi.Data;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace LogisticsApi.Data.Migrations
{
    [Migration("20261002000000_Projects")]
    [DbContext(typeof(AppDbContext))]
    /// <inheritdoc />
    public partial class Projects : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            // Material transport went to every head of department. It is project
            // work, and each project has its own PM, so requests now route to
            // the PM of the project named on the form.
            migrationBuilder.CreateTable(
                name: "Projects",
                columns: t => new
                {
                    Id = t.Column<Guid>(nullable: false, defaultValueSql: "NEWSEQUENTIALID()"),
                    Name = t.Column<string>(maxLength: 100, nullable: false),
                    ManagerUserId = t.Column<Guid>(nullable: true),
                    IsActive = t.Column<bool>(nullable: false, defaultValue: true),
                    CreatedAt = t.Column<DateTime>(nullable: false, defaultValueSql: "GETUTCDATE()")
                },
                constraints: t =>
                {
                    t.PrimaryKey("PK_Projects", x => x.Id);
                    t.ForeignKey("FK_Project_Manager", x => x.ManagerUserId, "Users", "Id",
                        onDelete: ReferentialAction.NoAction);
                });

            migrationBuilder.CreateIndex(
                name: "IX_Projects_Name", table: "Projects", column: "Name", unique: true);

            // The four projects and their PMs, as supplied. PMs are matched by
            // name; any that don't match can be set on the Departments screen.
            migrationBuilder.Sql(@"
                INSERT INTO Projects (Name, ManagerUserId) VALUES
                  (N'PMC',     (SELECT TOP 1 Id FROM Users WHERE FullName = N'Wilson Obarueroro' AND IsActive = 1)),
                  (N'Cluster', (SELECT TOP 1 Id FROM Users WHERE FullName = N'Paul Okonkwo'      AND IsActive = 1)),
                  (N'GMC',     (SELECT TOP 1 Id FROM Users WHERE FullName = N'Etimbuk Umoh'      AND IsActive = 1)),
                  (N'GTS',     (SELECT TOP 1 Id FROM Users WHERE FullName = N'Konboye Ekiotenne' AND IsActive = 1));
            ");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable("Projects");
        }
    }
}
