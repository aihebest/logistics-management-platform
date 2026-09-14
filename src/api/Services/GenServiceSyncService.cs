using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Text.Json;
using LogisticsApi.Data;
using LogisticsApi.Models.DTOs;
using LogisticsApi.Models.Entities;

namespace LogisticsApi.Services;

/// <summary>
/// Outcome of handing a vehicle over to General Service. Carries the reason on
/// failure so the user sees what actually went wrong rather than a guess — the
/// difference between a wrong URL, a wrong key and an unconfigured server is the
/// whole diagnosis, and it should not require reading Azure log streams.
/// </summary>
public record GenServiceHandoff(bool Success, string? Reference, string? Error)
{
    public static GenServiceHandoff Ok(string reference)  => new(true,  reference, null);
    public static GenServiceHandoff Fail(string error)    => new(false, null,      error);
}

public interface IGenServiceSyncService
{
    bool IsConfigured { get; }

    /// <summary>
    /// Report a vehicle to the General Service department for repair. Never throws —
    /// failures come back as <see cref="GenServiceHandoff.Error"/>.
    /// </summary>
    Task<GenServiceHandoff> RaiseMaintenanceRequestAsync(MaintenanceRecord record, CancellationToken ct = default);
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

    public async Task<GenServiceHandoff> RaiseMaintenanceRequestAsync(MaintenanceRecord record, CancellationToken ct = default)
    {
        if (!IsConfigured)
        {
            logger.LogDebug("General Service sync skipped for {Id} — integration not configured.", record.Id);
            return GenServiceHandoff.Fail(
                "The link to General Service is not configured on this server "
                + "(Integration__GenService__BaseUrl / __ApiKey).");
        }

        // Anything that originated on their side is already in their register.
        if (record.SourceSystem == "GenService" || record.GenServiceRequestId.HasValue)
            return GenServiceHandoff.Ok(record.GenServiceRequestNumber ?? "");

        var vehicle = record.Vehicle ?? await db.Vehicles.FindAsync([record.VehicleId], ct);
        if (vehicle is null)
        {
            logger.LogWarning("Cannot raise {Id} with General Service — vehicle {VehicleId} not found.",
                record.Id, record.VehicleId);
            return GenServiceHandoff.Fail("This record's vehicle is missing from the fleet register.");
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

                // Translate the status into the actual fix. These three are the
                // only realistic failures once both sides are deployed, and they
                // have completely different remedies.
                return GenServiceHandoff.Fail((int)res.StatusCode switch
                {
                    503 => "General Service received the request but has no integration key configured. "
                         + "Set Integration__InboundKey on the genservice-desicon app service.",
                    401 => "General Service rejected our key. Integration__GenService__ApiKey here must match "
                         + "Integration__InboundKey on the genservice-desicon app service.",
                    404 => "General Service returned 404 — Integration__GenService__BaseUrl is pointing at the "
                         + "wrong address. It must be the API app service URL, not the public website.",
                    _   => $"General Service returned HTTP {(int)res.StatusCode}. {Truncate(body, 200)}",
                });
            }

            var ack = await res.Content.ReadFromJsonAsync<GenServiceRequestAck>(Json, ct);
            if (ack is null)
                return GenServiceHandoff.Fail(
                    "General Service returned a response we could not read. Check that "
                    + "Integration__GenService__BaseUrl points at the API app service, not the public website.");

            record.GenServiceRequestId     = ack.RequestId;
            record.GenServiceRequestNumber = ack.RequestNumber;
            record.GenServiceStatus        = ack.Status;
            record.GenServiceSyncedAt      = DateTime.UtcNow;
            record.UpdatedAt               = DateTime.UtcNow;
            await db.SaveChangesAsync(ct);

            logger.LogInformation("Raised {Ref} with General Service for {Reg}.",
                ack.RequestNumber, vehicle.RegistrationNo);
            return GenServiceHandoff.Ok(ack.RequestNumber);
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Could not raise {Id} with General Service.", record.Id);
            return GenServiceHandoff.Fail(
                $"Could not reach General Service. {Truncate(ex.Message, 200)}");
        }
    }

    private static string Truncate(string s, int max) =>
        string.IsNullOrEmpty(s) ? s : s.Length <= max ? s : s[..max];
}
