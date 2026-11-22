using System.ComponentModel.DataAnnotations;

namespace ConstructionProjectManagement.Models;

public class ProjectPhase
{
    public int Id { get; set; }

    public int ProjectId { get; set; }

    public Project Project { get; set; } = null!;

    [MaxLength(200)]
    public required string Name { get; set; }

    public int SequenceOrder { get; set; }

    public ProjectPhaseStatus Status { get; set; } = ProjectPhaseStatus.NotStarted;

    public bool IsArchived { get; set; }

    public DateTime? ArchivedAt { get; set; }

    [MaxLength(450)]
    public string? ArchivedByUserId { get; set; }

    public ProjectPhaseStatus? StatusBeforeArchive { get; set; }

    public Guid? ArchiveGroupId { get; set; }

    public ICollection<WorkItem> WorkItems { get; set; } = [];
}
