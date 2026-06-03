using LogisticsApi.Models.Entities;
using Microsoft.EntityFrameworkCore;

namespace LogisticsApi.Data;

/// <summary>
/// Runs on every startup to ensure reference data exists.
/// Safe to call multiple times — all operations are idempotent.
/// </summary>
public static class DatabaseSeeder
{
    public static async Task SeedAsync(AppDbContext db, IConfiguration config, ILogger logger)
    {
        // ── 1. Always seed Locations if missing ──────────────────────────────
        if (!await db.Locations.AnyAsync())
        {
            logger.LogInformation("Seeding operational locations...");
            db.Locations.AddRange(
                new Location { Id = Guid.Parse("C0000001-0000-0000-0000-000000000001"), Name = "Desicon Lagos Office", Code = "LOS",   IsActive = true, CreatedAt = DateTime.UtcNow },
                new Location { Id = Guid.Parse("C0000001-0000-0000-0000-000000000002"), Name = "Desicon PH Office",    Code = "PH",    IsActive = true, CreatedAt = DateTime.UtcNow },
                new Location { Id = Guid.Parse("C0000001-0000-0000-0000-000000000003"), Name = "Desicon Abuja Office", Code = "ABJ",   IsActive = true, CreatedAt = DateTime.UtcNow },
                new Location { Id = Guid.Parse("C0000001-0000-0000-0000-000000000004"), Name = "Site Bonny",           Code = "BONNY", IsActive = true, CreatedAt = DateTime.UtcNow },
                new Location { Id = Guid.Parse("C0000001-0000-0000-0000-000000000005"), Name = "Others",               Code = "OTH",   IsActive = true, CreatedAt = DateTime.UtcNow }
            );
            await db.SaveChangesAsync();
            logger.LogInformation("Locations seeded: LOS, PH, ABJ, BONNY, OTH");
        }

        // ── 2. Seed demo data if flag is set and DB is empty ─────────────────
        var seedDemo = config.GetValue<bool>("Demo:SeedOnStartup");
        if (!seedDemo || await db.Users.AnyAsync()) return;

        logger.LogInformation("Seeding demo data (Demo:SeedOnStartup = true)...");

        // ── Demo Users ────────────────────────────────────────────────────────
        var users = new[]
        {
            // Drivers
            MakeDriver("A1000001-0000-0000-0000-000000000001", "00000000-0000-0000-0000-000000000001", "James Mokoena",    "j.mokoena@company.com",   "+27 82 111 0001", "Available",    "GP-DL-001", new DateOnly(2027,6,30),  -180),
            MakeDriver("A1000001-0000-0000-0000-000000000002", "00000000-0000-0000-0000-000000000002", "Sipho Dlamini",    "s.dlamini@company.com",   "+27 83 111 0002", "OnAssignment", "GP-DL-002", new DateOnly(2026,12,31), -210),
            MakeDriver("A1000001-0000-0000-0000-000000000003", "00000000-0000-0000-0000-000000000003", "Thabo Nkosi",      "t.nkosi@company.com",     "+27 84 111 0003", "Available",    "GP-DL-003", new DateOnly(2028,3,31),  -90),
            MakeDriver("A1000001-0000-0000-0000-000000000004", "00000000-0000-0000-0000-000000000004", "Nomsa Zulu",       "n.zulu@company.com",      "+27 71 111 0004", "OnBreak",      "GP-DL-004", new DateOnly(2027,9,30),  -150),
            MakeDriver("A1000001-0000-0000-0000-000000000005", "00000000-0000-0000-0000-000000000005", "Bongani Khumalo",  "b.khumalo@company.com",   "+27 73 111 0005", "OnAssignment", "GP-DL-005", new DateOnly(2026,8,31),  -300),
            MakeDriver("A1000001-0000-0000-0000-000000000006", "00000000-0000-0000-0000-000000000006", "Lungelo Mthembu",  "l.mthembu@company.com",   "+27 79 111 0006", "OffDuty",      "GP-DL-006", new DateOnly(2027,11,30), -60),
            MakeDriver("A1000001-0000-0000-0000-000000000007", "00000000-0000-0000-0000-000000000007", "Ayanda Cele",      "a.cele@company.com",      "+27 82 111 0007", "Available",    "GP-DL-007", new DateOnly(2025,10,31), -45),
            MakeDriver("A1000001-0000-0000-0000-000000000008", "00000000-0000-0000-0000-000000000008", "Zanele Sithole",   "z.sithole@company.com",   "+27 83 111 0008", "OnAssignment", "GP-DL-008", new DateOnly(2028,6,30),  -120),
            MakeDriver("A1000001-0000-0000-0000-000000000009", "00000000-0000-0000-0000-000000000009", "Mandla Shabalala", "m.shabalala@company.com", "+27 71 111 0009", "Available",    "GP-DL-009", new DateOnly(2027,4,30),  -30),
            MakeDriver("A1000001-0000-0000-0000-000000000010", "00000000-0000-0000-0000-000000000010", "Precious Ndlovu",  "p.ndlovu@company.com",    "+27 84 111 0010", "OffDuty",      "GP-DL-010", new DateOnly(2026,5,31),  -200),
            // Staff
            MakeStaff("A1000001-0000-0000-0000-000000000020", "00000000-0000-0000-0000-000000000020", "Coordinator",  "coordinator@company.com",  "Coordinator"),
            MakeStaff("A1000001-0000-0000-0000-000000000030", "00000000-0000-0000-0000-000000000030", "Manager",      "manager@company.com",      "Manager"),
            MakeStaff("A1000001-0000-0000-0000-000000000040", "00000000-0000-0000-0000-000000000040", "Admin User",   "admin@company.com",        "Admin"),
            MakeStaff("A1000001-0000-0000-0000-000000000050", "00000000-0000-0000-0000-000000000050", "Fleet Mechanic","mechanic@company.com",     "Mechanic"),
        };

        db.Users.AddRange(users);
        await db.SaveChangesAsync();

        // ── Demo Vehicles ─────────────────────────────────────────────────────
        var vehicles = new[]
        {
            MakeVehicle("B2000001-0000-0000-0000-000000000001", "GP 12-34 AA", "Toyota",    "Land Cruiser 200", 2021, "Diesel", 45200,  "Available"),
            MakeVehicle("B2000001-0000-0000-0000-000000000002", "GP 77-88 FF", "Toyota",    "Hilux GD6",        2022, "Diesel", 28750,  "Assigned"),
            MakeVehicle("B2000001-0000-0000-0000-000000000003", "GP 45-67 DD", "Toyota",    "Land Cruiser 79",  2020, "Diesel", 67300,  "Assigned"),
            MakeVehicle("B2000001-0000-0000-0000-000000000004", "GP 23-45 BB", "Ford",      "Ranger 3.2 4x4",   2021, "Diesel", 39100,  "Available"),
            MakeVehicle("B2000001-0000-0000-0000-000000000005", "GP 34-56 CC", "Toyota",    "Fortuner 2.8 GD6", 2022, "Diesel", 22400,  "InMaintenance"),
            MakeVehicle("B2000001-0000-0000-0000-000000000006", "GP 56-78 EE", "Mitsubishi","Pajero Sport",     2019, "Diesel", 88600,  "Available"),
            MakeVehicle("B2000001-0000-0000-0000-000000000007", "GP 67-89 GG", "Nissan",   "Navara NP300",     2020, "Diesel", 51200,  "Available"),
            MakeVehicle("B2000001-0000-0000-0000-000000000008", "GP 89-01 HH", "Toyota",    "Coaster Bus",      2018, "Diesel", 134000, "Available"),
        };

        db.Vehicles.AddRange(vehicles);
        await db.SaveChangesAsync();

        // ── Demo Trip Requests ────────────────────────────────────────────────
        var trips = new[]
        {
            MakeTrip("D3000001-0000-0000-0000-000000000001", "A1000001-0000-0000-0000-000000000020", "Staff transfer — OR Tambo airport",   "Head Office", "OR Tambo Airport",    "Approved",  -1),
            MakeTrip("D3000001-0000-0000-0000-000000000002", "A1000001-0000-0000-0000-000000000020", "Site inspection — Midrand warehouse",  "Head Office", "Midrand Warehouse",   "Approved",  -1),
            MakeTrip("D3000001-0000-0000-0000-000000000003", "A1000001-0000-0000-0000-000000000020", "Equipment delivery — Soweto depot",    "Head Office", "Soweto Depot",        "Approved",  -1),
            MakeTrip("D3000001-0000-0000-0000-000000000004", "A1000001-0000-0000-0000-000000000020", "Client meeting — Sandton CBD",         "Head Office", "Sandton CBD",         "Pending",    0),
            MakeTrip("D3000001-0000-0000-0000-000000000005", "A1000001-0000-0000-0000-000000000020", "Material collection — Centurion store","Head Office", "Centurion Store",     "Pending",    0),
        };

        db.TripRequests.AddRange(trips);
        await db.SaveChangesAsync();

        // ── Demo Assignments ──────────────────────────────────────────────────
        db.Assignments.AddRange(
            MakeAssignment("E4000001-0000-0000-0000-000000000001", "D3000001-0000-0000-0000-000000000001",
                "A1000001-0000-0000-0000-000000000005", "B2000001-0000-0000-0000-000000000002",
                "A1000001-0000-0000-0000-000000000020", "Active", -1, 4),
            MakeAssignment("E4000001-0000-0000-0000-000000000002", "D3000001-0000-0000-0000-000000000002",
                "A1000001-0000-0000-0000-000000000002", "B2000001-0000-0000-0000-000000000003",
                "A1000001-0000-0000-0000-000000000020", "Active", -1, 4),
            MakeAssignment("E4000001-0000-0000-0000-000000000003", "D3000001-0000-0000-0000-000000000003",
                "A1000001-0000-0000-0000-000000000008", "B2000001-0000-0000-0000-000000000001",
                "A1000001-0000-0000-0000-000000000020", "Active", -1, 4)
        );
        await db.SaveChangesAsync();

        // ── Demo Maintenance Records ──────────────────────────────────────────
        db.MaintenanceRecords.AddRange(
            MakeMaintenance("F5000001-0000-0000-0000-000000000001", "B2000001-0000-0000-0000-000000000005", "Oil Change",       "Routine",     "Scheduled", -5),
            MakeMaintenance("F5000001-0000-0000-0000-000000000002", "B2000001-0000-0000-0000-000000000006", "Tyre Replacement", "Routine",     "Completed", -30),
            MakeMaintenance("F5000001-0000-0000-0000-000000000003", "B2000001-0000-0000-0000-000000000008", "Brake Service",    "FaultRepair", "InProgress", -2),
            MakeMaintenance("F5000001-0000-0000-0000-000000000004", "B2000001-0000-0000-0000-000000000007", "Inspection",       "Routine",     "Scheduled", 7)
        );
        await db.SaveChangesAsync();

        logger.LogInformation("Demo data seeded successfully: {drivers} drivers, {vehicles} vehicles, {trips} trips.",
            10, 8, trips.Length);
    }

