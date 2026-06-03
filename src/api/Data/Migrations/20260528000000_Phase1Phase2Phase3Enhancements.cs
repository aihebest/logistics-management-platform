using System;
using LogisticsApi.Data;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace LogisticsApi.Data.Migrations
{
    [Migration("20260528000000_Phase1Phase2Phase3Enhancements")]
    [DbContext(typeof(AppDbContext))]
    /// <inheritdoc />
    public partial class Phase1Phase2Phase3Enhancements : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            // ── Locations table (reference data) ─────────────────────────────
            migrationBuilder.CreateTable(
                name: "Locations",
                columns: table => new
                {
                    Id = table.Column<Guid>(nullable: false, defaultValueSql: "NEWSEQUENTIALID()"),
                    Name = table.Column<string>(maxLength: 200, nullable: false),
                    Code = table.Column<string>(maxLength: 20, nullable: false),
                    IsActive = table.Column<bool>(nullable: false, defaultValue: true),
                    CreatedAt = table.Column<DateTime>(nullable: false, defaultValueSql: "GETUTCDATE()"),
                },
                constraints: table => table.PrimaryKey("PK_Locations", x => x.Id));
            migrationBuilder.CreateIndex("IX_Locations_Code", "Locations", "Code", unique: true);

            // ── Phase 1: Vehicle additions ────────────────────────────────────
            migrationBuilder.AddColumn<string>("ChassisNo", "Vehicles", maxLength: 100, nullable: true);
            migrationBuilder.AddColumn<short>("PurchaseYear", "Vehicles", nullable: true);
            migrationBuilder.AddColumn<string>("Colour", "Vehicles", maxLength: 50, nullable: true);

            // ── Phase 1: MaintenanceRecord enhancements ───────────────────────
            migrationBuilder.AddColumn<string>("Category", "MaintenanceRecords",
                maxLength: 50, nullable: false, defaultValue: "Routine");
            migrationBuilder.AddColumn<bool>("FaultReported", "MaintenanceRecords",
                nullable: false, defaultValue: false);
            migrationBuilder.AddColumn<string>("FaultDescription", "MaintenanceRecords",
                maxLength: 2000, nullable: true);
            migrationBuilder.AddColumn<DateOnly>("DateReported", "MaintenanceRecords", nullable: true);
            migrationBuilder.AddColumn<string>("PartsReplaced", "MaintenanceRecords",
                maxLength: 1000, nullable: true);
            migrationBuilder.AddColumn<string>("RepairRemarks", "MaintenanceRecords",
                maxLength: 2000, nullable: true);

            // ── Phase 1: FuelLog enhancements ────────────────────────────────
            migrationBuilder.AddColumn<string>("ProductType", "FuelLogs",
                maxLength: 20, nullable: false, defaultValue: "PMS");
            migrationBuilder.AddColumn<string>("CostCentre", "FuelLogs", maxLength: 100, nullable: true);
            migrationBuilder.AddColumn<decimal>("FuelGaugeBefore", "FuelLogs", nullable: true);
            migrationBuilder.AddColumn<decimal>("FuelGaugeAfter", "FuelLogs", nullable: true);
            migrationBuilder.AddColumn<int>("OdometerFrom", "FuelLogs", nullable: true);
            migrationBuilder.AddColumn<int>("OdometerTo", "FuelLogs", nullable: true);
            migrationBuilder.AddColumn<int>("MileageCovered", "FuelLogs", nullable: true);
            migrationBuilder.AddColumn<bool>("IsCashPayment", "FuelLogs",
                nullable: false, defaultValue: false);

            // ── Phase 1: TripRequest enhancements ────────────────────────────
            migrationBuilder.AddColumn<string>("MovementType", "TripRequests",
                maxLength: 50, nullable: false, defaultValue: "IntraState");
            migrationBuilder.AddColumn<DateOnly>("DepartureDate", "TripRequests", nullable: true);
            migrationBuilder.AddColumn<string>("DepartureTime", "TripRequests",
                maxLength: 10, nullable: true);

            // ── Phase 2: MaterialTransportRequests ───────────────────────────
            migrationBuilder.CreateTable(
                name: "MaterialTransportRequests",
                columns: t => new
                {
                    Id = t.Column<Guid>(nullable: false, defaultValueSql: "NEWSEQUENTIALID()"),
                    FormNumber = t.Column<string>(maxLength: 50, nullable: false),
                    RequestedById = t.Column<Guid>(nullable: false),
                    ProjectName = t.Column<string>(maxLength: 200, nullable: false),
                    Purpose = t.Column<string>(maxLength: 500, nullable: false),
                    LoadingPoint = t.Column<string>(maxLength: 300, nullable: false),
                    LoadingContactPerson = t.Column<string>(maxLength: 200, nullable: true),
                    LoadingContactPhone = t.Column<string>(maxLength: 50, nullable: true),
                    LoadingDate = t.Column<DateOnly>(nullable: true),
                    DeliveryPoint = t.Column<string>(maxLength: 300, nullable: false),
                    DeliveryContactPerson = t.Column<string>(maxLength: 200, nullable: true),
                    DeliveryContactPhone = t.Column<string>(maxLength: 50, nullable: true),
                    DeliveryDate = t.Column<DateOnly>(nullable: true),
                    Status = t.Column<string>(maxLength: 50, nullable: false, defaultValue: "Draft"),
                    HodApprovedById = t.Column<Guid>(nullable: true),
                    HodApprovedAt = t.Column<DateTime>(nullable: true),
                    HodRemarks = t.Column<string>(maxLength: 500, nullable: true),
                    ManagerApprovedById = t.Column<Guid>(nullable: true),
                    ManagerApprovedAt = t.Column<DateTime>(nullable: true),
                    ManagerRemarks = t.Column<string>(maxLength: 500, nullable: true),
                    AssignedDriverId = t.Column<Guid>(nullable: true),
                    AssignedVehicleId = t.Column<Guid>(nullable: true),
                    CreatedAt = t.Column<DateTime>(nullable: false, defaultValueSql: "GETUTCDATE()")
                },
                constraints: t =>
                {
                    t.PrimaryKey("PK_MaterialTransportRequests", x => x.Id);
                    t.ForeignKey("FK_MTR_RequestedBy", x => x.RequestedById, "Users", "Id", onDelete: ReferentialAction.Restrict);
                    t.ForeignKey("FK_MTR_HOD", x => x.HodApprovedById, "Users", "Id", onDelete: ReferentialAction.NoAction);
                    t.ForeignKey("FK_MTR_Manager", x => x.ManagerApprovedById, "Users", "Id", onDelete: ReferentialAction.NoAction);
                    t.ForeignKey("FK_MTR_Driver", x => x.AssignedDriverId, "Users", "Id", onDelete: ReferentialAction.NoAction);
                    t.ForeignKey("FK_MTR_Vehicle", x => x.AssignedVehicleId, "Vehicles", "Id", onDelete: ReferentialAction.NoAction);
                });

            migrationBuilder.CreateTable(
                name: "MaterialTransportItems",
                columns: t => new
                {
                    Id = t.Column<Guid>(nullable: false, defaultValueSql: "NEWSEQUENTIALID()"),
                    MaterialTransportRequestId = t.Column<Guid>(nullable: false),
                    SNo = t.Column<int>(nullable: false),
                    Material = t.Column<string>(maxLength: 200, nullable: false),
                    Description = t.Column<string>(maxLength: 1000, nullable: true),
                    Quantity = t.Column<decimal>(nullable: false, defaultValue: 0m)
                },
                constraints: t =>
                {
                    t.PrimaryKey("PK_MaterialTransportItems", x => x.Id);
                    t.ForeignKey("FK_MTI_Request", x => x.MaterialTransportRequestId,
                        "MaterialTransportRequests", "Id", onDelete: ReferentialAction.Cascade);
                });

            // ── Phase 2: DriverSchedules ─────────────────────────────────────
            migrationBuilder.CreateTable(
                name: "DriverSchedules",
                columns: t => new
                {
                    Id = t.Column<Guid>(nullable: false, defaultValueSql: "NEWSEQUENTIALID()"),
                    DriverId = t.Column<Guid>(nullable: false),
                    ScheduleDate = t.Column<DateOnly>(nullable: false),
                    Location = t.Column<string>(maxLength: 300, nullable: false),
                    Shift = t.Column<string>(maxLength: 20, nullable: false, defaultValue: "Day"),
                    Notes = t.Column<string>(maxLength: 500, nullable: true),
                    CreatedById = t.Column<Guid>(nullable: false),
                    CreatedAt = t.Column<DateTime>(nullable: false, defaultValueSql: "GETUTCDATE()")
                },
                constraints: t =>
                {
                    t.PrimaryKey("PK_DriverSchedules", x => x.Id);
                    t.ForeignKey("FK_DS_Driver", x => x.DriverId, "Users", "Id", onDelete: ReferentialAction.Restrict);
                    t.ForeignKey("FK_DS_CreatedBy", x => x.CreatedById, "Users", "Id", onDelete: ReferentialAction.NoAction);
                });

            // ── Phase 2: DriverIncidents ──────────────────────────────────────
            migrationBuilder.CreateTable(
                name: "DriverIncidents",
                columns: t => new
                {
                    Id = t.Column<Guid>(nullable: false, defaultValueSql: "NEWSEQUENTIALID()"),
                    DriverId = t.Column<Guid>(nullable: false),
                    IncidentDate = t.Column<DateOnly>(nullable: false),
                    Type = t.Column<string>(maxLength: 100, nullable: false),
                    Description = t.Column<string>(maxLength: 2000, nullable: false),
                    Severity = t.Column<string>(maxLength: 20, nullable: false, defaultValue: "Minor"),
                    ActionTaken = t.Column<string>(maxLength: 1000, nullable: true),
                    Notes = t.Column<string>(maxLength: 500, nullable: true),
                    ReportedById = t.Column<Guid>(nullable: false),
                    CreatedAt = t.Column<DateTime>(nullable: false, defaultValueSql: "GETUTCDATE()")
                },
                constraints: t =>
                {
                    t.PrimaryKey("PK_DriverIncidents", x => x.Id);
                    t.ForeignKey("FK_DI_Driver", x => x.DriverId, "Users", "Id", onDelete: ReferentialAction.Restrict);
                    t.ForeignKey("FK_DI_ReportedBy", x => x.ReportedById, "Users", "Id", onDelete: ReferentialAction.NoAction);
                });

            // ── Phase 3: TravelRequests ───────────────────────────────────────
            migrationBuilder.CreateTable(
                name: "TravelRequests",
                columns: t => new
                {
                    Id = t.Column<Guid>(nullable: false, defaultValueSql: "NEWSEQUENTIALID()"),
                    RequestedById = t.Column<Guid>(nullable: false),
                    TravelType = t.Column<string>(maxLength: 50, nullable: false),
                    TravellerName = t.Column<string>(maxLength: 200, nullable: false),
                    Purpose = t.Column<string>(maxLength: 500, nullable: false),
                    Origin = t.Column<string>(maxLength: 200, nullable: false),
                    Destination = t.Column<string>(maxLength: 200, nullable: false),
                    TravelDate = t.Column<DateOnly>(nullable: false),
                    ReturnDate = t.Column<DateOnly>(nullable: true),
                    FlightPreference = t.Column<string>(maxLength: 200, nullable: true),
                    HotelName = t.Column<string>(maxLength: 200, nullable: true),
                    NumberOfNights = t.Column<int>(nullable: true),
                    PassportNumber = t.Column<string>(maxLength: 50, nullable: true),
                    Status = t.Column<string>(maxLength: 50, nullable: false, defaultValue: "Pending"),
                    ApprovalNotes = t.Column<string>(maxLength: 500, nullable: true),
                    ApprovedById = t.Column<Guid>(nullable: true),
                    ApprovedAt = t.Column<DateTime>(nullable: true),
                    CreatedAt = t.Column<DateTime>(nullable: false, defaultValueSql: "GETUTCDATE()"),
                    UpdatedAt = t.Column<DateTime>(nullable: false, defaultValueSql: "GETUTCDATE()")
                },
                constraints: t =>
                {
                    t.PrimaryKey("PK_TravelRequests", x => x.Id);
                    t.ForeignKey("FK_TR_RequestedBy", x => x.RequestedById, "Users", "Id", onDelete: ReferentialAction.Restrict);
                    t.ForeignKey("FK_TR_ApprovedBy", x => x.ApprovedById, "Users", "Id", onDelete: ReferentialAction.NoAction);
                });

            // ── Phase 3: ProjectMaterialTrackings ─────────────────────────────
            migrationBuilder.CreateTable(
                name: "ProjectMaterialTrackings",
                columns: t => new
                {
                    Id = t.Column<Guid>(nullable: false, defaultValueSql: "NEWSEQUENTIALID()"),
                    TrackingYear = t.Column<int>(nullable: false),
                    PoNumber = t.Column<string>(maxLength: 100, nullable: true),
                    PoLineItem = t.Column<string>(maxLength: 50, nullable: true),
                    Project = t.Column<string>(maxLength: 200, nullable: false),
                    Buyer = t.Column<string>(maxLength: 200, nullable: false),
                    Description = t.Column<string>(maxLength: 1000, nullable: false),
                    Quantity = t.Column<decimal>(nullable: true),
                    Supplier = t.Column<string>(maxLength: 300, nullable: true),
                    FreightForwarder = t.Column<string>(maxLength: 300, nullable: true),
                    ReadinessDate = t.Column<DateOnly>(nullable: true),
                    PickupAuthDate = t.Column<DateOnly>(nullable: true),
                    PickupDate = t.Column<DateOnly>(nullable: true),
                    ModeOfTransport = t.Column<string>(maxLength: 50, nullable: true),
                    FormMNumber = t.Column<string>(maxLength: 100, nullable: true),
                    BlAwbNumber = t.Column<string>(maxLength: 100, nullable: true),
                    VesselName = t.Column<string>(maxLength: 200, nullable: true),
                    Etd = t.Column<DateOnly>(nullable: true),
                    Eta = t.Column<DateOnly>(nullable: true),
                    DeliveryStatus = t.Column<string>(maxLength: 50, nullable: false, defaultValue: "Pending"),
                    Remarks = t.Column<string>(maxLength: 1000, nullable: true),
                    ActualDeliveryDate = t.Column<DateOnly>(nullable: true),
                    CreatedById = t.Column<Guid>(nullable: false),
                    CreatedAt = t.Column<DateTime>(nullable: false, defaultValueSql: "GETUTCDATE()"),
                    UpdatedAt = t.Column<DateTime>(nullable: false, defaultValueSql: "GETUTCDATE()")
                },
                constraints: t =>
                {
                    t.PrimaryKey("PK_ProjectMaterialTrackings", x => x.Id);
                    t.ForeignKey("FK_PMT_CreatedBy", x => x.CreatedById, "Users", "Id", onDelete: ReferentialAction.Restrict);
                });

            // ── Phase 3: MovementRegisters ────────────────────────────────────
            migrationBuilder.CreateTable(
                name: "MovementRegisters",
                columns: t => new
                {
                    Id = t.Column<Guid>(nullable: false, defaultValueSql: "NEWSEQUENTIALID()"),
                    MovementType = t.Column<string>(maxLength: 50, nullable: false),
                    VehicleId = t.Column<Guid>(nullable: true),
                    DriverId = t.Column<Guid>(nullable: true),
                    RelatedRefNo = t.Column<string>(maxLength: 100, nullable: true),
                    Purpose = t.Column<string>(maxLength: 500, nullable: false),
                    Origin = t.Column<string>(maxLength: 300, nullable: true),
                    Destination = t.Column<string>(maxLength: 300, nullable: true),
                    MovementDateTime = t.Column<DateTime>(nullable: false),
                    ReturnDateTime = t.Column<DateTime>(nullable: true),
                    GatePassNo = t.Column<string>(maxLength: 50, nullable: true),
                    Status = t.Column<string>(maxLength: 20, nullable: false, defaultValue: "Open"),
                    Notes = t.Column<string>(maxLength: 500, nullable: true),
                    LoggedById = t.Column<Guid>(nullable: false),
                    CreatedAt = t.Column<DateTime>(nullable: false, defaultValueSql: "GETUTCDATE()")
                },
                constraints: t =>
                {
                    t.PrimaryKey("PK_MovementRegisters", x => x.Id);
                    t.ForeignKey("FK_MR_Vehicle", x => x.VehicleId, "Vehicles", "Id", onDelete: ReferentialAction.NoAction);
                    t.ForeignKey("FK_MR_Driver", x => x.DriverId, "Users", "Id", onDelete: ReferentialAction.NoAction);
                    t.ForeignKey("FK_MR_LoggedBy", x => x.LoggedById, "Users", "Id", onDelete: ReferentialAction.Restrict);
                });
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            // Drop new tables
            migrationBuilder.DropTable("MovementRegisters");
            migrationBuilder.DropTable("ProjectMaterialTrackings");
            migrationBuilder.DropTable("TravelRequests");
            migrationBuilder.DropTable("DriverIncidents");
            migrationBuilder.DropTable("DriverSchedules");
            migrationBuilder.DropTable("MaterialTransportItems");
            migrationBuilder.DropTable("MaterialTransportRequests");
            // Drop Phase 1 columns
            migrationBuilder.DropColumn("DepartureTime", "TripRequests");
            migrationBuilder.DropColumn("DepartureDate", "TripRequests");
            migrationBuilder.DropColumn("MovementType", "TripRequests");
            migrationBuilder.DropColumn("IsCashPayment", "FuelLogs");
            migrationBuilder.DropColumn("MileageCovered", "FuelLogs");
            migrationBuilder.DropColumn("OdometerTo", "FuelLogs");
            migrationBuilder.DropColumn("OdometerFrom", "FuelLogs");
            migrationBuilder.DropColumn("FuelGaugeAfter", "FuelLogs");
            migrationBuilder.DropColumn("FuelGaugeBefore", "FuelLogs");
            migrationBuilder.DropColumn("CostCentre", "FuelLogs");
            migrationBuilder.DropColumn("ProductType", "FuelLogs");
            migrationBuilder.DropColumn("RepairRemarks", "MaintenanceRecords");
            migrationBuilder.DropColumn("PartsReplaced", "MaintenanceRecords");
            migrationBuilder.DropColumn("DateReported", "MaintenanceRecords");
            migrationBuilder.DropColumn("FaultDescription", "MaintenanceRecords");
            migrationBuilder.DropColumn("FaultReported", "MaintenanceRecords");
            migrationBuilder.DropColumn("Category", "MaintenanceRecords");
            migrationBuilder.DropColumn("Colour", "Vehicles");
            migrationBuilder.DropColumn("PurchaseYear", "Vehicles");
            migrationBuilder.DropColumn("ChassisNo", "Vehicles");
        }
    }
}
