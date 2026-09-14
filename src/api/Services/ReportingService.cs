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
    Task<byte[]> ExportFuelReportAsync(
        DateTime from, DateTime to,
        Guid? vehicleId = null, Guid? locationId = null, string? productType = null);
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

    /// <summary>
    /// Fuel workbook for accounts reconciliation, requested by the HOD Logistics.
    ///
    /// Three sheets: the transaction detail, a breakdown by payment method (what
    /// accounts settles against), and a breakdown by vehicle (what cost control
    /// looks at). Dates go in as real dates and money as real numbers with a
    /// Naira format, so the finance team can filter, pivot and SUM without
    /// cleaning the file first — which is the whole point of sending .xlsx
    /// rather than CSV.
    /// </summary>
    public async Task<byte[]> ExportFuelReportAsync(
        DateTime from, DateTime to,
        Guid? vehicleId = null, Guid? locationId = null, string? productType = null)
    {
        var fromDate = DateOnly.FromDateTime(from);
        var toDate   = DateOnly.FromDateTime(to);

        // Mirrors the filters on the Fuel Logs page so the export matches what
        // the user is looking at when they click Export.
        var q = db.FuelLogs
            .Include(f => f.Vehicle)
            .Include(f => f.LoggedBy)
            .Include(f => f.Location)
            .Where(f => f.FuelDate >= fromDate && f.FuelDate <= toDate);

        if (vehicleId.HasValue)  q = q.Where(f => f.VehicleId == vehicleId.Value);
        if (locationId.HasValue) q = q.Where(f => f.LocationId == locationId.Value);
        if (!string.IsNullOrWhiteSpace(productType)) q = q.Where(f => f.ProductType == productType);

        var logs = await q
            .OrderBy(f => f.FuelDate)
            .ThenBy(f => f.Vehicle.RegistrationNo)
            .ToListAsync();

        const string money = "#,##0.00";
        using var wb = new XLWorkbook();

        // ── Sheet 1: transaction detail ──────────────────────────────────────
        var ws = wb.Worksheets.Add("Fuel Transactions");

        ws.Cell(1, 1).Value = "Desicon Engineering — Fuel Log";
        ws.Cell(1, 1).Style.Font.Bold = true;
        ws.Cell(1, 1).Style.Font.FontSize = 14;
        ws.Cell(2, 1).Value = $"Period: {fromDate:dd MMM yyyy} to {toDate:dd MMM yyyy}";
        ws.Cell(3, 1).Value = $"Generated: {DateTime.UtcNow:dd MMM yyyy HH:mm} UTC · {logs.Count} entries";
        ws.Range(2, 1, 3, 1).Style.Font.FontColor = XLColor.Gray;

        var headers = new[]
        {
            "Date", "Vehicle Reg", "Make / Model", "Location", "Cost Centre",
            "Product", "Station", "Payment Method", "Litres", "Rate (NGN)",
            "Total (NGN)", "Mileage Before (km)", "Mileage After (km)",
            "KM Covered", "Gauge Before", "Gauge After", "Logged By", "Notes"
        };

        const int headerRow = 5;
        for (var i = 0; i < headers.Length; i++)
        {
            var cell = ws.Cell(headerRow, i + 1);
            cell.Value = headers[i];
            cell.Style.Font.Bold = true;
            cell.Style.Fill.BackgroundColor = XLColor.FromHtml("#1F3864");
            cell.Style.Font.FontColor = XLColor.White;
        }

        var row = headerRow + 1;
        foreach (var f in logs)
        {
            var method = string.IsNullOrWhiteSpace(f.PaymentMethod)
                ? (f.IsCashPayment ? "Cash" : "Card")
                : f.PaymentMethod;

            ws.Cell(row, 1).Value = f.FuelDate.ToDateTime(TimeOnly.MinValue);
            ws.Cell(row, 1).Style.DateFormat.Format = "dd/mm/yyyy";
            ws.Cell(row, 2).Value = f.Vehicle.RegistrationNo;
            ws.Cell(row, 3).Value = $"{f.Vehicle.Make} {f.Vehicle.Model}".Trim();
            ws.Cell(row, 4).Value = f.Location?.Name ?? "";
            ws.Cell(row, 5).Value = f.CostCentre ?? "";
            ws.Cell(row, 6).Value = f.ProductType ?? "";
            ws.Cell(row, 7).Value = f.StationName ?? "";
            ws.Cell(row, 8).Value = method;
            ws.Cell(row, 9).Value = (double)f.LitresFilled;
            ws.Cell(row, 10).Value = (double)f.CostPerLitre;
            ws.Cell(row, 11).Value = (double)f.TotalCost;
            ws.Cell(row, 12).Value = f.OdometerAtFill;
            if (f.OdometerAfterFill.HasValue) ws.Cell(row, 13).Value = f.OdometerAfterFill.Value;
            if (f.MileageCovered.HasValue)    ws.Cell(row, 14).Value = f.MileageCovered.Value;
            ws.Cell(row, 15).Value = f.FuelGaugeBeforePosition ?? "";
            ws.Cell(row, 16).Value = f.FuelGaugeAfterPosition ?? "";
            ws.Cell(row, 17).Value = f.LoggedBy?.FullName ?? "";
            ws.Cell(row, 18).Value = f.Notes ?? "";

            ws.Cell(row, 9).Style.NumberFormat.Format  = "#,##0.00";
            ws.Cell(row, 10).Style.NumberFormat.Format = money;
            ws.Cell(row, 11).Style.NumberFormat.Format = money;
            row++;
        }

        // Totals row — live SUM formulas, so it still adds up if accounts filter
        // or delete rows in their own copy.
        if (logs.Count > 0)
        {
            var first = headerRow + 1;
            var last  = row - 1;
            ws.Cell(row, 8).Value = "TOTAL";
            ws.Cell(row, 9).FormulaA1  = $"SUM(I{first}:I{last})";   // litres
            ws.Cell(row, 11).FormulaA1 = $"SUM(K{first}:K{last})";   // cost
            ws.Cell(row, 14).FormulaA1 = $"SUM(N{first}:N{last})";   // km covered
            ws.Cell(row, 9).Style.NumberFormat.Format  = "#,##0.00";
            ws.Cell(row, 11).Style.NumberFormat.Format = money;
            ws.Cell(row, 14).Style.NumberFormat.Format = "#,##0";
            ws.Range(row, 1, row, headers.Length).Style.Font.Bold = true;
            ws.Range(row, 1, row, headers.Length).Style.Fill.BackgroundColor = XLColor.FromHtml("#DDEBF7");

            // Filter dropdowns and a frozen header make a long log workable.
            ws.Range(headerRow, 1, last, headers.Length).SetAutoFilter();
            ws.SheetView.FreezeRows(headerRow);
        }

        ws.Columns().AdjustToContents();

        // ── Sheet 2: by payment method — what accounts reconciles against ────
        var byMethod = logs
            .GroupBy(f => string.IsNullOrWhiteSpace(f.PaymentMethod)
                ? (f.IsCashPayment ? "Cash" : "Card")
                : f.PaymentMethod)
            .OrderBy(g => g.Key)
            .ToList();

        var ws2 = wb.Worksheets.Add("By Payment Method");
        WriteSummarySheet(ws2, "Payment Method",
            byMethod.Select(g => (Key: g.Key,
                                  Count: g.Count(),
                                  Litres: g.Sum(x => (double)x.LitresFilled),
                                  Total: g.Sum(x => (double)x.TotalCost))),
            money);

        // ── Sheet 3: by vehicle — what cost control reviews ──────────────────
        var byVehicle = logs
            .GroupBy(f => f.Vehicle.RegistrationNo)
            .OrderBy(g => g.Key)
            .ToList();

        var ws3 = wb.Worksheets.Add("By Vehicle");
        WriteSummarySheet(ws3, "Vehicle",
            byVehicle.Select(g => (Key: g.Key,
                                   Count: g.Count(),
                                   Litres: g.Sum(x => (double)x.LitresFilled),
                                   Total: g.Sum(x => (double)x.TotalCost))),
            money);

        using var stream = new MemoryStream();
        wb.SaveAs(stream);
        return stream.ToArray();
    }

    /// <summary>
    /// Writes a "group / entries / litres / total" breakdown with a totals row.
    /// Shared by the payment-method and vehicle sheets so both stay identical
    /// in layout and formatting.
    /// </summary>
    private static void WriteSummarySheet(
        IXLWorksheet ws,
        string groupLabel,
        IEnumerable<(string Key, int Count, double Litres, double Total)> rows,
        string moneyFormat)
    {
        var headers = new[] { groupLabel, "Entries", "Litres", "Total (NGN)" };
        for (var i = 0; i < headers.Length; i++)
        {
            var cell = ws.Cell(1, i + 1);
            cell.Value = headers[i];
            cell.Style.Font.Bold = true;
            cell.Style.Fill.BackgroundColor = XLColor.FromHtml("#1F3864");
            cell.Style.Font.FontColor = XLColor.White;
        }

        var row = 2;
        foreach (var r in rows)
        {
            ws.Cell(row, 1).Value = r.Key;
            ws.Cell(row, 2).Value = r.Count;
            ws.Cell(row, 3).Value = r.Litres;
            ws.Cell(row, 4).Value = r.Total;
            ws.Cell(row, 3).Style.NumberFormat.Format = "#,##0.00";
            ws.Cell(row, 4).Style.NumberFormat.Format = moneyFormat;
            row++;
        }

        if (row > 2)
        {
            ws.Cell(row, 1).Value = "TOTAL";
            ws.Cell(row, 2).FormulaA1 = $"SUM(B2:B{row - 1})";
            ws.Cell(row, 3).FormulaA1 = $"SUM(C2:C{row - 1})";
            ws.Cell(row, 4).FormulaA1 = $"SUM(D2:D{row - 1})";
            ws.Cell(row, 3).Style.NumberFormat.Format = "#,##0.00";
            ws.Cell(row, 4).Style.NumberFormat.Format = moneyFormat;
            ws.Range(row, 1, row, 4).Style.Font.Bold = true;
            ws.Range(row, 1, row, 4).Style.Fill.BackgroundColor = XLColor.FromHtml("#DDEBF7");
        }

        ws.Columns().AdjustToContents();
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
        var headers = new[] { "Vehicle Reg", "Type", "Date Reported", "Completed", "Date Returned", "Status", "Vendor", "Cost", "Notes" };
        for (var i = 0; i < headers.Length; i++)
            ws.Cell(1, i + 1).Value = headers[i];

        var row = 2;
        foreach (var m in records)
        {
            ws.Cell(row, 1).Value = m.Vehicle.RegistrationNo;
            ws.Cell(row, 2).Value = m.Type;
            ws.Cell(row, 3).Value = m.ScheduledDate.ToString("d");
            ws.Cell(row, 4).Value = m.CompletedDate?.ToString("d") ?? "";
            ws.Cell(row, 5).Value = m.DateReturned?.ToString("d") ?? "";
            ws.Cell(row, 6).Value = m.Status;
            ws.Cell(row, 7).Value = m.VendorName ?? "";
            ws.Cell(row, 8).Value = m.Cost.HasValue ? (double)m.Cost.Value : 0;
            ws.Cell(row, 9).Value = m.Notes ?? "";
            row++;
        }

        ws.Columns().AdjustToContents();
        using var ms = new MemoryStream();
        wb.SaveAs(ms);
        return ms.ToArray();
    }
}
