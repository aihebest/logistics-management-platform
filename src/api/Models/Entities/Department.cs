namespace LogisticsApi.Models.Entities;

/// <summary>
/// A Desicon department and the head who verifies its travel requests.
///
/// The head is held here rather than on the user because one person can head
/// more than one department — Maurizio Benassi heads both Business Development
/// and Contracts — which a single field on the user could never express.
///
/// Keeping departments as data rather than a hard-coded list also means the
/// logistics team can add one, or move a headship, without a code change.
/// </summary>
public class Department
{
    public Guid Id { get; set; }
    public string Name { get; set; } = string.Empty;

    /// <summary>
    /// The HOD who verifies travel requests raised by this department. Nullable
    /// because a department can exist before its head has a platform account —
    /// in that case verification falls back to any HOD and a warning is logged.
    /// </summary>
    public Guid? HodUserId { get; set; }

    public bool IsActive { get; set; } = true;
    public DateTime CreatedAt { get; set; }

    public User? Hod { get; set; }
    public ICollection<User> Members { get; set; } = [];
}
