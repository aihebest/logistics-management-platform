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
    ///
    /// For the executive department this is the MD, who approves directors'
    /// travel.
    /// </summary>
    public Guid? HodUserId { get; set; }

    /// <summary>
    /// Only meaningful for the executive department: the DMD, who approves the
    /// MD's own travel. Directors' travel needs a senior second signature, and
    /// nobody should sign off travel for the person they report to.
    /// </summary>
    public Guid? DeputyHodUserId { get; set; }

    /// <summary>
    /// Marks the department for the MD, DMD and directors. Their travel skips
    /// head-of-department verification — they are the heads — and goes straight
    /// to the MD, or to the DMD when the MD is the one travelling.
    /// </summary>
    public bool IsExecutive { get; set; }

    public bool IsActive { get; set; } = true;
    public DateTime CreatedAt { get; set; }

    public User? Hod { get; set; }
    public User? DeputyHod { get; set; }
    public ICollection<User> Members { get; set; } = [];
}
