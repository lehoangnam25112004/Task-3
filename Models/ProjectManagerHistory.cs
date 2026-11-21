using System.ComponentModel.DataAnnotations;

namespace ConstructionProjectManagement.Models;

public class ProjectManagerHistory
{
    public int Id { get; set; }

    public int ProjectId { get; set; }

    public Project Project { get; set; } = null!;

    public string? PreviousProjectManagerId { get; set; }

    public ApplicationUser? PreviousProjectManager { get; set; }

    public string? NewProjectManagerId { get; set; }

    public ApplicationUser? NewProjectManager { get; set; }

    public DateTime? StartedAt { get; set; }

    public DateTime? EndedAt { get; set; }

    [MaxLength(500)]
    public string? ChangeReason { get; set; }
}