    // ── Helper factories ──────────────────────────────────────────────────────

    private static User MakeDriver(string id, string entraId, string name, string email,
        string phone, string status, string licenceNo, DateOnly expiry, int daysAgo) => new()
    {
        Id = Guid.Parse(id),
        EntraObjectId = entraId,
        FullName = name,
        Email = email,
        PhoneNumber = phone,
        Role = "Driver",
        DriverStatus = status,
        LicenceNo = licenceNo,
        LicenceExpiry = expiry,
        IsActive = true,
        LastStatusChange = DateTime.UtcNow.AddHours(-2),
        CreatedAt = DateTime.UtcNow.AddDays(daysAgo),
    };

    private static User MakeStaff(string id, string entraId, string name, string email, string role) => new()
    {
        Id = Guid.Parse(id),
        EntraObjectId = entraId,
        FullName = name,
        Email = email,
        Role = role,
        IsActive = true,
        CreatedAt = DateTime.UtcNow.AddDays(-365),
    };

    private static Vehicle MakeVehicle(string id, string reg, string make, string model,
        int year, string fuel, int odometer, string status) => new()
    {
        Id = Guid.Parse(id),
        RegistrationNo = reg,
        Make = make,
        Model = model,
        Year = (short)year,
        FuelType = fuel,
        OdometerKm = odometer,
        ServiceIntervalKm = 10000,
        Status = status,
        LastServiceDate = DateOnly.FromDateTime(DateTime.UtcNow.AddDays(-90)),
        NextServiceDate = DateOnly.FromDateTime(DateTime.UtcNow.AddDays(60)),
        CreatedAt = DateTime.UtcNow.AddDays(-400),
        UpdatedAt = DateTime.UtcNow.AddDays(-90),
    };

