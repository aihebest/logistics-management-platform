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
            e.HasOne(x => x.RequestedBy).WithMany()
             .HasForeignKey(x => x.RequestedById).OnDelete(DeleteBehavior.Restrict);
            e.HasOne(x => x.ApprovedBy).WithMany()
             .HasForeignKey(x => x.ApprovedById).OnDelete(DeleteBehavior.SetNull);
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
