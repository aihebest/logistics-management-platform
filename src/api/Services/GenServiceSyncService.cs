using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Text.Json;
using LogisticsApi.Data;
using LogisticsApi.Models.DTOs;
using LogisticsApi.Models.Entities;

namespace LogisticsApi.Services;

public interface IGenServiceSyncService
{
    bool IsConfigured { get; }

    /// <summary>
    /// Report a vehicle to the General Service department for repair. Returns the
    /// GenService reference (e.g. "V/26/023") if it was raised, null otherwise.
    /// Never throws.
    /// </summary>
    Task<string?> RaiseMaintenanceRequestAsync(MaintenanceRecord record, CancellationToken ct = default);
}

/// <summary>
/// Pushes vehicle faults to the General Service platform.
///
/// The Logistics team owns the vehicles but General Service does the repairs, so
/// a fault logged here has to reach their register to actually get worked on.
///
/// Design rules:
///  • A General Service outage must never stop a Logistics coordinator logging a
///    fault. Every call is wrapped; failures are logged and the record is saved
///    regardless, to be re-sent later.
///  • The reference they return is stored on our record so the two stay tied.
/// </summary>
public class GenServiceSyncService(
    AppDbContext db,
    IHttpClientFactory httpFactory,
    IConfiguration cfg,
    ILogger<GenServiceSyncService> logger) : IGenServiceSyncService
{
    public const string HttpClientName = "GenServiceApi";

    private string? BaseUrl => cfg["Integration:GenService:BaseUrl"]?.TrimEnd('/');
    private string? ApiKey  => cfg["Integration:GenService:ApiKey"];

    public bool IsConfigured => !string.IsNullOrWhiteSpace(BaseUrl) && !string.IsNullOrWhiteSpace(ApiKey);

    private static readonly JsonSerializerOptions Json = new(JsonSerializerDefaults.Web);

    public async Task<string?> RaiseMaintenanceRequestAsync(MaintenanceRecord record, CancellationToken ct = default)
    {
        if (!IsConfigured)
        {
            logger.LogDebug("General Service sync skipped for {Id} — integration not configured.", record.Id);
            return null;
        }

        // Anything that originated on their side is already in their register.
        if (record.SourceSystem == "GenService" || record.GenServiceRequestId.HasValue)
            return record.GenServiceRequestNumber;

        var vehicle = record.Vehicle ?? await db.Vehicles.FindAsync([record.VehicleId], ct);
        if (vehicle is null)
        {
            logger.LogWarning("Cannot raise {Id} with General Service — vehicle {VehicleId} not found.",
                record.Id, record.VehicleId);
            return null;
        }

        var payload = new LogisticsVehicleRequestDto(
            LogisticsRecordId:  record.Id,
            LogisticsVehicleId: vehicle.Id,
            VehicleRegNo:       vehicle.RegistrationNo,
            VehicleType:        $"{vehicle.Make} {vehicle.Model}".Trim(),
            AssetNo:            vehicle.AssetTagNo,
            Category:           GenServiceStatusMap.ToGenServiceCategory(record.Category, record.FaultReported),
            ServiceType:        record.Type,
            Description:        record.FaultDescription ?? record.Type,
            // A reported fault means the vehicle is already off the road.
            Priority:           record.FaultReported ? "High" : "Normal",
            CurrentLocation:    null,
            OdometerKm:         vehicle.OdometerKm == 0 ? null : vehicle.OdometerKm,
            DateReported:       record.ScheduledDate.ToString("yyyy-MM-dd"),
            VendorName:         record.VendorName,
            RequestedByEmail:   cfg["Integration:GenService:RequesterEmail"] ?? "logistics@desicongroup.com",
            RequestedByName:    cfg["Integration:GenService:RequesterName"]  ?? "Logistics Department",
            Notes:              record.Notes
        );

        try
        {
            var client = httpFactory.CreateClient(HttpClientName);
            client.Timeout = TimeSpan.FromSeconds(20);
            client.DefaultRequestHeaders.Accept.Add(new MediaTypeWithQualityHeaderValue("application/json"));
            client.DefaultRequestHeaders.Remove(RequireIntegrationKeyAttribute.HeaderName);
            client.DefaultRequestHeaders.Add(RequireIntegrationKeyAttribute.HeaderName, ApiKey);

            var res = await client.PostAsJsonAsync(
                $"{BaseUrl}/api/v1/integration/logistics/vehicle-requests", payload, Json, ct);

            if (!res.IsSuccessStatusCode)
            {
                var body = await res.Content.ReadAsStringAsync(ct);
                logger.LogWarning("General Service rejected {Id}: HTTP {Code} {Body}",
                    record.Id, (int)res.StatusCode, Truncate(body, 400));
                return null;
            }

            var ack = await res.Content.ReadFromJsonAsync<GenServiceRequestAck>(Json, ct);
            if (ack is null) return null;

            record.GenServiceRequestId     = ack.RequestId;
            record.GenServiceRequestNumber = ack.RequestNumber;
            record.GenServiceStatus        = ack.Status;
            record.GenServiceSyncedAt      = DateTime.UtcNow;
            record.UpdatedAt               = DateTime.UtcNow;
            await db.SaveChangesAsync(ct);

            logger.LogInformation("Raised {Ref} with General Service for {Reg}.",
                ack.RequestNumber, vehicle.RegistrationNo);
            return ack.RequestNumber;
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Could not raise {Id} with General Service.", record.Id);
            return null;
        }
    }

    private static string Truncate(string s, int max) =>
        string.IsNullOrEmpty(s) ? s : s.Length <= max ? s : s[..max];
}
