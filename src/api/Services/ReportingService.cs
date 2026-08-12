using ClosedXML.Excel;
using LogisticsApi.Data;
using LogisticsApi.Models.DTOs;
using Microsoft.EntityFrameworkCore;

namespace LogisticsApi.Services;

public interface IReportingService
{
    Task<DashboardSummaryDto> GetDashboardSummaryAsync();
    Task<byte[]> ExportVehicleReportAsync(DateTime from, DateTime to);
    Task<byte[]> ExportDriverReportAsync(DateTime from, DateTime to);
    Task<byte[]> ExportFuelReportAsync(DateTime from, DateTime to);
    Task<byte[]> ExportMaintenanceReportAsync(DateTime from, DateTime to);
}

public class ReportingService(AppDbContext db) : IReportingService
{
    public async Task<DashboardSummaryDto> GetDashboardSummaryAsync()
    {
        var today = DateOnly.FromDateTime(DateTime.UtcNow);
        var todayPlus14 = today.AddDays(14);

        var driverStats = await db.Users
            .Where(u => u.Role == "Driver" && u.IsActive)
            .GroupBy(u => u.DriverStatus)
            .Select(g => new { Status = g.Key, Count = g.Count() })
            .ToDictionaryAsync(x => x.Status ?? "", x => x.Count);

        var vehicleStats = await db.Vehicles
            .GroupBy(v => v.Status)
            .Select(g => new { Status = g.Key, Count = g.Count() })
            .ToDictionaryAsync(x => x.Status, x => x.Count);

        var pendingTrips = await db.TripRequests.CountAsync(t => t.Status == "Pending");
        var activeAssignments = await db.Assignments.CountAsync(a => a.Status == "Active");
        var overdueCount = await db.MaintenanceRecords
            .CountAsync(m => m.Status != "Completed" && m.Status != "Cancelled"
                          && m.ScheduledDate < today);
        var upcomingCount = await db.MaintenanceRecords
            .CountAsync(m => m.Status == "Scheduled"
                          && m.ScheduledDate >= today
                          && m.ScheduledDate <= todayPlus14);

        // ── Material movement pipeline ────────────────────────────────────────
        var materialStats = await db.MaterialTransportRequests
            .GroupBy(m => m.Status)
            .Select(g => new { Status = g.Key, Count = g.Count() })
            .ToDictionaryAsync(x => x.Status, x => x.Count);

        // "Approved" means signed off but not yet given a driver/vehicle.
        var approvedUnassigned = await db.MaterialTransportRequests
            .CountAsync(m => m.Status == "Approved"
                          && (m.AssignedDriverId == null || m.AssignedVehicleId == null));

        var projectInTransit = await db.ProjectMaterialTrackings
            .CountAsync(p => p.DeliveryStatus == "InTransit" || p.DeliveryStatus == "Customs");

        // Past the expected arrival date and still not delivered.
        var projectOverdue = await db.ProjectMaterialTrackings
            .CountAsync(p => p.Eta != null
                          && p.Eta < today
                          && p.DeliveryStatus != "Delivered");

        return new DashboardSummaryDto(
            AvailableDrivers: driverStats.GetValueOrDefault("Available", 0),
            DriversOnAssignment: driverStats.GetValueOrDefault("OnAssignment", 0),
            DriversOffDuty: driverStats.GetValueOrDefault("OffDuty", 0),
            DriversOnBreak: driverStats.GetValueOrDefault("OnBreak", 0),
            AvailableVehicles: vehicleStats.GetValueOrDefault("Available", 0),
            VehiclesAssigned: vehicleStats.GetValueOrDefault("Assigned", 0),
            VehiclesInMaintenance: vehicleStats.GetValueOrDefault("InMaintenance", 0),
            PendingTripRequests: pendingTrips,
            ActiveAssignments: activeAssignments,
            OverdueMaintenanceCount: overdueCount,
            UpcomingMaintenanceCount: upcomingCount,
            MaterialAwaitingHod:        materialStats.GetValueOrDefault("PendingHOD", 0),
            MaterialAwaitingManager:    materialStats.GetValueOrDefault("PendingManager", 0),
            MaterialApprovedUnassigned: approvedUnassigned,
            MaterialDispatched:         materialStats.GetValueOrDefault("Assigned", 0),
            ProjectMaterialsInTransit:  projectInTransit,
            ProjectMaterialsOverdue:    projectOverdue
        );
    }

    public async Task<byte[]> ExportVehicleReportAsync(DateTime from, DateTime to)
    {
        var assignments = await db.Assignments
            .Include(a => a.Vehicle)
            .Include(a => a.Driver)
            .Include(a => a.TripRequest)
            .Where(a => a.StartTime >= from && a.StartTime <= to)
            .OrderBy(a => a.Vehicle.RegistrationNo).ThenBy(a => a.StartTime)
            .ToListAsync();

        using var wb = new XLWorkbook();
        var ws = wb.Worksheets.Add("Vehicle Assignments");
        var headers = new[] { "Vehicle Reg", "Make/Model", "Driver", "Purpose", "Pickup", "Destination", "Start", "End", "Status" };
        for (var i = 0; i < headers.Length; i++)
            ws.Cell(1, i + 1).Value = headers[i];

        var row = 2;
        foreach (var a in assignments)
        {
            ws.Cell(row, 1).Value = a.Vehicle.RegistrationNo;
            ws.Cell(row, 2).Value = $"{a.Vehicle.Make} {a.Vehicle.Model}";
            ws.Cell(row, 3).Value = a.Driver.FullName;
            ws.Cell(row, 4).Value = a.TripRequest.Purpose;
            ws.Cell(row, 5).Value = a.TripRequest.PickupLocation;
            ws.Cell(row, 6).Value = a.TripRequest.DestinationLocation;
            ws.Cell(row, 7).Value = a.StartTime.ToString("g");
            ws.Cell(row, 8).Value = a.ActualEndTime?.ToString("g") ?? "";
            ws.Cell(row, 9).Value = a.Status;
            row++;
        }

        ws.Columns().AdjustToContents();
        using var ms = new MemoryStream();
        wb.SaveAs(ms);
        return ms.ToArray();
    }

