namespace LogisticsApi.Models.Entities;

/// <summary>
/// One of Desicon's projects and the project manager who gives the first
/// approval on its material transport requests.
///
/// Material transport is project work only, and each project has its own PM.
/// Routing by project means a PMC request goes to the PMC project manager alone,
/// instead of to every head of department in the company.
/// </summary>
public class Project
{
    public Guid Id { get; set; }
    public string Name { get; set; } = string.Empty;

    /// <summary>The PM who approves this project's material transport (stage 1).</summary>
    public Guid? ManagerUserId { get; set; }

    public bool IsActive { get; set; } = true;
    public DateTime CreatedAt { get; set; }

    public User? Manager { get; set; }
}
