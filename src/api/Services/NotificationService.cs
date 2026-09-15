using Azure.Communication.Email;
using LogisticsApi.Data;
using LogisticsApi.Models.Entities;
using Microsoft.EntityFrameworkCore;
using System.Net;
using System.Net.Mail;

namespace LogisticsApi.Services;

public interface INotificationService
{
    // ── Trip request lifecycle ──────────────────────────────────────────────────
    Task SendTripRequestSubmittedAsync(TripRequest trip);
    Task SendTripRequestApprovedAsync(TripRequest trip);
    Task SendTripRequestRejectedAsync(TripRequest trip, string reason);
    Task SendTripCompletedAsync(TripRequest trip);
    Task SendNoDriverAvailableAsync(TripRequest trip);

    // ── Assignment ──────────────────────────────────────────────────────────────
    Task SendAssignmentConfirmedAsync(Assignment assignment);

    // ── Material transport approval chain ───────────────────────────────────────
    Task SendMaterialAwaitingHodAsync(MaterialTransportRequest request);
    Task SendMaterialAwaitingManagerAsync(MaterialTransportRequest request);
    Task SendMaterialApprovedAsync(MaterialTransportRequest request);
    Task SendMaterialRejectedAsync(MaterialTransportRequest request, string stage, string? reason);
    Task SendMaterialDispatchedAsync(MaterialTransportRequest request);

    // ── Maintenance ─────────────────────────────────────────────────────────────
    Task SendVehicleServiceDueAsync(Vehicle vehicle, int daysUntilDue);

    // ── Travel Request Form ─────────────────────────────────────────────────────
    /// <summary>Pass the department's head, or null to fall back to all HODs.</summary>
    Task SendTravelRequestSubmittedAsync(TravelRequest request, User? hod);
    Task SendTravelRequestVerifiedAsync(TravelRequest request);
    Task SendTravelRequestApprovedAsync(TravelRequest request);
    Task SendTravelRequestRejectedAsync(TravelRequest request, string rejectedByName);
    Task SendMaintenanceOverdueAsync(MaintenanceRecord record);
    Task SendEmergencyMaintenanceLoggedAsync(MaintenanceRecord record);
    Task SendMaintenanceCompletedAsync(MaintenanceRecord record);
    Task SendVehicleReturnedToServiceAsync(MaintenanceRecord record);

    // ── In-app ──────────────────────────────────────────────────────────────────
    Task NotifyInAppAsync(Guid recipientId, string type, string subject, string body,
                          string? relatedEntityType = null, string? relatedEntityId = null);
}

