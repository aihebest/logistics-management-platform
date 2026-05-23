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

    [HttpGet("fuel/export")]
    public async Task<IActionResult> ExportFuel([FromQuery] DateTime? from, [FromQuery] DateTime? to)
    {
        var bytes = await reporting.ExportFuelReportAsync(
            from ?? DateTime.UtcNow.AddMonths(-1), to ?? DateTime.UtcNow);
        return File(bytes, "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet",
            $"fuel-report-{DateTime.UtcNow:yyyyMMdd}.xlsx");
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
