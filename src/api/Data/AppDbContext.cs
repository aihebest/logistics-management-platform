using LogisticsApi.Models.Entities;
using Microsoft.EntityFrameworkCore;

namespace LogisticsApi.Data;

public class AppDbContext(DbContextOptions<AppDbContext> options) : DbContext(options)
{
    public DbSet<User> Users => Set<User>();
    public DbSet<Location> Locations => Set<Location>();
    public DbSet<Vehicle> Vehicles => Set<Vehicle>();
    public DbSet<TripRequest> TripRequests => Set<TripRequest>();
    public DbSet<Assignment> Assignments => Set<Assignment>();
    public DbSet<MaintenanceRecord> MaintenanceRecords => Set<MaintenanceRecord>();
    public DbSet<FuelLog> FuelLogs => Set<FuelLog>();
    public DbSet<AuditLog> AuditLogs => Set<AuditLog>();
    public DbSet<Notification> Notifications => Set<Notification>();
    // Phase 2
    public DbSet<MaterialTransportRequest> MaterialTransportRequests => Set<MaterialTransportRequest>();
    public DbSet<MaterialTransportItem> MaterialTransportItems => Set<MaterialTransportItem>();
    public DbSet<DriverSchedule> DriverSchedules => Set<DriverSchedule>();
    public DbSet<DriverIncident> DriverIncidents => Set<DriverIncident>();
    // Phase 3
    public DbSet<TravelRequest> TravelRequests => Set<TravelRequest>();
    public DbSet<TravelRequestLeg> TravelRequestLegs => Set<TravelRequestLeg>();
    public DbSet<Department> Departments => Set<Department>();
    public DbSet<ProjectMaterialTracking> ProjectMaterialTrackings => Set<ProjectMaterialTracking>();
    public DbSet<MovementRegister> MovementRegisters => Set<MovementRegister>();

