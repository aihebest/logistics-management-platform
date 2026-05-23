using LogisticsApi.Models.Entities;

namespace LogisticsApi.Services.AssignmentEngine;

public interface IAssignmentEngine
{
    /// <summary>
    /// Attempts to automatically assign a driver and vehicle to the given trip request.
    /// Returns null if no eligible driver is available (request remains Pending).
    /// </summary>
    Task<Assignment?> AssignAsync(TripRequest tripRequest, Guid assignedById);
}
