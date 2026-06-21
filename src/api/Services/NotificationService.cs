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

    // ── Assignment ──────────────────────────────────────────────────────────────
    Task SendAssignmentConfirmedAsync(Assignment assignment);

    // ── Maintenance ─────────────────────────────────────────────────────────────
    Task SendMaintenanceDueAsync(MaintenanceRecord record, int daysUntilDue);
    Task SendMaintenanceOverdueAsync(MaintenanceRecord record);

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

        // Notify all active Coordinators and Managers by email
        var recipients = await GetEmailsForRolesAsync("Coordinator", "Manager", "Admin");
        foreach (var email in recipients)
            await SendEmailAsync(email, subject, body);

        // Also in-app notify Coordinators and Managers
        var users = await db.Users
            .Where(u => u.IsActive && (u.Role == "Coordinator" || u.Role == "Manager" || u.Role == "Admin"))
            .ToListAsync();
        foreach (var u in users)
            await NotifyInAppAsync(u.Id, "TripRequestSubmitted", subject,
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

        var recipients = await GetEmailsForRolesAsync("Coordinator", "Manager", "Admin");
        foreach (var email in recipients)
            await SendEmailAsync(email, subject, body);
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

    public async Task SendMaintenanceDueAsync(MaintenanceRecord record, int daysUntilDue)
    {
        var vehicle = record.Vehicle;
        var subject = $"Maintenance Due in {daysUntilDue} day{(daysUntilDue == 1 ? "" : "s")} — {vehicle.RegistrationNo}";
        var body    = $"""
            Maintenance Reminder — Action Required

            Vehicle:        {vehicle.Make} {vehicle.Model} ({vehicle.RegistrationNo})
            Service Type:   {record.Type}
            Scheduled Date: {record.ScheduledDate:D}
            Days Until Due: {daysUntilDue}
            Vendor:         {record.VendorName ?? "Not specified"}
            Notes:          {record.Notes ?? "None"}

            Please arrange for this vehicle to be taken in for service before the scheduled date.

            {PlatformUrl()}
            """;

        await SendToMaintenanceTeamAsync(subject, body);
        logger.LogInformation("Maintenance due reminder sent: {Vehicle} — {Days} days", vehicle.RegistrationNo, daysUntilDue);
    }

    public async Task SendMaintenanceOverdueAsync(MaintenanceRecord record)
    {
        var vehicle   = record.Vehicle;
        var daysOver  = DateOnly.FromDateTime(DateTime.UtcNow).DayNumber - record.ScheduledDate.DayNumber;
        var subject   = $"OVERDUE Maintenance — {vehicle.RegistrationNo} ({daysOver} days overdue)";
        var body      = $"""
            ⚠️ OVERDUE MAINTENANCE ALERT — Immediate Action Required

            Vehicle:        {vehicle.Make} {vehicle.Model} ({vehicle.RegistrationNo})
            Service Type:   {record.Type}
            Was Scheduled:  {record.ScheduledDate:D}
            Days Overdue:   {daysOver}
            Vendor:         {record.VendorName ?? "Not specified"}

            This vehicle may not be fit for continued service. Please arrange maintenance immediately and update the record in the platform.

            {PlatformUrl()}
            """;

        await SendToMaintenanceTeamAsync(subject, body);
        logger.LogWarning("Overdue maintenance alert sent: {Vehicle} — {Days} days overdue", vehicle.RegistrationNo, daysOver);
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

        foreach (var email in all)
            await SendEmailAsync(email, subject, body);
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
