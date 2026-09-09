using LogisticsApi.Models.DTOs;
using LogisticsApi.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace LogisticsApi.Controllers;

[ApiController]
[Route("api/reports")]
[Authorize(Roles = "Manager,Admin")]
public class ReportsController(IReportingService reporting) : ControllerBase
{
    [HttpGet("dashboard")]
    [AllowAnonymous] // Auth still required via [Authorize] on class, but allow all roles
    [Authorize]
    public async Task<DashboardSummaryDto> GetDashboard()
        => await reporting.GetDashboardSummaryAsync();

    [HttpGet("vehicles/export")]
    public async Task<IActionResult> ExportVehicles([FromQuery] DateTime? from, [FromQuery] DateTime? to)
    {
        var bytes = await reporting.ExportVehicleReportAsync(
            from ?? DateTime.UtcNow.AddMonths(-1), to ?? DateTime.UtcNow);
        return File(bytes, "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet",
            $"vehicle-report-{DateTime.UtcNow:yyyyMMdd}.xlsx");
    }

    [HttpGet("drivers/export")]
    public async Task<IActionResult> ExportDrivers([FromQuery] DateTime? from, [FromQuery] DateTime? to)
    {
        var bytes = await reporting.ExportDriverReportAsync(
            from ?? DateTime.UtcNow.AddMonths(-1), to ?? DateTime.UtcNow);
        return File(bytes, "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet",
            $"driver-report-{DateTime.UtcNow:yyyyMMdd}.xlsx");
    }

    /// <summary>
    /// Fuel workbook for accounts reconciliation. Accepts the same filters as the
    /// Fuel Logs page so the file matches whatever the user has on screen.
    /// </summary>
    [HttpGet("fuel/export")]
    public async Task<IActionResult> ExportFuel(
        [FromQuery] DateTime? from,
        [FromQuery] DateTime? to,
        [FromQuery] Guid? vehicleId,
        [FromQuery] Guid? locationId,
        [FromQuery] string? productType)
    {
        // Default to the last 12 months — reconciliation usually reaches further
        // back than the one month the other exports assume.
        var fromDate = from ?? DateTime.UtcNow.AddMonths(-12);
        var toDate   = to ?? DateTime.UtcNow;

        var bytes = await reporting.ExportFuelReportAsync(
            fromDate, toDate, vehicleId, locationId, productType);

        return File(bytes, "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet",
            $"fuel-log_{fromDate:yyyyMMdd}_to_{toDate:yyyyMMdd}.xlsx");
    }

    [HttpGet("maintenance/export")]
    public async Task<IActionResult> ExportMaintenance([FromQuery] DateTime? from, [FromQuery] DateTime? to)
    {
        var bytes = await reporting.ExportMaintenanceReportAsync(
            from ?? DateTime.UtcNow.AddMonths(-1), to ?? DateTime.UtcNow);
        return File(bytes, "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet",
            $"maintenance-report-{DateTime.UtcNow:yyyyMMdd}.xlsx");
    }
}
