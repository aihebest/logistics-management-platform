using Azure.Communication.Email;
using LogisticsApi.Data;
using LogisticsApi.Models.Entities;

namespace LogisticsApi.Services;

public interface INotificationService
{
    Task SendAssignmentConfirmedAsync(Assignment assignment);
    Task SendMaintenanceDueAsync(Models.Entities.MaintenanceRecord record, int daysUntilDue);
    Task SendMaintenanceOverdueAsync(Models.Entities.MaintenanceRecord record);
    Task NotifyInAppAsync(Guid recipientId, string type, string subject, string body,
                          string? relatedEntityType = null, string? relatedEntityId = null);
}

public class NotificationService(
    AppDbContext db,
    IConfiguration config,
    ILogger<NotificationService> logger,
    EmailClient? emailClient = null) : INotificationService
{
    private readonly string _fromAddress = config["Email:FromAddress"] ?? "noreply@logistics-demo.local";

    public async Task SendAssignmentConfirmedAsync(Assignment assignment)
    {
        var driver = assignment.Driver;
        var subject = $"Trip Assignment — {assignment.TripRequest.Purpose}";
        var body = $"""
            Hi {driver.FullName},

            You have been assigned to a trip:

            Purpose:     {assignment.TripRequest.Purpose}
            Pickup:      {assignment.TripRequest.PickupLocation}
            Destination: {assignment.TripRequest.DestinationLocation}
            Start Time:  {assignment.StartTime:f}
            Vehicle:     {assignment.Vehicle.Make} {assignment.Vehicle.Model} ({assignment.Vehicle.RegistrationNo})

            Please acknowledge your availability on the platform.

            Logistics Platform
            """;

        await NotifyInAppAsync(driver.Id, "AssignmentConfirmed", subject,
            $"New trip: {assignment.TripRequest.Purpose} — {assignment.TripRequest.PickupLocation} → {assignment.TripRequest.DestinationLocation}",
            "Assignment", assignment.Id.ToString());

        await SendEmailAsync(driver.Email, subject, body);
    }

    public async Task SendMaintenanceDueAsync(Models.Entities.MaintenanceRecord record, int daysUntilDue)
    {
        var vehicle = record.Vehicle;
        var subject = $"Maintenance Due in {daysUntilDue} days — {vehicle.RegistrationNo}";
        var body = $"""
            Maintenance Reminder

            Vehicle:        {vehicle.Make} {vehicle.Model} ({vehicle.RegistrationNo})
            Service Type:   {record.Type}
            Scheduled Date: {record.ScheduledDate:D}
            Days Until Due: {daysUntilDue}
            Vendor:         {record.VendorName ?? "Not specified"}

            Please arrange for this vehicle to be taken in for service.

            Logistics Platform
            """;

        await SendEmailAsync(_fromAddress, subject, body);
        logger.LogInformation("Maintenance due notification sent for vehicle {Reg}, {Days} days", vehicle.RegistrationNo, daysUntilDue);
    }

    public async Task SendMaintenanceOverdueAsync(Models.Entities.MaintenanceRecord record)
    {
        var vehicle = record.Vehicle;
        var subject = $"OVERDUE Maintenance — {vehicle.RegistrationNo}";
        var body = $"""
            ⚠️ OVERDUE MAINTENANCE ALERT

            Vehicle:        {vehicle.Make} {vehicle.Model} ({vehicle.RegistrationNo})
            Service Type:   {record.Type}
            Was Scheduled:  {record.ScheduledDate:D}
            Now Overdue By: {(DateOnly.FromDateTime(DateTime.UtcNow).DayNumber - record.ScheduledDate.DayNumber)} days

            Immediate action required. This vehicle may not be fit for service.

            Logistics Platform
            """;

        await SendEmailAsync(_fromAddress, subject, body);
        logger.LogWarning("Overdue maintenance alert for vehicle {Reg}", vehicle.RegistrationNo);
    }

    public async Task NotifyInAppAsync(Guid recipientId, string type, string subject, string body,
                                       string? relatedEntityType = null, string? relatedEntityId = null)
    {
        db.Notifications.Add(new Notification
        {
            Id = Guid.NewGuid(),
            RecipientId = recipientId,
            Channel = "InApp",
            Type = type,
            Subject = subject,
            Body = body,
            Status = "Sent",
            SentAt = DateTime.UtcNow,
            RelatedEntityType = relatedEntityType,
            RelatedEntityId = relatedEntityId,
            CreatedAt = DateTime.UtcNow
        });
        await db.SaveChangesAsync();
    }

    private async Task SendEmailAsync(string to, string subject, string body)
    {
        if (emailClient == null)
        {
            await SendSmtpEmailAsync(to, subject, body);
            return;
        }

        try
        {
            var msg = new EmailMessage(
                _fromAddress,
                new EmailRecipients([new EmailAddress(to)]),
                new EmailContent(subject) { PlainText = body });
            await emailClient.SendAsync(Azure.WaitUntil.Started, msg);
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Failed to send email to {To}", to);
        }
    }

    private async Task SendSmtpEmailAsync(string to, string subject, string body)
    {
        var host = config["Email:SmtpHost"] ?? "localhost";
        var port = int.Parse(config["Email:SmtpPort"] ?? "1025");

        try
        {
            using var client = new System.Net.Mail.SmtpClient(host, port);
            client.EnableSsl = bool.Parse(config["Email:UseSsl"] ?? "false");
            var from = config["Email:FromAddress"] ?? "noreply@logistics-demo.local";
            await client.SendMailAsync(from, to, subject, body);
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Failed to send SMTP email to {To}", to);
        }
    }
}
