using System;
using LogisticsApi.Data;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace LogisticsApi.Data.Migrations
{
    [Migration("20260523000000_InitialCreate")]
    [DbContext(typeof(AppDbContext))]
    public partial class InitialCreate : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "AuditLogs",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false, defaultValueSql: "NEWSEQUENTIALID()"),
                    EntityType = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    EntityId = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    Action = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    UserId = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    UserEmail = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    Timestamp = table.Column<DateTime>(type: "datetime2", nullable: false, defaultValueSql: "GETUTCDATE()"),
                    IpAddress = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    Notes = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    OldValues = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    NewValues = table.Column<string>(type: "nvarchar(max)", nullable: true)
                },
                constraints: table => table.PrimaryKey("PK_AuditLogs", x => x.Id));

            migrationBuilder.CreateTable(
                name: "Users",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false, defaultValueSql: "NEWSEQUENTIALID()"),
                    EntraObjectId = table.Column<string>(type: "nvarchar(36)", maxLength: 36, nullable: false),
                    FullName = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: false),
                    Email = table.Column<string>(type: "nvarchar(150)", maxLength: 150, nullable: false),
                    PhoneNumber = table.Column<string>(type: "nvarchar(20)", maxLength: 20, nullable: true),
                    Role = table.Column<string>(type: "nvarchar(30)", maxLength: 30, nullable: false),
                    DriverStatus = table.Column<string>(type: "nvarchar(20)", maxLength: 20, nullable: true),
                    LicenceNo = table.Column<string>(type: "nvarchar(30)", maxLength: 30, nullable: true),
                    LicenceExpiry = table.Column<DateOnly>(type: "date", nullable: true),
                    IsActive = table.Column<bool>(type: "bit", nullable: false, defaultValue: true),
                    LastStatusChange = table.Column<DateTime>(type: "datetime2", nullable: true),
                    CreatedAt = table.Column<DateTime>(type: "datetime2", nullable: false, defaultValueSql: "GETUTCDATE()")
                },
                constraints: table => table.PrimaryKey("PK_Users", x => x.Id));

            migrationBuilder.CreateIndex(
                name: "IX_Users_EntraObjectId",
                table: "Users",
                column: "EntraObjectId",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_Users_Email",
                table: "Users",
                column: "Email",
                unique: true);

            migrationBuilder.CreateTable(
                name: "Vehicles",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false, defaultValueSql: "NEWSEQUENTIALID()"),
                    RegistrationNo = table.Column<string>(type: "nvarchar(20)", maxLength: 20, nullable: false),
                    Make = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: false),
                    Model = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: false),
                    Year = table.Column<short>(type: "smallint", nullable: false),
                    Status = table.Column<string>(type: "nvarchar(20)", maxLength: 20, nullable: false, defaultValue: "Available"),
                    FuelType = table.Column<string>(type: "nvarchar(20)", maxLength: 20, nullable: false, defaultValue: "Diesel"),
                    OdometerKm = table.Column<int>(type: "int", nullable: false, defaultValue: 0),
                    ServiceIntervalKm = table.Column<int>(type: "int", nullable: false, defaultValue: 10000),
                    LastServiceDate = table.Column<DateOnly>(type: "date", nullable: true),
                    NextServiceDate = table.Column<DateOnly>(type: "date", nullable: true),
                    AssignedMechanicId = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    CreatedAt = table.Column<DateTime>(type: "datetime2", nullable: false, defaultValueSql: "GETUTCDATE()"),
                    UpdatedAt = table.Column<DateTime>(type: "datetime2", nullable: false, defaultValueSql: "GETUTCDATE()")
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Vehicles", x => x.Id);
                    table.ForeignKey(
                        name: "FK_Vehicles_Users_AssignedMechanicId",
                        column: x => x.AssignedMechanicId,
                        principalTable: "Users",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.SetNull);
                });

            migrationBuilder.CreateIndex(
                name: "IX_Vehicles_RegistrationNo",
                table: "Vehicles",
                column: "RegistrationNo",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_Vehicles_AssignedMechanicId",
                table: "Vehicles",
                column: "AssignedMechanicId");

            migrationBuilder.CreateTable(
                name: "TripRequests",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false, defaultValueSql: "NEWSEQUENTIALID()"),
                    RequestedById = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    Purpose = table.Column<string>(type: "nvarchar(500)", maxLength: 500, nullable: false),
                    PickupLocation = table.Column<string>(type: "nvarchar(200)", maxLength: 200, nullable: false),
                    DestinationLocation = table.Column<string>(type: "nvarchar(200)", maxLength: 200, nullable: false),
                    RequestedDateTime = table.Column<DateTime>(type: "datetime2", nullable: false),
                    Status = table.Column<string>(type: "nvarchar(20)", maxLength: 20, nullable: false, defaultValue: "Pending"),
                    Priority = table.Column<string>(type: "nvarchar(20)", maxLength: 20, nullable: false, defaultValue: "Normal"),
                    Notes = table.Column<string>(type: "nvarchar(500)", maxLength: 500, nullable: true),
                    CreatedAt = table.Column<DateTime>(type: "datetime2", nullable: false, defaultValueSql: "GETUTCDATE()")
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_TripRequests", x => x.Id);
                    table.ForeignKey(
                        name: "FK_TripRequests_Users_RequestedById",
                        column: x => x.RequestedById,
                        principalTable: "Users",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateIndex(
                name: "IX_TripRequests_RequestedById",
                table: "TripRequests",
                column: "RequestedById");

            migrationBuilder.CreateTable(
                name: "Assignments",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false, defaultValueSql: "NEWSEQUENTIALID()"),
                    TripRequestId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    DriverId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    VehicleId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    AssignedById = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    AssignmentType = table.Column<string>(type: "nvarchar(10)", maxLength: 10, nullable: false, defaultValue: "Auto"),
                    Status = table.Column<string>(type: "nvarchar(20)", maxLength: 20, nullable: false, defaultValue: "Active"),
                    StartTime = table.Column<DateTime>(type: "datetime2", nullable: false),
                    EstimatedEndTime = table.Column<DateTime>(type: "datetime2", nullable: true),
                    ActualEndTime = table.Column<DateTime>(type: "datetime2", nullable: true),
                    Notes = table.Column<string>(type: "nvarchar(500)", maxLength: 500, nullable: true),
                    CreatedAt = table.Column<DateTime>(type: "datetime2", nullable: false, defaultValueSql: "GETUTCDATE()")
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Assignments", x => x.Id);
                    table.ForeignKey(
                        name: "FK_Assignments_TripRequests_TripRequestId",
                        column: x => x.TripRequestId,
                        principalTable: "TripRequests",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_Assignments_Users_DriverId",
                        column: x => x.DriverId,
                        principalTable: "Users",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_Assignments_Users_AssignedById",
                        column: x => x.AssignedById,
                        principalTable: "Users",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_Assignments_Vehicles_VehicleId",
                        column: x => x.VehicleId,
                        principalTable: "Vehicles",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateIndex(name: "IX_Assignments_TripRequestId", table: "Assignments", column: "TripRequestId", unique: true);
            migrationBuilder.CreateIndex(name: "IX_Assignments_DriverId", table: "Assignments", column: "DriverId");
            migrationBuilder.CreateIndex(name: "IX_Assignments_VehicleId", table: "Assignments", column: "VehicleId");
            migrationBuilder.CreateIndex(name: "IX_Assignments_AssignedById", table: "Assignments", column: "AssignedById");

            migrationBuilder.CreateTable(
                name: "MaintenanceRecords",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false, defaultValueSql: "NEWSEQUENTIALID()"),
                    VehicleId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    Type = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: false),
                    ScheduledDate = table.Column<DateOnly>(type: "date", nullable: false),
                    CompletedDate = table.Column<DateOnly>(type: "date", nullable: true),
                    Cost = table.Column<decimal>(type: "decimal(10,2)", nullable: true),
                    VendorName = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: true),
                    VendorContact = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: true),
                    Notes = table.Column<string>(type: "nvarchar(1000)", maxLength: 1000, nullable: true),
                    Status = table.Column<string>(type: "nvarchar(20)", maxLength: 20, nullable: false, defaultValue: "Scheduled"),
                    AttachmentBlobUrl = table.Column<string>(type: "nvarchar(500)", maxLength: 500, nullable: true),
                    CreatedAt = table.Column<DateTime>(type: "datetime2", nullable: false, defaultValueSql: "GETUTCDATE()"),
                    UpdatedAt = table.Column<DateTime>(type: "datetime2", nullable: false, defaultValueSql: "GETUTCDATE()")
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_MaintenanceRecords", x => x.Id);
                    table.ForeignKey(
                        name: "FK_MaintenanceRecords_Vehicles_VehicleId",
                        column: x => x.VehicleId,
                        principalTable: "Vehicles",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(name: "IX_MaintenanceRecords_VehicleId", table: "MaintenanceRecords", column: "VehicleId");

            migrationBuilder.CreateTable(
                name: "FuelLogs",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false, defaultValueSql: "NEWSEQUENTIALID()"),
                    VehicleId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    LoggedById = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    FuelDate = table.Column<DateOnly>(type: "date", nullable: false),
                    LitresFilled = table.Column<decimal>(type: "decimal(8,2)", nullable: false),
                    CostPerLitre = table.Column<decimal>(type: "decimal(8,4)", nullable: false),
                    TotalCost = table.Column<decimal>(type: "decimal(10,2)", nullable: false),
                    OdometerAtFill = table.Column<int>(type: "int", nullable: false),
                    StationName = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: true),
                    ReceiptBlobUrl = table.Column<string>(type: "nvarchar(500)", maxLength: 500, nullable: true),
                    Notes = table.Column<string>(type: "nvarchar(500)", maxLength: 500, nullable: true),
                    CreatedAt = table.Column<DateTime>(type: "datetime2", nullable: false, defaultValueSql: "GETUTCDATE()")
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_FuelLogs", x => x.Id);
                    table.ForeignKey(
                        name: "FK_FuelLogs_Vehicles_VehicleId",
                        column: x => x.VehicleId,
                        principalTable: "Vehicles",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_FuelLogs_Users_LoggedById",
                        column: x => x.LoggedById,
                        principalTable: "Users",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateIndex(name: "IX_FuelLogs_VehicleId", table: "FuelLogs", column: "VehicleId");
            migrationBuilder.CreateIndex(name: "IX_FuelLogs_LoggedById", table: "FuelLogs", column: "LoggedById");

            migrationBuilder.CreateTable(
                name: "Notifications",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false, defaultValueSql: "NEWSEQUENTIALID()"),
                    RecipientId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    Channel = table.Column<string>(type: "nvarchar(20)", maxLength: 20, nullable: false, defaultValue: "InApp"),
                    Type = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: false),
                    Subject = table.Column<string>(type: "nvarchar(200)", maxLength: 200, nullable: false),
                    Body = table.Column<string>(type: "nvarchar(2000)", maxLength: 2000, nullable: false),
                    IsRead = table.Column<bool>(type: "bit", nullable: false, defaultValue: false),
                    Status = table.Column<string>(type: "nvarchar(20)", maxLength: 20, nullable: false, defaultValue: "Pending"),
                    SentAt = table.Column<DateTime>(type: "datetime2", nullable: true),
                    RelatedEntityType = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: true),
                    RelatedEntityId = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: true),
                    CreatedAt = table.Column<DateTime>(type: "datetime2", nullable: false, defaultValueSql: "GETUTCDATE()")
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Notifications", x => x.Id);
                    table.ForeignKey(
                        name: "FK_Notifications_Users_RecipientId",
                        column: x => x.RecipientId,
                        principalTable: "Users",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(name: "IX_Notifications_RecipientId", table: "Notifications", column: "RecipientId");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(name: "AuditLogs");
            migrationBuilder.DropTable(name: "Notifications");
            migrationBuilder.DropTable(name: "FuelLogs");
            migrationBuilder.DropTable(name: "MaintenanceRecords");
            migrationBuilder.DropTable(name: "Assignments");
            migrationBuilder.DropTable(name: "TripRequests");
            migrationBuilder.DropTable(name: "Vehicles");
            migrationBuilder.DropTable(name: "Users");
        }
    }
}
