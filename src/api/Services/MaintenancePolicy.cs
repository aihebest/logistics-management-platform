namespace LogisticsApi.Services;

/// <summary>
/// Shared maintenance thresholds, so the API and the reminder job cannot drift
/// apart on what counts as overdue.
/// </summary>
public static class MaintenancePolicy
{
    /// <summary>
    /// How long a reported job may stay open before it is treated as overdue.
    ///
    /// The date column behind this used to mean "scheduled for", so anything past
    /// its date was late by definition. It now means "reported on", which is
    /// always today or earlier — without a grace period every open job would flip
    /// to Overdue the day after it was raised and the badge would stop carrying
    /// any information. Seven days matches the "Long-Standing" threshold the
    /// General Service tracker already shows, so the two systems agree on which
    /// vehicles need chasing.
    /// </summary>
    public const int OverdueGraceDays = 7;

    /// <summary>
    /// A job reported on or before this date, and still open, is overdue.
    /// </summary>
    public static DateOnly OverdueCutoff(DateOnly today) => today.AddDays(-OverdueGraceDays);

    /// <summary>Days before a vehicle's next service that a reminder goes out.</summary>
    public static readonly int[] ServiceReminderCheckpoints = [14, 7, 3, 0];
}