    private static TripRequest MakeTrip(string id, string requestedById, string purpose,
        string pickup, string destination, string status, int daysAgo) => new()
    {
        Id = Guid.Parse(id),
        RequestedById = Guid.Parse(requestedById),
        Purpose = purpose,
        PickupLocation = pickup,
        DestinationLocation = destination,
        RequestedDateTime = DateTime.UtcNow.AddDays(daysAgo).AddHours(9),
        Status = status,
        Priority = "Normal",
        MovementType = "IntraState",
        CreatedAt = DateTime.UtcNow.AddDays(daysAgo),
    };

    private static Assignment MakeAssignment(string id, string tripId, string driverId,
        string vehicleId, string assignedById, string status, int startDaysAgo, int durationHours) => new()
    {
        Id = Guid.Parse(id),
        TripRequestId = Guid.Parse(tripId),
        DriverId = Guid.Parse(driverId),
        VehicleId = Guid.Parse(vehicleId),
        AssignedById = Guid.Parse(assignedById),
        AssignmentType = "Auto",
        Status = status,
        StartTime = DateTime.UtcNow.AddDays(startDaysAgo).AddHours(9),
        EstimatedEndTime = DateTime.UtcNow.AddDays(startDaysAgo).AddHours(9 + durationHours),
        CreatedAt = DateTime.UtcNow.AddDays(startDaysAgo),
    };

    private static MaintenanceRecord MakeMaintenance(string id, string vehicleId, string type,
        string category, string status, int scheduledDaysFromNow) => new()
    {
        Id = Guid.Parse(id),
        VehicleId = Guid.Parse(vehicleId),
        Type = type,
        Category = category,
        ScheduledDate = DateOnly.FromDateTime(DateTime.UtcNow.AddDays(scheduledDaysFromNow)),
        Status = status,
        FaultReported = category == "FaultRepair",
        CreatedAt = DateTime.UtcNow.AddDays(-1),
        UpdatedAt = DateTime.UtcNow.AddDays(-1),
    };
}
