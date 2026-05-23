using LogisticsApi.Models.Entities;
using Microsoft.EntityFrameworkCore;

namespace LogisticsApi.Data;

public class AppDbContext(DbContextOptions<AppDbContext> options) : DbContext(options)
{
    public DbSet<User> Users => Set<User>();
    public DbSet<Vehicle> Vehicles => Set<Vehicle>();
    public DbSet<TripRequest> TripRequests => Set<TripRequest>();
    public DbSet<Assignment> Assignments => Set<Assignment>();
    public DbSet<MaintenanceRecord> MaintenanceRecords => Set<MaintenanceRecord>();
    public DbSet<FuelLog> FuelLogs => Set<FuelLog>();
    public DbSet<AuditLog> AuditLogs => Set<AuditLog>();
    public DbSet<Notification> Notifications => Set<Notification>();

    protected override void OnModelCreating(ModelBuilder mb)
    {
        // ── Users ────────────────────────────────────────────────────────────
        mb.Entity<User>(e =>
        {
            e.HasKey(x => x.Id);
            e.Property(x => x.Id).HasDefaultValueSql("NEWSEQUENTIALID()");
            e.HasIndex(x => x.EntraObjectId).IsUnique();
            e.HasIndex(x => x.Email).IsUnique();
            e.Property(x => x.CreatedAt).HasDefaultValueSql("GETUTCDATE()");
        });

        // ── Vehicles ─────────────────────────────────────────────────────────
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

        // ── TripRequests ──────────────────────────────────────────────────────
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

        // ── Assignments ───────────────────────────────────────────────────────
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

        // ── MaintenanceRecords ────────────────────────────────────────────────
        mb.Entity<MaintenanceRecord>(e =>
        {
            e.HasKey(x => x.Id);
            e.Property(x => x.Id).HasDefaultValueSql("NEWSEQUENTIALID()");
            e.Property(x => x.CreatedAt).HasDefaultValueSql("GETUTCDATE()");
            e.Property(x => x.UpdatedAt).HasDefaultValueSql("GETUTCDATE()");
            e.Property(x => x.Cost).HasColumnType("decimal(10,2)");
            e.HasOne(x => x.Vehicle)
             .WithMany(v => v.MaintenanceRecords)
             .HasForeignKey(x => x.VehicleId)
             .OnDelete(DeleteBehavior.Cascade);
        });

        // ── FuelLogs ──────────────────────────────────────────────────────────
        mb.Entity<FuelLog>(e =>
        {
            e.HasKey(x => x.Id);
            e.Property(x => x.Id).HasDefaultValueSql("NEWSEQUENTIALID()");
            e.Property(x => x.CreatedAt).HasDefaultValueSql("GETUTCDATE()");
            e.Property(x => x.LitresFilled).HasColumnType("decimal(8,2)");
            e.Property(x => x.CostPerLitre).HasColumnType("decimal(8,4)");
            e.Property(x => x.TotalCost).HasColumnType("decimal(10,2)");
            e.HasOne(x => x.Vehicle)
             .WithMany(v => v.FuelLogs)
             .HasForeignKey(x => x.VehicleId)
             .OnDelete(DeleteBehavior.Cascade);
            e.HasOne(x => x.LoggedBy)
             .WithMany(u => u.FuelLogs)
             .HasForeignKey(x => x.LoggedById)
             .OnDelete(DeleteBehavior.Restrict);
        });

        // ── AuditLogs ─────────────────────────────────────────────────────────
        mb.Entity<AuditLog>(e =>
        {
            e.HasKey(x => x.Id);
            e.Property(x => x.Id).HasDefaultValueSql("NEWSEQUENTIALID()");
            e.Property(x => x.Timestamp).HasDefaultValueSql("GETUTCDATE()");
            // No FK constraints — denormalised for immutability
        });

        // ── Notifications ─────────────────────────────────────────────────────
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
    }
}
