using System.ComponentModel.DataAnnotations;

namespace ConstructionProjectManagement.Models;

public class WorkItem
{
    public int Id { get; set; }

    public int ProjectId { get; set; }

    public Project Project { get; set; } = null!;

    public int ProjectPhaseId { get; set; }

    public ProjectPhase ProjectPhase { get; set; } = null!;

    public int? ParentWorkItemId { get; set; }

    public WorkItem? ParentWorkItem { get; set; }

    public ICollection<WorkItem> Children { get; set; } = [];

    [MaxLength(50)]
    public required string WbsCode { get; set; }

    [MaxLength(250)]
    public required string Name { get; set; }

    [MaxLength(1000)]
    public string? Description { get; set; }

    public int Level { get; set; }

    public int SequenceOrder { get; set; }

    public WbsItemStatus Status { get; set; } = WbsItemStatus.NotStarted;

    public bool IsArchived { get; set; }

    public DateTime? ArchivedAt { get; set; }

    [MaxLength(450)]
    public string? ArchivedByUserId { get; set; }

    public WbsItemStatus? StatusBeforeArchive { get; set; }

    public Guid? ArchiveGroupId { get; set; }

    [MaxLength(450)]
    public string? CreatedByUserId { get; set; }

    public DateTime? CreatedAt { get; set; }

    [MaxLength(450)]
    public string? UpdatedByUserId { get; set; }

    public DateTime? UpdatedAt { get; set; }
}
