using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace LogisticsApi.Data.Migrations
{
    /// <inheritdoc />
    public partial class LocationsAndMovementEnhancements : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            // ── Locations master table ────────────────────────────────────────
            migrationBuilder.CreateTable(
                name: "Locations",
                columns: t => new
                {
                    Id   = t.Column<Guid>(nullable: false, defaultValueSql: "NEWSEQUENTIALID()"),
                    Name = t.Column<string>(maxLength: 200, nullable: false),
                    Code = t.Column<string>(maxLength: 20,  nullable: false),
                    IsActive  = t.Column<bool>(nullable: false, defaultValue: true),
                    CreatedAt = t.Column<DateTime>(nullable: false, defaultValueSql: "GETUTCDATE()")
                },
                constraints: t =>
                {
                    t.PrimaryKey("PK_Locations", x => x.Id);
                });

            migrationBuilder.CreateIndex("IX_Locations_Code", "Locations", "Code", unique: true);

            // ── Seed operational locations ────────────────────────────────────
            migrationBuilder.Sql(@"
INSERT INTO Locations (Id, Name, Code, IsActive, CreatedAt) VALUES
  ('C0000001-0000-0000-0000-000000000001', 'Desicon Lagos Office', 'LOS',   1, GETUTCDATE()),
  ('C0000001-0000-0000-0000-000000000002', 'Desicon PH Office',    'PH',    1, GETUTCDATE()),
  ('C0000001-0000-0000-0000-000000000003', 'Desicon Abuja Office', 'ABJ',   1, GETUTCDATE()),
  ('C0000001-0000-0000-0000-000000000004', 'Site Bonny',           'BONNY', 1, GETUTCDATE()),
  ('C0000001-0000-0000-0000-000000000005', 'Others',               'OTH',   1, GETUTCDATE());
");

            // ── FuelLogs: add LocationId ──────────────────────────────────────
            migrationBuilder.AddColumn<Guid>("LocationId", "FuelLogs", nullable: true);
            migrationBuilder.AddForeignKey(
                name: "FK_FuelLogs_Locations",
                table: "FuelLogs",
                column: "LocationId",
                principalTable: "Locations",
                principalColumn: "Id",
                onDelete: ReferentialAction.SetNull);

            // ── DriverSchedules: rename Location→WorkLocation, add LocationId ─
            migrationBuilder.RenameColumn("Location", "DriverSchedules", "WorkLocation");
            migrationBuilder.AddColumn<Guid>("LocationId", "DriverSchedules", nullable: true);
            migrationBuilder.AddForeignKey(
                name: "FK_DriverSchedules_Locations",
                table: "DriverSchedules",
                column: "LocationId",
                principalTable: "Locations",
                principalColumn: "Id",
                onDelete: ReferentialAction.SetNull);

            // ── MovementRegisters: add MileageOut, MileageIn ──────────────────
            migrationBuilder.AddColumn<int>("MileageOut", "MovementRegisters", nullable: true);
            migrationBuilder.AddColumn<int>("MileageIn",  "MovementRegisters", nullable: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn("MileageIn",  "MovementRegisters");
            migrationBuilder.DropColumn("MileageOut", "MovementRegisters");

            migrationBuilder.DropForeignKey("FK_DriverSchedules_Locations", "DriverSchedules");
            migrationBuilder.DropColumn("LocationId", "DriverSchedules");
            migrationBuilder.RenameColumn("WorkLocation", "DriverSchedules", "Location");

            migrationBuilder.DropForeignKey("FK_FuelLogs_Locations", "FuelLogs");
            migrationBuilder.DropColumn("LocationId", "FuelLogs");

            migrationBuilder.DropTable("Locations");
        }
    }
}