    protected override void OnModelCreating(ModelBuilder mb)
    {
        mb.Entity<Location>(e =>
        {
            e.HasKey(x => x.Id);
            e.Property(x => x.Id).HasDefaultValueSql("NEWSEQUENTIALID()");
            e.HasIndex(x => x.Code).IsUnique();
            e.Property(x => x.CreatedAt).HasDefaultValueSql("GETUTCDATE()");
        });

        mb.Entity<User>(e =>
        {
            e.HasKey(x => x.Id);
            e.Property(x => x.Id).HasDefaultValueSql("NEWSEQUENTIALID()");
            e.HasIndex(x => x.EntraObjectId).IsUnique();
            // Unique per real email, but drivers are registered without one and
            // are stored with an empty email. A plain unique index treats those
            // as duplicates and allows only a single such record, so filter them
            // out of the constraint.
            e.HasIndex(x => x.Email)
             .IsUnique()
             .HasFilter("[Email] IS NOT NULL AND [Email] <> ''");
            e.Property(x => x.CreatedAt).HasDefaultValueSql("GETUTCDATE()");
            e.Property(x => x.Position).HasMaxLength(100);
            // Closing a department must not delete its people.
            e.HasOne(x => x.Department).WithMany(d => d.Members)
             .HasForeignKey(x => x.DepartmentId).OnDelete(DeleteBehavior.SetNull);
        });

        mb.Entity<Vehicle>(e =>
        {
            e.HasKey(x => x.Id);
            e.Property(x => x.Id).HasDefaultValueSql("NEWSEQUENTIALID()");
            e.HasIndex(x => x.RegistrationNo).IsUnique();
            e.Property(x => x.CreatedAt).HasDefaultValueSql("GETUTCDATE()");
            e.Property(x => x.UpdatedAt).HasDefaultValueSql("GETUTCDATE()");
            e.HasOne(x => x.AssignedMechanic)
             .WithMany()
             .HasForeignKey(x => x.AssignedMechanicId)
             .OnDelete(DeleteBehavior.SetNull);
        });

        mb.Entity<TripRequest>(e =>
        {
            e.HasKey(x => x.Id);
            e.Property(x => x.Id).HasDefaultValueSql("NEWSEQUENTIALID()");
            e.Property(x => x.CreatedAt).HasDefaultValueSql("GETUTCDATE()");
            // Lengths mirror 20260825000000_TripPersonnelAndFuelPaymentMethod so the
            // model and the database agree — drift here is what caused SQL error 207.
            e.Property(x => x.PersonnelNames).HasMaxLength(1000);
            e.Property(x => x.PersonnelCategory).HasMaxLength(50);
            e.Property(x => x.MovementDuration).HasMaxLength(50);
            e.Property(x => x.MaterialDescription).HasMaxLength(500);
            e.HasOne(x => x.RequestedBy)
             .WithMany()
             .HasForeignKey(x => x.RequestedById)
             .OnDelete(DeleteBehavior.Restrict);
        });

        mb.Entity<Assignment>(e =>
        {
            e.HasKey(x => x.Id);
            e.Property(x => x.Id).HasDefaultValueSql("NEWSEQUENTIALID()");
            e.Property(x => x.CreatedAt).HasDefaultValueSql("GETUTCDATE()");
            e.HasOne(x => x.TripRequest)
             .WithOne(t => t.Assignment)
             .HasForeignKey<Assignment>(x => x.TripRequestId)
             .OnDelete(DeleteBehavior.Restrict);
            e.HasOne(x => x.Driver)
             .WithMany(u => u.AssignmentsAsDriver)
             .HasForeignKey(x => x.DriverId)
             .OnDelete(DeleteBehavior.Restrict);
            e.HasOne(x => x.Vehicle)
             .WithMany(v => v.Assignments)
             .HasForeignKey(x => x.VehicleId)
             .OnDelete(DeleteBehavior.Restrict);
            e.HasOne(x => x.AssignedBy)
             .WithMany(u => u.AssignmentsCreated)
             .HasForeignKey(x => x.AssignedById)
             .OnDelete(DeleteBehavior.Restrict);
        });

        mb.Entity<MaintenanceRecord>(e =>
        {
            e.HasKey(x => x.Id);
            e.Property(x => x.Id).HasDefaultValueSql("NEWSEQUENTIALID()");
            // Repair cost in NGN — pinned so large amounts aren't silently truncated.
            e.Property(x => x.Cost).HasColumnType("decimal(14,2)");
            // General Service platform link. Indexed because every inbound status
            // push from GenService looks the record up by their request id.
            e.Property(x => x.GenServiceRequestNumber).HasMaxLength(40);
            e.Property(x => x.GenServiceStatus).HasMaxLength(40);
            e.Property(x => x.GenServiceFaultIdentified).HasMaxLength(2000);
            e.Property(x => x.GenServiceWorkDone).HasMaxLength(2000);
            e.Property(x => x.GenServiceWorkshopName).HasMaxLength(200);
            e.Property(x => x.SourceSystem).HasMaxLength(20).HasDefaultValue("Logistics");
            e.HasIndex(x => x.GenServiceRequestId);
            e.Property(x => x.CreatedAt).HasDefaultValueSql("GETUTCDATE()");
            e.Property(x => x.UpdatedAt).HasDefaultValueSql("GETUTCDATE()");
            e.HasOne(x => x.Vehicle)
             .WithMany(v => v.MaintenanceRecords)
             .HasForeignKey(x => x.VehicleId)
             .OnDelete(DeleteBehavior.Cascade);
        });

        mb.Entity<FuelLog>(e =>
        {
            e.HasKey(x => x.Id);
            e.Property(x => x.Id).HasDefaultValueSql("NEWSEQUENTIALID()");
            e.Property(x => x.CreatedAt).HasDefaultValueSql("GETUTCDATE()");
            e.Property(x => x.LitresFilled).HasColumnType("decimal(10,3)");
            e.Property(x => x.CostPerLitre).HasColumnType("decimal(10,4)");
            e.Property(x => x.TotalCost).HasColumnType("decimal(14,2)");
            // Gauge readings are percentages (0–100).
            e.Property(x => x.FuelGaugeBefore).HasColumnType("decimal(5,2)");
            e.Property(x => x.FuelGaugeAfter).HasColumnType("decimal(5,2)");
            e.Property(x => x.PaymentMethod).HasMaxLength(20);
            // Lengths mirror 20260914020000_FuelMileageAndGaugePosition.
            e.Property(x => x.FuelGaugeBeforePosition).HasMaxLength(30);
            e.Property(x => x.FuelGaugeAfterPosition).HasMaxLength(30);
            e.HasOne(x => x.Vehicle)
             .WithMany(v => v.FuelLogs)
             .HasForeignKey(x => x.VehicleId)
             .OnDelete(DeleteBehavior.Cascade);
            e.HasOne(x => x.LoggedBy)
             .WithMany(u => u.FuelLogs)
             .HasForeignKey(x => x.LoggedById)
             .OnDelete(DeleteBehavior.Restrict);
            e.HasOne(x => x.Location).WithMany()
             .HasForeignKey(x => x.LocationId).OnDelete(DeleteBehavior.SetNull);
        });

        mb.Entity<AuditLog>(e =>
        {
            e.HasKey(x => x.Id);
            e.Property(x => x.Id).HasDefaultValueSql("NEWSEQUENTIALID()");
            e.Property(x => x.Timestamp).HasDefaultValueSql("GETUTCDATE()");
        });

        mb.Entity<Notification>(e =>
        {
            e.HasKey(x => x.Id);
            e.Property(x => x.Id).HasDefaultValueSql("NEWSEQUENTIALID()");
            e.Property(x => x.CreatedAt).HasDefaultValueSql("GETUTCDATE()");
            e.HasOne(x => x.Recipient)
             .WithMany(u => u.Notifications)
             .HasForeignKey(x => x.RecipientId)
             .OnDelete(DeleteBehavior.Cascade);
        });

        // ── Phase 2 ──────────────────────────────────────────────────────────
        mb.Entity<MaterialTransportRequest>(e =>
        {
            e.HasKey(x => x.Id);
            e.Property(x => x.Id).HasDefaultValueSql("NEWSEQUENTIALID()");
            e.Property(x => x.CreatedAt).HasDefaultValueSql("GETUTCDATE()");
            e.HasOne(x => x.RequestedBy).WithMany()
             .HasForeignKey(x => x.RequestedById).OnDelete(DeleteBehavior.Restrict);
            e.HasOne(x => x.HodApprovedBy).WithMany()
             .HasForeignKey(x => x.HodApprovedById).OnDelete(DeleteBehavior.SetNull);
            e.HasOne(x => x.ManagerApprovedBy).WithMany()
             .HasForeignKey(x => x.ManagerApprovedById).OnDelete(DeleteBehavior.SetNull);
            e.HasOne(x => x.AssignedDriver).WithMany()
             .HasForeignKey(x => x.AssignedDriverId).OnDelete(DeleteBehavior.SetNull);
            e.HasOne(x => x.AssignedVehicle).WithMany()
             .HasForeignKey(x => x.AssignedVehicleId).OnDelete(DeleteBehavior.SetNull);
        });

        mb.Entity<MaterialTransportItem>(e =>
        {
            e.HasKey(x => x.Id);
            e.Property(x => x.Id).HasDefaultValueSql("NEWSEQUENTIALID()");
            // Allows fractional quantities (e.g. metres, tonnes).
            e.Property(x => x.Quantity).HasColumnType("decimal(12,3)");
            e.HasOne(x => x.Request)
             .WithMany(r => r.Items)
             .HasForeignKey(x => x.MaterialTransportRequestId)
             .OnDelete(DeleteBehavior.Cascade);
        });

        mb.Entity<DriverSchedule>(e =>
        {
            e.HasKey(x => x.Id);
            e.Property(x => x.Id).HasDefaultValueSql("NEWSEQUENTIALID()");
            e.Property(x => x.CreatedAt).HasDefaultValueSql("GETUTCDATE()");
            e.HasOne(x => x.Driver).WithMany()
             .HasForeignKey(x => x.DriverId).OnDelete(DeleteBehavior.Restrict);
            e.HasOne(x => x.CreatedBy).WithMany()
             .HasForeignKey(x => x.CreatedById).OnDelete(DeleteBehavior.Restrict);
            e.HasOne(x => x.Location).WithMany()
             .HasForeignKey(x => x.LocationId).OnDelete(DeleteBehavior.SetNull);
        });

        mb.Entity<DriverIncident>(e =>
        {
            e.HasKey(x => x.Id);
            e.Property(x => x.Id).HasDefaultValueSql("NEWSEQUENTIALID()");
            e.Property(x => x.CreatedAt).HasDefaultValueSql("GETUTCDATE()");
            e.HasOne(x => x.Driver).WithMany()
             .HasForeignKey(x => x.DriverId).OnDelete(DeleteBehavior.Restrict);
            e.HasOne(x => x.ReportedBy).WithMany()
             .HasForeignKey(x => x.ReportedById).OnDelete(DeleteBehavior.Restrict);
        });

        // ── Phase 3 ──────────────────────────────────────────────────────────
        mb.Entity<TravelRequest>(e =>
        {
            e.HasKey(x => x.Id);
            e.Property(x => x.Id).HasDefaultValueSql("NEWSEQUENTIALID()");
            e.Property(x => x.CreatedAt).HasDefaultValueSql("GETUTCDATE()");
            // Form numbers are the reference the logistics team quote to each
            // other, so two requests must never share one.
            e.HasIndex(x => x.FormNumber).IsUnique();
            // Lengths mirror the TRF migration — drift here is what produced
            // SQL error 207 earlier in this project.
            e.Property(x => x.FormNumber).HasMaxLength(30);
            e.Property(x => x.ProjectCostCentreCode).HasMaxLength(50);
            e.Property(x => x.Surname).HasMaxLength(100);
            e.Property(x => x.GivenName).HasMaxLength(100);
            e.Property(x => x.Department).HasMaxLength(100);
            e.Property(x => x.Position).HasMaxLength(100);
            e.Property(x => x.PhoneNumber).HasMaxLength(50);
            e.Property(x => x.Email).HasMaxLength(256);
            e.Property(x => x.PurposeOfTravel).HasMaxLength(1000);
            e.Property(x => x.OtherInformation).HasMaxLength(1000);
            e.Property(x => x.Status).HasMaxLength(30);
            e.Property(x => x.VerificationNotes).HasMaxLength(500);
            e.Property(x => x.ApprovalNotes).HasMaxLength(500);
            e.Property(x => x.RejectionReason).HasMaxLength(500);

            e.HasOne(x => x.RequestedBy).WithMany()
             .HasForeignKey(x => x.RequestedById).OnDelete(DeleteBehavior.Restrict);
            e.HasOne(x => x.VerifiedBy).WithMany()
             .HasForeignKey(x => x.VerifiedById).OnDelete(DeleteBehavior.SetNull);
            e.HasOne(x => x.ApprovedBy).WithMany()
             .HasForeignKey(x => x.ApprovedById).OnDelete(DeleteBehavior.SetNull);

            // Legs belong to the form and have no life of their own.
            e.HasMany(x => x.Legs)
             .WithOne(l => l.TravelRequest)
             .HasForeignKey(l => l.TravelRequestId)
             .OnDelete(DeleteBehavior.Cascade);
        });

        mb.Entity<Department>(e =>
        {
            e.HasKey(x => x.Id);
            e.Property(x => x.Id).HasDefaultValueSql("NEWSEQUENTIALID()");
            e.Property(x => x.CreatedAt).HasDefaultValueSql("GETUTCDATE()");
            e.HasIndex(x => x.Name).IsUnique();
            e.Property(x => x.Name).HasMaxLength(150);
            // A head can be reassigned or leave without taking the department
            // with them, so clear the link rather than blocking the delete.
            e.HasOne(x => x.Hod).WithMany()
             .HasForeignKey(x => x.HodUserId).OnDelete(DeleteBehavior.NoAction);
        });

        mb.Entity<TravelRequestLeg>(e =>
        {
            e.HasKey(x => x.Id);
            e.Property(x => x.Id).HasDefaultValueSql("NEWSEQUENTIALID()");
            e.Property(x => x.Direction).HasMaxLength(20);
            e.Property(x => x.From).HasMaxLength(150);
            e.Property(x => x.To).HasMaxLength(150);
            e.Property(x => x.PreferredAirline).HasMaxLength(100);
            e.Property(x => x.PreferredTime).HasMaxLength(50);
        });

        mb.Entity<ProjectMaterialTracking>(e =>
        {
            e.HasKey(x => x.Id);
            e.Property(x => x.Id).HasDefaultValueSql("NEWSEQUENTIALID()");
            e.Property(x => x.Quantity).HasColumnType("decimal(12,3)");
            e.Property(x => x.CreatedAt).HasDefaultValueSql("GETUTCDATE()");
            e.Property(x => x.UpdatedAt).HasDefaultValueSql("GETUTCDATE()");
            e.HasOne(x => x.CreatedBy).WithMany()
             .HasForeignKey(x => x.CreatedById).OnDelete(DeleteBehavior.Restrict);
        });

        mb.Entity<MovementRegister>(e =>
        {
            e.HasKey(x => x.Id);
            e.Property(x => x.Id).HasDefaultValueSql("NEWSEQUENTIALID()");
            e.Property(x => x.CreatedAt).HasDefaultValueSql("GETUTCDATE()");
            e.HasOne(x => x.Vehicle).WithMany()
             .HasForeignKey(x => x.VehicleId).OnDelete(DeleteBehavior.SetNull);
            e.HasOne(x => x.Driver).WithMany()
             .HasForeignKey(x => x.DriverId).OnDelete(DeleteBehavior.SetNull);
            e.HasOne(x => x.LoggedBy).WithMany()
             .HasForeignKey(x => x.LoggedById).OnDelete(DeleteBehavior.Restrict);
        });
    }
}