public class NotificationService(
    AppDbContext db,
    IConfiguration config,
    ILogger<NotificationService> logger,
    EmailClient? emailClient = null) : INotificationService
{
    private readonly string _fromAddress  = config["Email:FromAddress"]  ?? "noreply@desicon.com";
    private readonly string _fromName     = config["Email:FromName"]     ?? "Desicon Logistics Platform";
    private readonly string _smtpHost     = config["Email:SmtpHost"]     ?? "smtp.office365.com";
    private readonly int    _smtpPort     = int.Parse(config["Email:SmtpPort"] ?? "587");
    private readonly bool   _smtpSsl      = bool.Parse(config["Email:UseSsl"] ?? "true");
    private readonly string? _smtpUser    = config["Email:SmtpUsername"];
    private readonly string? _smtpPass    = config["Email:SmtpPassword"];

    // ── Configured escalation recipients ────────────────────────────────────────
    // Override via App Service → Configuration:
    //   Email__ManagerEmail = logistics.manager@desicongroup.com
    //   Email__SupervisorEmail = supervisor@desicongroup.com
    private readonly string? _managerEmail    = config["Email:ManagerEmail"];
    private readonly string? _supervisorEmail = config["Email:SupervisorEmail"];

    // ═══════════════════════════════════════════════════════════════════════════
    // TRIP REQUEST NOTIFICATIONS
    // ═══════════════════════════════════════════════════════════════════════════

    public async Task SendTripRequestSubmittedAsync(TripRequest trip)
    {
        var requester = trip.RequestedBy;
        var subject   = $"New Trip Request — {trip.Purpose} ({trip.RequestedDateTime:dd MMM yyyy HH:mm})";
        var body      = $"""
            A new transport request has been submitted and requires your attention.

            Reference:   {trip.Id}
            Requested By:{requester?.FullName ?? "Unknown"}
            Purpose:     {trip.Purpose}
            Pickup:      {trip.PickupLocation}
            Destination: {trip.DestinationLocation}
            Date/Time:   {trip.RequestedDateTime:f}
            Priority:    {trip.Priority}
            Notes:       {trip.Notes ?? "None"}

            Please log in to the platform to review and assign this request.

            {PlatformUrl()}
            """;

        // Coordinators and Managers action trip requests — send to them only.
        await SendToRolesAsync(subject, body, "Coordinator", "Manager");

        // Also in-app notify Coordinators and Managers
        var recipientIds = await db.Users
            .Where(u => u.IsActive && (u.Role == "Coordinator" || u.Role == "Manager"))
            .Select(u => u.Id)
            .ToListAsync();
        await NotifyManyInAppAsync(recipientIds, "TripRequestSubmitted", subject,
            $"New request from {requester?.FullName}: {trip.Purpose} — {trip.PickupLocation} → {trip.DestinationLocation}",
            "TripRequest", trip.Id.ToString());

        // Send confirmation to the requester
        if (requester?.Email is { Length: > 0 } requesterEmail)
        {
            var confirmSubject = $"Trip Request Received — Ref: {trip.Id.ToString()[..8].ToUpper()}";
            var confirmBody    = $"""
                Hi {requester.FullName},

                Your transport request has been received and is being reviewed by the logistics team.

                Reference:   {trip.Id.ToString()[..8].ToUpper()}
                Purpose:     {trip.Purpose}
                Pickup:      {trip.PickupLocation}
                Destination: {trip.DestinationLocation}
                Date/Time:   {trip.RequestedDateTime:f}

                You will be notified once a driver and vehicle have been assigned.

                {PlatformUrl()}
                """;
            await SendEmailAsync(requesterEmail, confirmSubject, confirmBody);
        }

        logger.LogInformation("Trip request submitted notifications sent for {TripId}", trip.Id);
    }

    public async Task SendTripRequestApprovedAsync(TripRequest trip)
    {
        var requester = trip.RequestedBy;
        if (requester?.Email is not { Length: > 0 } to) return;

        var assignment = trip.Assignment;
        var subject    = $"Trip Approved & Driver Assigned — {trip.Purpose}";
        var body       = $"""
            Hi {requester.FullName},

            Your transport request has been approved and a driver has been assigned.

            Reference:   {trip.Id.ToString()[..8].ToUpper()}
            Purpose:     {trip.Purpose}
            Pickup:      {trip.PickupLocation}
            Destination: {trip.DestinationLocation}
            Date/Time:   {trip.RequestedDateTime:f}
            Driver:      {assignment?.Driver?.FullName ?? "To be confirmed"}
            Vehicle:     {(assignment?.Vehicle != null ? $"{assignment.Vehicle.Make} {assignment.Vehicle.Model} ({assignment.Vehicle.RegistrationNo})" : "To be confirmed")}

            Please be at the pickup location on time. Contact the logistics coordinator if you have any questions.

            {PlatformUrl()}
            """;

        await SendEmailAsync(to, subject, body);
        await NotifyInAppAsync(requester.Id, "TripRequestApproved", subject,
            $"Your trip request has been approved. Driver: {assignment?.Driver?.FullName ?? "TBC"}",
            "TripRequest", trip.Id.ToString());

        logger.LogInformation("Trip approved notification sent to {Email}", to);
    }

    public async Task SendTripRequestRejectedAsync(TripRequest trip, string reason)
    {
        var requester = trip.RequestedBy;
        if (requester?.Email is not { Length: > 0 } to) return;

        var subject = $"Trip Request Update — {trip.Purpose}";
        var body    = $"""
            Hi {requester.FullName},

            We were unable to accommodate your transport request at this time.

            Reference:   {trip.Id.ToString()[..8].ToUpper()}
            Purpose:     {trip.Purpose}
            Date/Time:   {trip.RequestedDateTime:f}
            Reason:      {reason}

            Please contact your transport coordinator if you need to discuss alternatives or resubmit your request.

            {PlatformUrl()}
            """;

        await SendEmailAsync(to, subject, body);
        await NotifyInAppAsync(requester.Id, "TripRequestRejected", subject,
            $"Your trip request could not be processed: {reason}",
            "TripRequest", trip.Id.ToString());

        logger.LogInformation("Trip rejected notification sent to {Email}", to);
    }

    public async Task SendTripCompletedAsync(TripRequest trip)
    {
        var subject = $"Trip Completed — {trip.Purpose}";
        var body    = $"""
            A trip has been completed.

            Reference:   {trip.Id.ToString()[..8].ToUpper()}
            Purpose:     {trip.Purpose}
            Pickup:      {trip.PickupLocation}
            Destination: {trip.DestinationLocation}
            Driver:      {trip.Assignment?.Driver?.FullName ?? "Unknown"}
            Vehicle:     {trip.Assignment?.Vehicle?.RegistrationNo ?? "Unknown"}

            The driver and vehicle are now available for the next assignment.

            {PlatformUrl()}
            """;

        var recipients = await GetEmailsForRolesAsync("Coordinator", "Manager");
        await SendEmailToManyAsync(recipients, subject, body);
    }

    /// <summary>
    /// A request was approved but no driver/vehicle is available. Alerts
    /// coordinators and managers so they can arrange alternative capacity.
    /// </summary>
    public async Task SendNoDriverAvailableAsync(TripRequest trip)
    {
        var subject = $"No Driver/Vehicle Available — {trip.Purpose} ({trip.RequestedDateTime:dd MMM yyyy HH:mm})";
        var body    = $"""
            A trip request has been approved but cannot be assigned — no driver or
            vehicle is currently available at the requested time.

            Reference:   {trip.Id.ToString()[..8].ToUpper()}
            Requested By:{trip.RequestedBy?.FullName ?? "Unknown"}
            Purpose:     {trip.Purpose}
            Pickup:      {trip.PickupLocation}
            Destination: {trip.DestinationLocation}
            Date/Time:   {trip.RequestedDateTime:f}
            Priority:    {trip.Priority}

            The request is sitting in the pending queue. Please arrange an alternative
            driver/vehicle or contact the requester to reschedule.

            {PlatformUrl()}
            """;

        var recipients = await GetEmailsForRolesAsync("Coordinator", "Manager");
        await SendEmailToManyAsync(recipients, subject, body);

        // In-app alert to coordinators/managers too
        var alertIds = await db.Users
            .Where(u => u.IsActive && (u.Role == "Coordinator" || u.Role == "Manager"))
            .Select(u => u.Id)
            .ToListAsync();
        await NotifyManyInAppAsync(alertIds, "NoDriverAvailable", subject,
            $"No capacity for: {trip.Purpose} — {trip.PickupLocation} → {trip.DestinationLocation}",
            "TripRequest", trip.Id.ToString());

        logger.LogWarning("No driver available alert sent for trip {TripId}", trip.Id);
    }

    // ═══════════════════════════════════════════════════════════════════════════
    // ASSIGNMENT NOTIFICATION
    // ═══════════════════════════════════════════════════════════════════════════

    public async Task SendAssignmentConfirmedAsync(Assignment assignment)
    {
        var driver  = assignment.Driver;
        var subject = $"New Trip Assignment — {assignment.TripRequest.Purpose}";
        var body    = $"""
            Hi {driver.FullName},

            You have been assigned to a transport trip. Please review the details below.

            Purpose:     {assignment.TripRequest.Purpose}
            Pickup:      {assignment.TripRequest.PickupLocation}
            Destination: {assignment.TripRequest.DestinationLocation}
            Date/Time:   {assignment.StartTime:f}
            Vehicle:     {assignment.Vehicle.Make} {assignment.Vehicle.Model} ({assignment.Vehicle.RegistrationNo})
            Requested By:{assignment.TripRequest.RequestedBy?.FullName ?? "Unknown"}

            Please update your status on the platform once the trip is underway, and again when it is complete.

            {PlatformUrl()}
            """;

        // In-app notification for the driver
        await NotifyInAppAsync(driver.Id, "AssignmentConfirmed", subject,
            $"New trip: {assignment.TripRequest.Purpose} — {assignment.TripRequest.PickupLocation} → {assignment.TripRequest.DestinationLocation}",
            "Assignment", assignment.Id.ToString());

        // Email the driver
        if (!string.IsNullOrWhiteSpace(driver.Email))
            await SendEmailAsync(driver.Email, subject, body);

        // Also notify the requester that a driver has been assigned
        await SendTripRequestApprovedAsync(assignment.TripRequest);

        logger.LogInformation("Assignment confirmed notifications sent for assignment {Id}", assignment.Id);
    }

    // ═══════════════════════════════════════════════════════════════════════════
    // MAINTENANCE NOTIFICATIONS — send to managers, NOT the from address
    // ═══════════════════════════════════════════════════════════════════════════

    /// <summary>
    /// A vehicle is approaching its next scheduled service.
    ///
    /// Driven by the vehicle's own NextServiceDate, which is set when a service
    /// completes — not by a maintenance record. Maintenance records now carry the
    /// date a fault was *reported*, which is always in the past and so can never
    /// tell anyone what is coming up.
    /// </summary>
    public async Task SendVehicleServiceDueAsync(Vehicle vehicle, int daysUntilDue)
    {
        var whenText = daysUntilDue == 0
            ? "due today"
            : $"due in {daysUntilDue} day{(daysUntilDue == 1 ? "" : "s")}";

        var subject = $"Service {whenText} — {vehicle.RegistrationNo}";
        var body    = $"""
            Service Reminder — Action Required

            Vehicle:        {vehicle.Make} {vehicle.Model} ({vehicle.RegistrationNo})
            Next Service:   {vehicle.NextServiceDate:D}
            Days Until Due: {daysUntilDue}
            Last Service:   {(vehicle.LastServiceDate.HasValue ? vehicle.LastServiceDate.Value.ToString("D") : "Not recorded")}
            Odometer:       {vehicle.OdometerKm:N0} km
            Service Every:  {vehicle.ServiceIntervalKm:N0} km

            Please arrange for this vehicle to be taken in for service before the due date.

            {PlatformUrl()}
            """;

        await SendToMaintenanceTeamAsync(subject, body);
        logger.LogInformation("Service due reminder sent: {Vehicle} — {Days} days", vehicle.RegistrationNo, daysUntilDue);
    }

    // ── Travel Request Form (DEL-LG-FRM-002) ────────────────────────────────────

    /// <summary>
    /// Short description of the journey for an email subject or summary line —
    /// the first outbound leg, which is what people recognise the trip by.
    /// </summary>
    private static string TravelSummary(TravelRequest r)
    {
        var first = r.Legs
            .Where(l => l.Direction == "Outbound")
            .OrderBy(l => l.Sequence)
            .FirstOrDefault();

        return first == null
            ? r.PurposeOfTravel
            : $"{first.From} → {first.To} on {first.TravelDate:dd MMM yyyy}";
    }

    private static string TravellerName(TravelRequest r) => $"{r.GivenName} {r.Surname}".Trim();

    /// <summary>
    /// A submitted form is waiting on the head of the requester's department.
    /// When that department has no registered head, every HOD is told instead so
    /// the request is not left with nowhere to go.
    /// </summary>
    public async Task SendTravelRequestSubmittedAsync(TravelRequest request, User? hod)
    {
        var subject = $"Travel Request for Verification — {request.FormNumber} ({TravellerName(request)})";
        var body    = $"""
            A travel request needs your verification.

            Form No:     {request.FormNumber}
            Traveller:   {TravellerName(request)}
            Department:  {request.Department}
            Position:    {request.Position ?? "Not stated"}
            Journey:     {TravelSummary(request)}
            Purpose:     {request.PurposeOfTravel}
            Hotel:       {(request.HotelBookingRequired ? "Required" : "Not required")}

            Once you verify it, the request goes to the DMD for management approval.

            {PlatformUrl()}
            """;

        if (hod is { Email.Length: > 0 })
        {
            await SendEmailAsync(hod.Email, subject, body);
            await NotifyInAppAsync(hod.Id, "TravelRequestSubmitted", subject,
                $"{TravellerName(request)} — {TravelSummary(request)}",
                "TravelRequest", request.Id.ToString());
        }
        else
        {
            // No head registered for that department — tell them all, and say why.
            await SendToRolesAsync(
                subject,
                body + $"\n\nNote: {request.Department} has no head of department assigned on the " +
                       "platform, so this has gone to all HODs. Please assign one under Departments.",
                "HOD");
        }

        // Confirm receipt to whoever raised it.
        var requester = request.RequestedBy;
        if (requester?.Email is { Length: > 0 } requesterEmail)
        {
            await SendEmailAsync(requesterEmail,
                $"Travel Request Received — {request.FormNumber}",
                $"""
                Hi {requester.FullName},

                Your travel request has been received and is with your head of department
                for verification. You will be notified when it has been approved.

                Form No:   {request.FormNumber}
                Journey:   {TravelSummary(request)}
                Purpose:   {request.PurposeOfTravel}

                {PlatformUrl()}
                """);
        }

        logger.LogInformation("Travel request {Form} submitted notifications sent", request.FormNumber);
    }

    /// <summary>Verified by the head of department — now waiting on the DMD/MD.</summary>
    public async Task SendTravelRequestVerifiedAsync(TravelRequest request)
    {
        var subject = $"Travel Request for Approval — {request.FormNumber} ({TravellerName(request)})";
        var body    = $"""
            A travel request has been verified by the head of department and needs
            management approval.

            Form No:     {request.FormNumber}
            Traveller:   {TravellerName(request)}
            Department:  {request.Department}
            Journey:     {TravelSummary(request)}
            Purpose:     {request.PurposeOfTravel}
            Hotel:       {(request.HotelBookingRequired ? "Required" : "Not required")}

            Verified by: {request.VerifiedBy?.FullName ?? "Head of Department"}
            Verified on: {request.VerifiedAt:dd MMM yyyy HH:mm} UTC

            {PlatformUrl()}
            """;

        await SendToRolesAsync(subject, body, "Management");

        // Keep the requester informed that it has cleared the first stage.
        if (request.RequestedBy?.Email is { Length: > 0 } requesterEmail)
        {
            await SendEmailAsync(requesterEmail,
                $"Travel Request Verified — {request.FormNumber}",
                $"""
                Hi {request.RequestedBy.FullName},

                Your travel request has been verified by {request.VerifiedBy?.FullName ?? "your head of department"}
                and is now with management for final approval.

                Form No:  {request.FormNumber}
                Journey:  {TravelSummary(request)}

                {PlatformUrl()}
                """);
        }

        logger.LogInformation("Travel request {Form} verified notifications sent", request.FormNumber);
    }

    /// <summary>
    /// Fully approved. Logistics can now download the completed form and book,
    /// so they are the ones who need this most.
    /// </summary>
    public async Task SendTravelRequestApprovedAsync(TravelRequest request)
    {
        var subject = $"Travel Request APPROVED — {request.FormNumber} ({TravellerName(request)})";
        var body    = $"""
            A travel request has been fully approved and is ready to be processed.

            Form No:     {request.FormNumber}
            Traveller:   {TravellerName(request)}
            Department:  {request.Department}
            Phone:       {request.PhoneNumber ?? "Not given"}
            Email:       {request.Email ?? "Not given"}
            Journey:     {TravelSummary(request)}
            Purpose:     {request.PurposeOfTravel}
            Hotel:       {(request.HotelBookingRequired ? "REQUIRED" : "Not required")}
            Cost Centre: {request.ProjectCostCentreCode ?? "Not stated"}

            Verified by: {request.VerifiedBy?.FullName ?? "—"} on {request.VerifiedAt:dd MMM yyyy HH:mm}
            Approved by: {request.ApprovedBy?.FullName ?? "—"} on {request.ApprovedAt:dd MMM yyyy HH:mm}

            Open the request in the platform and use Download Form to produce the
            signed PDF for booking.

            {PlatformUrl()}
            """;

        await SendToRolesAsync(subject, body, "Coordinator", "Manager");

        if (request.RequestedBy?.Email is { Length: > 0 } requesterEmail)
        {
            await SendEmailAsync(requesterEmail,
                $"Travel Request Approved — {request.FormNumber}",
                $"""
                Hi {request.RequestedBy.FullName},

                Your travel request has been approved by management and passed to the
                logistics team to book.

                Form No:  {request.FormNumber}
                Journey:  {TravelSummary(request)}

                Please notify the Logistics Unit of any change to your travel date at
                least 24 hours before departure.

                {PlatformUrl()}
                """);

            await NotifyInAppAsync(request.RequestedById, "TravelRequestApproved",
                $"Travel request {request.FormNumber} approved",
                TravelSummary(request), "TravelRequest", request.Id.ToString());
        }

        logger.LogInformation("Travel request {Form} approved notifications sent", request.FormNumber);
    }

    public async Task SendTravelRequestRejectedAsync(TravelRequest request, string rejectedByName)
    {
        if (request.RequestedBy?.Email is not { Length: > 0 } to) return;

        var subject = $"Travel Request Not Approved — {request.FormNumber}";
        var body    = $"""
            Hi {request.RequestedBy.FullName},

            Your travel request could not be approved.

            Form No:  {request.FormNumber}
            Journey:  {TravelSummary(request)}
            Reason:   {request.RejectionReason ?? "No reason given"}
            Decided by: {rejectedByName}

            Speak with {rejectedByName} if you need to discuss it, or raise a new
            request with the details corrected.

            {PlatformUrl()}
            """;

        await SendEmailAsync(to, subject, body);
        await NotifyInAppAsync(request.RequestedById, "TravelRequestRejected", subject,
            request.RejectionReason ?? "No reason given",
            "TravelRequest", request.Id.ToString());

        logger.LogInformation("Travel request {Form} rejection notification sent", request.FormNumber);
    }

    public async Task SendMaintenanceOverdueAsync(MaintenanceRecord record)
    {
        var vehicle   = record.Vehicle;
        // Days the job has been open since it was reported — the record's date
        // column now means "reported on", not "scheduled for".
        var daysOpen  = DateOnly.FromDateTime(DateTime.UtcNow).DayNumber - record.ScheduledDate.DayNumber;
        var subject   = $"OVERDUE Maintenance — {vehicle.RegistrationNo} (open {daysOpen} days)";
        var body      = $"""
            ⚠️ OVERDUE MAINTENANCE ALERT — Immediate Action Required

            Vehicle:        {vehicle.Make} {vehicle.Model} ({vehicle.RegistrationNo})
            Service Type:   {record.Type}
            Date Reported:  {record.ScheduledDate:D}
            Days Open:      {daysOpen}
            Status:         {record.Status}
            Vendor:         {record.VendorName ?? "Not specified"}

            This job has been open for more than {MaintenancePolicy.OverdueGraceDays} days. The vehicle may not be fit for
            continued service. Please chase the workshop and update the record in the platform.

            {PlatformUrl()}
            """;

        await SendToMaintenanceTeamAsync(subject, body);
        logger.LogWarning("Overdue maintenance alert sent: {Vehicle} — open {Days} days", vehicle.RegistrationNo, daysOpen);
    }

    /// <summary>
    /// An emergency/fault maintenance record was logged. Notifies the Logistics
    /// Manager and Supervisor immediately so repairs can be authorised.
    /// </summary>
    public async Task SendEmergencyMaintenanceLoggedAsync(MaintenanceRecord record)
    {
        var vehicle = record.Vehicle;
        var subject = $"EMERGENCY Maintenance Logged — {vehicle.RegistrationNo}";
        var body    = $"""
            ⚠️ EMERGENCY / FAULT MAINTENANCE LOGGED — Action Required

            Vehicle:        {vehicle.Make} {vehicle.Model} ({vehicle.RegistrationNo})
            Fault Reported: {record.FaultDescription ?? record.Type}
            Date Reported:  {(record.DateReported?.ToString("f") ?? DateTime.UtcNow.ToString("f"))}
            Logged Type:    {record.Type}
            Vendor:         {record.VendorName ?? "Not yet assigned"}
            Notes:          {record.Notes ?? "None"}

            The vehicle has been placed In Maintenance and is not available for trips.
            Please authorise repairs and arrange an alternative vehicle if needed.

            {PlatformUrl()}
            """;

        await SendToMaintenanceTeamAsync(subject, body);
        logger.LogWarning("Emergency maintenance alert sent: {Vehicle}", vehicle.RegistrationNo);
    }

    /// <summary>
    /// Maintenance was completed. Notifies Logistics Manager and Coordinator with
    /// the service summary and cost for budget records.
    /// </summary>
    public async Task SendMaintenanceCompletedAsync(MaintenanceRecord record)
    {
        var vehicle = record.Vehicle;
        var subject = $"Maintenance Completed — {vehicle.RegistrationNo}";
        var body    = $"""
            Maintenance has been completed and signed off.

            Vehicle:        {vehicle.Make} {vehicle.Model} ({vehicle.RegistrationNo})
            Service Type:   {record.Type}
            Completed:      {(record.CompletedDate?.ToString("D") ?? DateTime.UtcNow.ToString("D"))}
            Cost:           {(record.Cost.HasValue ? record.Cost.Value.ToString("N2") : "Not recorded")}
            Vendor:         {record.VendorName ?? "Not specified"}
            Parts Replaced: {record.PartsReplaced ?? "None recorded"}
            Next Service:   {(vehicle.NextServiceDate?.ToString("D") ?? "See schedule")}

            {PlatformUrl()}
            """;

        var recipients = await GetEmailsForRolesAsync("Coordinator", "Manager");
        await SendEmailToManyAsync(recipients, subject, body);

        logger.LogInformation("Maintenance completed notification sent: {Vehicle}", vehicle.RegistrationNo);
    }

    /// <summary>
    /// A vehicle has been returned to Available status after maintenance.
    /// Notifies coordinators it can be assigned again.
    /// </summary>
    public async Task SendVehicleReturnedToServiceAsync(MaintenanceRecord record)
    {
        var vehicle = record.Vehicle;
        var subject = $"Vehicle Back In Service — {vehicle.RegistrationNo}";
        var body    = $"""
            The following vehicle has completed maintenance and is now Available for
            assignment.

            Vehicle:      {vehicle.Make} {vehicle.Model} ({vehicle.RegistrationNo})
            Service Done: {record.Type}
            Completed:    {(record.CompletedDate?.ToString("D") ?? DateTime.UtcNow.ToString("D"))}

            {PlatformUrl()}
            """;

        var recipients = await GetEmailsForRolesAsync("Coordinator", "Manager");
        await SendEmailToManyAsync(recipients, subject, body);

        logger.LogInformation("Vehicle returned to service notification sent: {Vehicle}", vehicle.RegistrationNo);
    }

    // ═══════════════════════════════════════════════════════════════════════════
    // MATERIAL TRANSPORT APPROVAL CHAIN
    // Requestor submits → HOD approves → GM Logistics approves → driver assigned.
    // Each handoff emails the group that now has to act, so requests don't sit
    // unnoticed in the queue.
    // ═══════════════════════════════════════════════════════════════════════════

    /// <summary>Shared summary block used by every material transport email.</summary>
    private static string MaterialDetails(MaterialTransportRequest r) => $"""
        Form No:     {r.FormNumber}
        Project:     {r.ProjectName}
        Purpose:     {r.Purpose}
        Loading:     {r.LoadingPoint}{(r.LoadingDate.HasValue ? $" on {r.LoadingDate:dd MMM yyyy}" : "")}
        Delivery:    {r.DeliveryPoint}{(r.DeliveryDate.HasValue ? $" by {r.DeliveryDate:dd MMM yyyy}" : "")}
        Items:       {r.Items.Count}
        Requested By:{r.RequestedBy?.FullName ?? "Unknown"}
        """;

    public async Task SendMaterialAwaitingHodAsync(MaterialTransportRequest request)
    {
        var subject = $"Material Transport — HOD approval needed ({request.FormNumber})";
        var body = $"""
            A material transport request has been submitted and is awaiting your approval as HOD.

            {MaterialDetails(request)}

            Please log in to review and approve or reject this request. It cannot proceed
            to GM Logistics until you action it.

            {PlatformUrl()}
            """;

        await SendToRolesAsync(subject, body, "HOD");
        await NotifyRolesInAppAsync("MaterialAwaitingHod", subject,
            $"{request.FormNumber} — {request.ProjectName}: {request.Purpose}",
            "MaterialTransportRequest", request.Id, "HOD");

        logger.LogInformation("Material transport {FormNo} — HOD approval requested", request.FormNumber);
    }

    public async Task SendMaterialAwaitingManagerAsync(MaterialTransportRequest request)
    {
        var subject = $"Material Transport — GM Logistics approval needed ({request.FormNumber})";
        var body = $"""
            A material transport request has been approved by the HOD and now requires
            GM Logistics approval.

            {MaterialDetails(request)}
            HOD Approved:{request.HodApprovedAt:dd MMM yyyy HH:mm}
            HOD Remarks: {request.HodRemarks ?? "None"}

            Once you approve, a driver and vehicle can be assigned.

            {PlatformUrl()}
            """;

        await SendToRolesAsync(subject, body, "Manager");
        await NotifyRolesInAppAsync("MaterialAwaitingManager", subject,
            $"{request.FormNumber} — HOD approved, awaiting GM Logistics",
            "MaterialTransportRequest", request.Id, "Manager");

        logger.LogInformation("Material transport {FormNo} — GM Logistics approval requested", request.FormNumber);
    }

    public async Task SendMaterialApprovedAsync(MaterialTransportRequest request)
    {
        var subject = $"Material Transport approved — assign driver ({request.FormNumber})";
        var body = $"""
            A material transport request has completed both approval stages and is ready
            for a driver and vehicle to be assigned.

            {MaterialDetails(request)}

            Please assign a driver and vehicle so the movement can be scheduled.

            {PlatformUrl()}
            """;

        // Coordinators do the assigning; managers copied for visibility.
        await SendToRolesAsync(subject, body, "Coordinator");
        await NotifyRolesInAppAsync("MaterialApproved", subject,
            $"{request.FormNumber} approved — needs driver & vehicle",
            "MaterialTransportRequest", request.Id, "Coordinator");

        // Tell the requester it cleared approval.
        if (request.RequestedBy?.Email is { Length: > 0 } requesterEmail)
        {
            await SendEmailAsync(requesterEmail,
                $"Your material transport request was approved ({request.FormNumber})",
                $"""
                Hi {request.RequestedBy.FullName},

                Your material transport request has been approved by both the HOD and
                GM Logistics. A driver and vehicle will be assigned shortly.

                {MaterialDetails(request)}

                {PlatformUrl()}
                """);
        }

        logger.LogInformation("Material transport {FormNo} fully approved", request.FormNumber);
    }

    public async Task SendMaterialRejectedAsync(MaterialTransportRequest request, string stage, string? reason)
    {
        var subject = $"Material Transport request declined ({request.FormNumber})";
        var body = $"""
            Hi {request.RequestedBy?.FullName ?? "there"},

            Your material transport request was not approved at the {stage} stage.

            {MaterialDetails(request)}
            Reason:      {reason ?? "No reason provided"}

            Please contact the logistics team if you need to discuss or resubmit.

            {PlatformUrl()}
            """;

        if (request.RequestedBy?.Email is { Length: > 0 } to)
            await SendEmailAsync(to, subject, body);

        if (request.RequestedBy != null)
            await NotifyInAppAsync(request.RequestedBy.Id, "MaterialRejected", subject,
                $"{request.FormNumber} declined at {stage}: {reason ?? "no reason given"}",
                "MaterialTransportRequest", request.Id.ToString());

        // Keep the logistics team copied so the queue stays visible.
        await SendToRolesAsync(subject, body, "Coordinator");

        logger.LogInformation("Material transport {FormNo} rejected at {Stage}", request.FormNumber, stage);
    }

    public async Task SendMaterialDispatchedAsync(MaterialTransportRequest request)
    {
        var driver  = request.AssignedDriver;
        var vehicle = request.AssignedVehicle;
        var subject = $"Material Transport assigned ({request.FormNumber})";
        var body = $"""
            A driver and vehicle have been assigned to a material transport request.

            {MaterialDetails(request)}
            Driver:      {driver?.FullName ?? "To be confirmed"}
            Vehicle:     {vehicle?.RegistrationNo ?? "To be confirmed"}

            {PlatformUrl()}
            """;

        if (driver != null)
        {
            await NotifyInAppAsync(driver.Id, "MaterialAssigned", subject,
                $"{request.FormNumber}: {request.LoadingPoint} → {request.DeliveryPoint}",
                "MaterialTransportRequest", request.Id.ToString());

            if (!string.IsNullOrWhiteSpace(driver.Email))
                await SendEmailAsync(driver.Email, subject, body);
        }

        if (request.RequestedBy?.Email is { Length: > 0 } requesterEmail)
            await SendEmailAsync(requesterEmail, subject, body);

        await SendToRolesAsync(subject, body, "Coordinator");

        logger.LogInformation("Material transport {FormNo} dispatched", request.FormNumber);
    }

    /// <summary>
    /// Emails the people who actually hold the given roles.
    ///
    /// The configured Logistics-Team address is a <em>fallback only</em>, used when
    /// nobody holds the role yet — otherwise every notification went to the whole
    /// distribution list and the entire team received messages meant for one
    /// approver.
    /// </summary>
    private async Task SendToRolesAsync(string subject, string body, params string[] roles)
    {
        var recipients = await GetEmailsForRolesAsync(roles);

        if (recipients.Count == 0)
        {
            recipients = new[] { _managerEmail, _supervisorEmail }
                .Where(e => !string.IsNullOrWhiteSpace(e))
                .Select(e => e!)
                .Distinct(StringComparer.OrdinalIgnoreCase)
                .ToList();

            if (recipients.Count > 0)
                logger.LogWarning(
                    "No users hold role(s) {Roles} — falling back to the configured team address",
                    string.Join("/", roles));
        }

        await SendEmailToManyAsync(recipients, subject, body);
    }

    /// <summary>Raises an in-app notification for every active user in the given roles.</summary>
    private async Task NotifyRolesInAppAsync(string type, string subject, string body,
                                             string relatedType, Guid relatedId, params string[] roles)
    {
        var recipientIds = await db.Users
            .Where(u => u.IsActive && roles.Contains(u.Role))
            .Select(u => u.Id)
            .ToListAsync();

        await NotifyManyInAppAsync(recipientIds, type, subject, body, relatedType, relatedId.ToString());
    }

    /// <summary>
    /// Inserts in-app notifications for several recipients in a single round-trip.
    /// Saving once per person meant a separate database call for every member of
    /// the team on each request.
    /// </summary>
    private async Task NotifyManyInAppAsync(IEnumerable<Guid> recipientIds, string type,
                                            string subject, string body,
                                            string? relatedEntityType = null, string? relatedEntityId = null)
    {
        var ids = recipientIds.Distinct().ToList();
        if (ids.Count == 0) return;

        try
        {
            db.ChangeTracker.AutoDetectChangesEnabled = false;
            var now = DateTime.UtcNow;

            db.Notifications.AddRange(ids.Select(id => new Notification
            {
                Id                = Guid.NewGuid(),
                RecipientId       = id,
                Channel           = "InApp",
                Type              = type,
                Subject           = subject,
                Body              = body,
                Status            = "Sent",
                SentAt            = now,
                RelatedEntityType = relatedEntityType,
                RelatedEntityId   = relatedEntityId,
                CreatedAt         = now
            }));

            await db.SaveChangesAsync();
        }
        catch (Exception ex)
        {
            logger.LogWarning(ex, "Bulk in-app notification failed for {Count} recipient(s) — continuing", ids.Count);
        }
        finally
        {
            db.ChangeTracker.AutoDetectChangesEnabled = true;
        }
    }

    // ═══════════════════════════════════════════════════════════════════════════
    // IN-APP NOTIFICATIONS
    // ═══════════════════════════════════════════════════════════════════════════

    public async Task NotifyInAppAsync(Guid recipientId, string type, string subject, string body,
                                       string? relatedEntityType = null, string? relatedEntityId = null)
    {
        try
        {
            // Use a fresh SaveChanges scope — detach everything to avoid EF tracker conflicts
            // when called after another SaveChangesAsync in the same request pipeline
            db.ChangeTracker.AutoDetectChangesEnabled = false;
            db.Notifications.Add(new Notification
            {
                Id                = Guid.NewGuid(),
                RecipientId       = recipientId,
                Channel           = "InApp",
                Type              = type,
                Subject           = subject,
                Body              = body,
                Status            = "Sent",
                SentAt            = DateTime.UtcNow,
                RelatedEntityType = relatedEntityType,
                RelatedEntityId   = relatedEntityId,
                CreatedAt         = DateTime.UtcNow
            });
            await db.SaveChangesAsync();
        }
        catch (Exception ex)
        {
            logger.LogWarning(ex, "In-app notification failed for recipient {RecipientId} — continuing", recipientId);
        }
        finally
        {
            db.ChangeTracker.AutoDetectChangesEnabled = true;
        }
    }

    // ═══════════════════════════════════════════════════════════════════════════
    // PRIVATE HELPERS
    // ═══════════════════════════════════════════════════════════════════════════

    /// <summary>
    /// Sends to all active Managers/Admins in the DB, plus any hardcoded config recipients.
    /// </summary>
    private async Task SendToMaintenanceTeamAsync(string subject, string body)
    {
        var dbEmails = await GetEmailsForRolesAsync("Manager", "Admin");
        var configEmails = new[] { _managerEmail, _supervisorEmail }
            .Where(e => !string.IsNullOrWhiteSpace(e))
            .Select(e => e!);

        var all = dbEmails.Union(configEmails, StringComparer.OrdinalIgnoreCase).ToList();

        if (all.Count == 0)
        {
            logger.LogWarning("No maintenance team recipients configured. " +
                "Set Email:ManagerEmail and/or Email:SupervisorEmail in App Service configuration.");
            return;
        }

        await SendEmailToManyAsync(all, subject, body);
    }

    private async Task<List<string>> GetEmailsForRolesAsync(params string[] roles)
    {
        return await db.Users
            .Where(u => u.IsActive && roles.Contains(u.Role) && u.Email != null && u.Email != "")
            .Select(u => u.Email!)
            .Distinct()
            .ToListAsync();
    }

    private string PlatformUrl()
    {
        var url = config["App:BaseUrl"] ?? "https://logistics.desiconapp.com";
        return $"Platform: {url}";
    }

    private async Task SendEmailAsync(string to, string subject, string body)
    {
        if (emailClient != null)
        {
            // Azure Communication Services path
            try
            {
                var msg = new EmailMessage(
                    _fromAddress,
                    new EmailRecipients([new EmailAddress(to)]),
                    new EmailContent(subject) { PlainText = body });
                await emailClient.SendAsync(Azure.WaitUntil.Started, msg);
                logger.LogDebug("ACS email sent to {To}: {Subject}", to, subject);
            }
            catch (Exception ex)
            {
                logger.LogError(ex, "ACS email failed to {To}: {Subject}", to, subject);
            }
            return;
        }

        // SMTP path (Office 365 / smtp.office365.com)
        await SendSmtpEmailAsync(to, subject, body);
    }

    /// <summary>
    /// Sends one identical message to several recipients over a <em>single</em> SMTP
    /// connection.
    ///
    /// Previously each recipient got its own SmtpClient, meaning a fresh TCP + TLS
    /// + AUTH handshake to Office 365 per person. With a full logistics team that
    /// added ten-plus seconds to every submit, because the request waited for all
    /// of them in sequence.
    /// </summary>
    private async Task SendEmailToManyAsync(IEnumerable<string> recipients, string subject, string body)
    {
        var to = recipients
            .Where(e => !string.IsNullOrWhiteSpace(e))
            .Select(e => e.Trim())
            .Distinct(StringComparer.OrdinalIgnoreCase)
            .ToList();

        if (to.Count == 0) return;

        // Azure Communication Services path handles its own batching.
        if (emailClient != null)
        {
            foreach (var email in to) await SendEmailAsync(email, subject, body);
            return;
        }

        if (string.IsNullOrWhiteSpace(_smtpUser) || string.IsNullOrWhiteSpace(_smtpPass))
        {
            logger.LogWarning("SMTP credentials not configured — {Count} notification(s) not sent.", to.Count);
            return;
        }

        try
        {
            using var client = new SmtpClient(_smtpHost, _smtpPort)
            {
                EnableSsl      = _smtpSsl,
                Credentials    = new NetworkCredential(_smtpUser, _smtpPass),
                DeliveryMethod = SmtpDeliveryMethod.Network
            };

            using var msg = new MailMessage
            {
                From       = new MailAddress(_fromAddress, _fromName),
                Subject    = subject,
                Body       = body,
                IsBodyHtml = false
            };
            foreach (var email in to) msg.To.Add(email);

            await client.SendMailAsync(msg);
            logger.LogDebug("SMTP email sent to {Count} recipient(s): {Subject}", to.Count, subject);
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "SMTP batch email failed for {Count} recipient(s): {Subject}", to.Count, subject);
        }
    }

    private async Task SendSmtpEmailAsync(string to, string subject, string body)
    {
        if (string.IsNullOrWhiteSpace(_smtpUser) || string.IsNullOrWhiteSpace(_smtpPass))
        {
            logger.LogWarning(
                "SMTP credentials not configured — email not sent to {To}. " +
                "Set Email__SmtpUsername and Email__SmtpPassword in App Service Configuration.",
                to);
            return;
        }

        try
        {
            using var client = new SmtpClient(_smtpHost, _smtpPort)
            {
                EnableSsl   = _smtpSsl,
                Credentials = new NetworkCredential(_smtpUser, _smtpPass),
                DeliveryMethod = SmtpDeliveryMethod.Network
            };

            var msg = new MailMessage
            {
                From       = new MailAddress(_fromAddress, _fromName),
                Subject    = subject,
                Body       = body,
                IsBodyHtml = false
            };
            msg.To.Add(to);

            await client.SendMailAsync(msg);
            logger.LogDebug("SMTP email sent to {To}: {Subject}", to, subject);
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "SMTP email failed to {To}: {Subject}", to, subject);
        }
    }
}
