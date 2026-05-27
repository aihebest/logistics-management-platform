using System.Security.Claims;
using LogisticsApi.Data;
using LogisticsApi.Models.DTOs;
using LogisticsApi.Models.Entities;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace LogisticsApi.Controllers;

[ApiController]
[Route("api/travel")]
[Authorize]
public class TravelRequestController(AppDbContext db) : ControllerBase
{
    [HttpGet]
    public async Task<IEnumerable<TravelRequestDto>> GetAll(
        [FromQuery] string? status,
        [FromQuery] string? travelType)
    {
        var q = db.TravelRequests
            .Include(r => r.RequestedBy)
            .Include(r => r.ApprovedBy)
            .AsQueryable();

        if (!string.IsNullOrEmpty(status)) q = q.Where(r => r.Status == status);
        if (!string.IsNullOrEmpty(travelType)) q = q.Where(r => r.TravelType == travelType);

        return await q.OrderByDescending(r => r.CreatedAt).Select(r => ToDto(r)).ToListAsync();
    }

    [HttpGet("{id:guid}")]
    public async Task<ActionResult<TravelRequestDto>> Get(Guid id)
    {
        var r = await db.TravelRequests
            .Include(x => x.RequestedBy)
            .Include(x => x.ApprovedBy)
            .FirstOrDefaultAsync(x => x.Id == id);
        return r == null ? NotFound() : ToDto(r);
    }

    [HttpPost]
    public async Task<ActionResult<TravelRequestDto>> Create(CreateTravelRequestDto dto)
    {
        var callerId = User.FindFirstValue("oid") ?? User.FindFirstValue(ClaimTypes.NameIdentifier);
        var caller = await db.Users.FirstOrDefaultAsync(u => u.EntraObjectId == callerId);
        if (caller == null) return Unauthorized();

        var request = new TravelRequest
        {
            Id = Guid.NewGuid(),
            RequestedById = caller.Id,
            TravellerName = dto.TravellerName,
            TravelType = dto.TravelType,
            Purpose = dto.Purpose,
            Origin = dto.Origin,
            Destination = dto.Destination,
            TravelDate = dto.TravelDate,
            ReturnDate = dto.ReturnDate,
            FlightPreference = dto.FlightPreference,
            HotelName = dto.HotelName,
            NumberOfNights = dto.NumberOfNights,
            PassportNumber = dto.PassportNumber,
            Status = "Pending",
            CreatedAt = DateTime.UtcNow,
            UpdatedAt = DateTime.UtcNow
        };

        db.TravelRequests.Add(request);
        await db.SaveChangesAsync();

        return CreatedAtAction(nameof(Get), new { id = request.Id },
            await GetFullDto(request.Id));
    }

    [HttpPost("{id:guid}/approve")]
    [Authorize(Roles = "Manager,Admin")]
    public async Task<IActionResult> Approve(Guid id, ApproveTravelRequestDto dto)
    {
        var request = await db.TravelRequests.FindAsync(id);
        if (request == null) return NotFound();
        if (request.Status != "Pending")
            return BadRequest(new { error = "Request is not pending" });

        var callerId = User.FindFirstValue("oid") ?? User.FindFirstValue(ClaimTypes.NameIdentifier);
        var caller = await db.Users.FirstOrDefaultAsync(u => u.EntraObjectId == callerId);
        if (caller == null) return Unauthorized();

        request.Status = dto.Action == "Approve" ? "Approved" : "Rejected";
        request.ApprovedById = caller.Id;
        request.ApprovedAt = DateTime.UtcNow;
        request.ApprovalNotes = dto.Notes;
        request.UpdatedAt = DateTime.UtcNow;

        await db.SaveChangesAsync();
        return NoContent();
    }

    [HttpPatch("{id:guid}/booked")]
    [Authorize(Roles = "Coordinator,Manager,Admin")]
    public async Task<IActionResult> MarkBooked(Guid id)
    {
        var request = await db.TravelRequests.FindAsync(id);
        if (request == null) return NotFound();
        request.Status = "Booked";
        request.UpdatedAt = DateTime.UtcNow;
        await db.SaveChangesAsync();
        return NoContent();
    }

    private async Task<TravelRequestDto> GetFullDto(Guid id)
    {
        var r = await db.TravelRequests
            .Include(x => x.RequestedBy)
            .Include(x => x.ApprovedBy)
            .FirstAsync(x => x.Id == id);
        return ToDto(r);
    }

    private static TravelRequestDto ToDto(TravelRequest r) => new(
        r.Id,
        r.RequestedBy?.FullName ?? "",
        r.TravellerName,
        r.TravelType,
        r.Purpose,
        r.Origin,
        r.Destination,
        r.TravelDate,
        r.ReturnDate,
        r.FlightPreference,
        r.HotelName,
        r.NumberOfNights,
        r.PassportNumber,
        r.Status,
        r.ApprovedBy?.FullName,
        r.ApprovedAt,
        r.ApprovalNotes,
        r.CreatedAt);
}