    public async Task<byte[]> ExportDriverReportAsync(DateTime from, DateTime to)
    {
        var drivers = await db.Users
            .Where(u => u.Role == "Driver" && u.IsActive)
            .Select(u => new
            {
                u.FullName,
                u.Email,
                u.DriverStatus,
                AssignmentCount = u.AssignmentsAsDriver
                    .Count(a => a.StartTime >= from && a.StartTime <= to && a.Status != "Cancelled")
            })
            .OrderBy(u => u.FullName)
            .ToListAsync();

        using var wb = new XLWorkbook();
        var ws = wb.Worksheets.Add("Driver Report");
        ws.Cell(1, 1).Value = "Driver";
        ws.Cell(1, 2).Value = "Email";
        ws.Cell(1, 3).Value = "Current Status";
        ws.Cell(1, 4).Value = "Assignments in Period";

        var row = 2;
        foreach (var d in drivers)
        {
            ws.Cell(row, 1).Value = d.FullName;
            ws.Cell(row, 2).Value = d.Email;
            ws.Cell(row, 3).Value = d.DriverStatus ?? "N/A";
            ws.Cell(row, 4).Value = d.AssignmentCount;
            row++;
        }

        ws.Columns().AdjustToContents();
        using var ms = new MemoryStream();
        wb.SaveAs(ms);
        return ms.ToArray();
    }

    public async Task<byte[]> ExportFuelReportAsync(DateTime from, DateTime to)
    {
        var logs = await db.FuelLogs
            .Include(f => f.Vehicle)
            .Include(f => f.LoggedBy)
            .Where(f => f.FuelDate >= DateOnly.FromDateTime(from) && f.FuelDate <= DateOnly.FromDateTime(to))
            .OrderBy(f => f.Vehicle.RegistrationNo).ThenBy(f => f.FuelDate)
            .ToListAsync();

        using var wb = new XLWorkbook();
        var ws = wb.Worksheets.Add("Fuel Report");
        var headers = new[] { "Vehicle Reg", "Date", "Station", "Litres", "Cost/Litre", "Total Cost", "Odometer", "Logged By" };
        for (var i = 0; i < headers.Length; i++)
            ws.Cell(1, i + 1).Value = headers[i];

        var row = 2;
        foreach (var f in logs)
        {
            ws.Cell(row, 1).Value = f.Vehicle.RegistrationNo;
            ws.Cell(row, 2).Value = f.FuelDate.ToString("d");
            ws.Cell(row, 3).Value = f.StationName ?? "";
            ws.Cell(row, 4).Value = (double)f.LitresFilled;
            ws.Cell(row, 5).Value = (double)f.CostPerLitre;
            ws.Cell(row, 6).Value = (double)f.TotalCost;
            ws.Cell(row, 7).Value = f.OdometerAtFill;
            ws.Cell(row, 8).Value = f.LoggedBy.FullName;
            row++;
        }

        ws.Columns().AdjustToContents();
        using var ms = new MemoryStream();
        wb.SaveAs(ms);
        return ms.ToArray();
    }

    public async Task<byte[]> ExportMaintenanceReportAsync(DateTime from, DateTime to)
    {
        var records = await db.MaintenanceRecords
            .Include(m => m.Vehicle)
            .Where(m => m.ScheduledDate >= DateOnly.FromDateTime(from) && m.ScheduledDate <= DateOnly.FromDateTime(to))
            .OrderBy(m => m.Vehicle.RegistrationNo).ThenBy(m => m.ScheduledDate)
            .ToListAsync();

        using var wb = new XLWorkbook();
        var ws = wb.Worksheets.Add("Maintenance Report");
        var headers = new[] { "Vehicle Reg", "Type", "Scheduled", "Completed", "Status", "Vendor", "Cost", "Notes" };
        for (var i = 0; i < headers.Length; i++)
            ws.Cell(1, i + 1).Value = headers[i];

        var row = 2;
        foreach (var m in records)
        {
            ws.Cell(row, 1).Value = m.Vehicle.RegistrationNo;
            ws.Cell(row, 2).Value = m.Type;
            ws.Cell(row, 3).Value = m.ScheduledDate.ToString("d");
            ws.Cell(row, 4).Value = m.CompletedDate?.ToString("d") ?? "";
            ws.Cell(row, 5).Value = m.Status;
            ws.Cell(row, 6).Value = m.VendorName ?? "";
            ws.Cell(row, 7).Value = m.Cost.HasValue ? (double)m.Cost.Value : 0;
            ws.Cell(row, 8).Value = m.Notes ?? "";
            row++;
        }

        ws.Columns().AdjustToContents();
        using var ms = new MemoryStream();
        wb.SaveAs(ms);
        return ms.ToArray();
    }
}
