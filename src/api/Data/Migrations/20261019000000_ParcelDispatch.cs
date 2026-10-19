using LogisticsApi.Data;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace LogisticsApi.Data.Migrations
{
    [Migration("20261019000000_ParcelDispatch")]
    [DbContext(typeof(AppDbContext))]
    /// <inheritdoc />
    public partial class AddParcelDispatch : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            // Parcel dispatch, requested by the Logistics Manager: sender, receiver
            // and parcel details, HOD then GM Logistics approval, then Logistics
            // records the courier and tracking number.
            migrationBuilder.CreateTable(
                name: "ParcelDispatches",
                columns: t => new
                {
                    Id = t.Column<Guid>(nullable: false, defaultValueSql: "NEWSEQUENTIALID()"),
                    FormNumber = t.Column<string>(maxLength: 30, nullable: false),

                    RequestedById = t.Column<Guid>(nullable: false),
                    SenderName = t.Column<string>(maxLength: 150, nullable: false),
                    SenderLocation = t.Column<string>(maxLength: 100, nullable: false),
                    SenderDepartment = t.Column<string>(maxLength: 150, nullable: false),
                    SenderPhone = t.Column<string>(maxLength: 50, nullable: true),

                    ReceiverName = t.Column<string>(maxLength: 150, nullable: false),
                    ReceiverAddress = t.Column<string>(maxLength: 500, nullable: false),
                    ReceiverPhone = t.Column<string>(maxLength: 50, nullable: true),

                    Description = t.Column<string>(maxLength: 1000, nullable: false),
                    IsFragile = t.Column<bool>(nullable: false, defaultValue: false),
                    Priority = t.Column<string>(maxLength: 20, nullable: false, defaultValue: "Normal"),
                    Scope = t.Column<string>(maxLength: 20, nullable: false, defaultValue: "Domestic"),
                    Carrier = t.Column<string>(maxLength: 50, nullable: false),

                    Status = t.Column<string>(maxLength: 30, nullable: false, defaultValue: "PendingHOD"),
                    HodApprovedById = t.Column<Guid>(nullable: true),
                    HodApprovedAt = t.Column<DateTime>(nullable: true),
                    HodRemarks = t.Column<string>(maxLength: 500, nullable: true),
                    ManagerApprovedById = t.Column<Guid>(nullable: true),
                    ManagerApprovedAt = t.Column<DateTime>(nullable: true),
                    ManagerRemarks = t.Column<string>(maxLength: 500, nullable: true),
                    RejectionReason = t.Column<string>(maxLength: 500, nullable: true),
                    RejectedAt = t.Column<DateTime>(nullable: true),

                    TrackingNumber = t.Column<string>(maxLength: 100, nullable: true),
                    DispatchedAt = t.Column<DateTime>(nullable: true),
                    DispatchedById = t.Column<Guid>(nullable: true),

                    CreatedAt = t.Column<DateTime>(nullable: false, defaultValueSql: "GETUTCDATE()"),
                    UpdatedAt = t.Column<DateTime>(nullable: false, defaultValueSql: "GETUTCDATE()")
                },
                constraints: t =>
                {
                    t.PrimaryKey("PK_ParcelDispatches", x => x.Id);
                    t.ForeignKey("FK_PD_RequestedBy", x => x.RequestedById, "Users", "Id", onDelete: ReferentialAction.Restrict);
                    t.ForeignKey("FK_PD_HodApprovedBy", x => x.HodApprovedById, "Users", "Id", onDelete: ReferentialAction.NoAction);
                    t.ForeignKey("FK_PD_ManagerApprovedBy", x => x.ManagerApprovedById, "Users", "Id", onDelete: ReferentialAction.NoAction);
                    t.ForeignKey("FK_PD_DispatchedBy", x => x.DispatchedById, "Users", "Id", onDelete: ReferentialAction.NoAction);
                });

            migrationBuilder.CreateIndex(
                name: "IX_ParcelDispatches_FormNumber",
                table: "ParcelDispatches", column: "FormNumber", unique: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable("ParcelDispatches");
        }
    }
}
