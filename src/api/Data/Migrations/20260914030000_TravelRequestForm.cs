using LogisticsApi.Data;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace LogisticsApi.Data.Migrations
{
    [Migration("20260914030000_TravelRequestForm")]
    [DbContext(typeof(AppDbContext))]
    /// <inheritdoc />
    public partial class TravelRequestForm : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            // ── Departments ───────────────────────────────────────────────────
            // The head is held against the department rather than the person,
            // because one person can head more than one — Maurizio Benassi heads
            // both Business Development and Contracts. A single field on the user
            // could never express that.
            migrationBuilder.CreateTable(
                name: "Departments",
                columns: t => new
                {
                    Id = t.Column<Guid>(nullable: false, defaultValueSql: "NEWSEQUENTIALID()"),
                    Name = t.Column<string>(maxLength: 150, nullable: false),
                    HodUserId = t.Column<Guid>(nullable: true),
                    IsActive = t.Column<bool>(nullable: false, defaultValue: true),
                    CreatedAt = t.Column<DateTime>(nullable: false, defaultValueSql: "GETUTCDATE()")
                },
                constraints: t =>
                {
                    t.PrimaryKey("PK_Departments", x => x.Id);
                    t.ForeignKey("FK_Dept_Hod", x => x.HodUserId, "Users", "Id",
                        onDelete: ReferentialAction.NoAction);
                });

            migrationBuilder.CreateIndex(
                name: "IX_Departments_Name",
                table: "Departments", column: "Name", unique: true);

            // The sixteen Desicon departments as supplied by the logistics
            // manager. Heads are linked separately, from the admin screen, once
            // each HOD has a platform account with their real email address —
            // guessing sixteen mailboxes here would be worse than leaving the
            // link empty, since a wrong address fails silently.
            migrationBuilder.Sql(@"
                INSERT INTO Departments (Name) VALUES
                    (N'Account, Control & Finance'),
                    (N'Business Development'),
                    (N'Commercial & Proposal'),
                    (N'Contracts'),
                    (N'General Counsel'),
                    (N'General Services'),
                    (N'HSE'),
                    (N'Human Resources & Administration'),
                    (N'ICT'),
                    (N'Logistics'),
                    (N'Procurement'),
                    (N'Project Control'),
                    (N'Project Operations'),
                    (N'QA/QC'),
                    (N'Security'),
                    (N'Technical Services');
            ");

            // ── Department and position on users ──────────────────────────────
            migrationBuilder.AddColumn<Guid>(
                name: "DepartmentId", table: "Users",
                type: "uniqueidentifier", nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "Position", table: "Users",
                type: "nvarchar(100)", maxLength: 100, nullable: true);

            migrationBuilder.CreateIndex(
                name: "IX_Users_DepartmentId",
                table: "Users", column: "DepartmentId");

            migrationBuilder.AddForeignKey(
                name: "FK_Users_Department",
                table: "Users", column: "DepartmentId",
                principalTable: "Departments", principalColumn: "Id",
                onDelete: ReferentialAction.SetNull);

            // ── Travel Request Form — DEL-LG-FRM-002 Rev 07 ───────────────────
            // The Phase 3 TravelRequests table was scaffolding for a simpler,
            // single-leg form that the business never adopted. The paper TRF is
            // the real requirement, and its shape differs enough that rebuilding
            // the table is cleaner than bending the old one into position.
            migrationBuilder.DropTable("TravelRequests");

            migrationBuilder.CreateTable(
                name: "TravelRequests",
                columns: t => new
                {
                    Id = t.Column<Guid>(nullable: false, defaultValueSql: "NEWSEQUENTIALID()"),
                    FormNumber = t.Column<string>(maxLength: 30, nullable: false),
                    FormDate = t.Column<DateTime>(nullable: false),
                    ProjectCostCentreCode = t.Column<string>(maxLength: 50, nullable: true),

                    RequestedById = t.Column<Guid>(nullable: false),
                    Surname = t.Column<string>(maxLength: 100, nullable: false),
                    GivenName = t.Column<string>(maxLength: 100, nullable: false),
                    Department = t.Column<string>(maxLength: 100, nullable: false),
                    Position = t.Column<string>(maxLength: 100, nullable: true),
                    PhoneNumber = t.Column<string>(maxLength: 50, nullable: true),
                    Email = t.Column<string>(maxLength: 256, nullable: true),

                    PurposeOfTravel = t.Column<string>(maxLength: 1000, nullable: false),
                    HotelBookingRequired = t.Column<bool>(nullable: false, defaultValue: false),
                    OtherInformation = t.Column<string>(maxLength: 1000, nullable: true),

                    Status = t.Column<string>(maxLength: 30, nullable: false, defaultValue: "PendingVerification"),
                    VerifiedById = t.Column<Guid>(nullable: true),
                    VerifiedAt = t.Column<DateTime>(nullable: true),
                    VerificationNotes = t.Column<string>(maxLength: 500, nullable: true),
                    ApprovedById = t.Column<Guid>(nullable: true),
                    ApprovedAt = t.Column<DateTime>(nullable: true),
                    ApprovalNotes = t.Column<string>(maxLength: 500, nullable: true),
                    RejectionReason = t.Column<string>(maxLength: 500, nullable: true),
                    RejectedAt = t.Column<DateTime>(nullable: true),

                    CreatedAt = t.Column<DateTime>(nullable: false, defaultValueSql: "GETUTCDATE()"),
                    UpdatedAt = t.Column<DateTime>(nullable: false, defaultValueSql: "GETUTCDATE()")
                },
                constraints: t =>
                {
                    t.PrimaryKey("PK_TravelRequests", x => x.Id);
                    t.ForeignKey("FK_TRF_RequestedBy", x => x.RequestedById, "Users", "Id", onDelete: ReferentialAction.Restrict);
                    t.ForeignKey("FK_TRF_VerifiedBy",  x => x.VerifiedById,  "Users", "Id", onDelete: ReferentialAction.NoAction);
                    t.ForeignKey("FK_TRF_ApprovedBy",  x => x.ApprovedById,  "Users", "Id", onDelete: ReferentialAction.NoAction);
                });

            // Form numbers are quoted between the logistics team and the
            // traveller, so two requests must never share one.
            migrationBuilder.CreateIndex(
                name: "IX_TravelRequests_FormNumber",
                table: "TravelRequests", column: "FormNumber", unique: true);

            // ── Routing rows ──────────────────────────────────────────────────
            // The paper form gives several blank rows under Outbound and Inbound,
            // so a journey with connections is one request with several legs.
            migrationBuilder.CreateTable(
                name: "TravelRequestLegs",
                columns: t => new
                {
                    Id = t.Column<Guid>(nullable: false, defaultValueSql: "NEWSEQUENTIALID()"),
                    TravelRequestId = t.Column<Guid>(nullable: false),
                    Direction = t.Column<string>(maxLength: 20, nullable: false),
                    Sequence = t.Column<int>(nullable: false, defaultValue: 0),
                    TravelDate = t.Column<DateOnly>(nullable: false),
                    From = t.Column<string>(maxLength: 150, nullable: false),
                    To = t.Column<string>(maxLength: 150, nullable: false),
                    PreferredAirline = t.Column<string>(maxLength: 100, nullable: true),
                    PreferredTime = t.Column<string>(maxLength: 50, nullable: true)
                },
                constraints: t =>
                {
                    t.PrimaryKey("PK_TravelRequestLegs", x => x.Id);
                    t.ForeignKey("FK_TRFLeg_Request", x => x.TravelRequestId,
                        "TravelRequests", "Id", onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_TravelRequestLegs_TravelRequestId",
                table: "TravelRequestLegs", column: "TravelRequestId");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable("TravelRequestLegs");
            migrationBuilder.DropTable("TravelRequests");

            // Restore the Phase 3 shape so the migration is genuinely reversible.
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

            migrationBuilder.DropForeignKey(name: "FK_Users_Department", table: "Users");
            migrationBuilder.DropIndex(name: "IX_Users_DepartmentId", table: "Users");
            migrationBuilder.DropColumn(name: "Position",     table: "Users");
            migrationBuilder.DropColumn(name: "DepartmentId", table: "Users");
            migrationBuilder.DropTable("Departments");
        }
    }
}
