using LogisticsApi.Data;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace LogisticsApi.Data.Migrations
{
    [Migration("20260929000000_ExecutiveDepartment")]
    [DbContext(typeof(AppDbContext))]
    /// <inheritdoc />
    public partial class ExecutiveDepartment : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            // Directors' travel is captured on the TRF too, at the DMD's request.
            // It can't follow the staff route — a department head would end up
            // verifying the DMD's travel — so it gets its own department: the MD
            // approves directors' travel, and the DMD (deputy) approves the MD's.
            migrationBuilder.AddColumn<bool>(
                name: "IsExecutive", table: "Departments",
                type: "bit", nullable: false, defaultValue: false);

            migrationBuilder.AddColumn<Guid>(
                name: "DeputyHodUserId", table: "Departments",
                type: "uniqueidentifier", nullable: true);

            migrationBuilder.AddForeignKey(
                name: "FK_Dept_DeputyHod",
                table: "Departments", column: "DeputyHodUserId",
                principalTable: "Users", principalColumn: "Id",
                onDelete: ReferentialAction.NoAction);

            // MD and DMD are assigned from the Departments screen once both have
            // platform accounts — the same approach as every other headship.
            migrationBuilder.Sql(@"
                IF NOT EXISTS (SELECT 1 FROM Departments WHERE Name = N'Executive Management')
                    INSERT INTO Departments (Name, IsExecutive) VALUES (N'Executive Management', 1);
                ELSE
                    UPDATE Departments SET IsExecutive = 1 WHERE Name = N'Executive Management';
            ");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(name: "FK_Dept_DeputyHod", table: "Departments");
            migrationBuilder.DropColumn(name: "DeputyHodUserId", table: "Departments");
            migrationBuilder.DropColumn(name: "IsExecutive", table: "Departments");
        }
    }
}
