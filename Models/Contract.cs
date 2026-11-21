using System.ComponentModel.DataAnnotations;

namespace ConstructionProjectManagement.Models;

public class Contract
{
    public int Id { get; set; }

    [Obsolete("Use ContractProjects for the project associations.")]
    public int ProjectId { get; set; }

    [Obsolete("Use ContractProjects for the project associations.")]
    public Project Project { get; set; } = null!;

    [MaxLength(50)]
    public required string ContractNumber { get; set; }

    [MaxLength(200)]
    public string Title { get; set; } = string.Empty;

    [MaxLength(200)]
    public string? ClientName { get; set; }

    [MaxLength(200)]
    public string? ContractorName { get; set; }

    public decimal ContractValue { get; set; }
    public ContractType ContractType { get; set; } = ContractType.Construction;
    public int? ClientPartyId { get; set; }
    public ContractParty? ClientParty { get; set; }
    public int? ContractorPartyId { get; set; }
    public ContractParty? ContractorParty { get; set; }
    public decimal OriginalValue { get; set; }
    public decimal CurrentValue { get; set; }
    [MaxLength(10)]
    public string Currency { get; set; } = "VND";
    [MaxLength(4000)]
    public string? ScopeOfWork { get; set; }
    [Range(0, 100)]
    public int ProgressPercentage { get; set; }
    public int ApprovalStep { get; set; }
    [MaxLength(1000)]
    public string? RejectionReason { get; set; }
    [MaxLength(1000)]
    public string? StatusReason { get; set; }
    public string? CreatedById { get; set; }
    public ApplicationUser? CreatedBy { get; set; }
    public string? ReviewedById { get; set; }
    public ApplicationUser? ReviewedBy { get; set; }
    public DateTime? ReviewedAt { get; set; }
    public string? ApprovedById { get; set; }
    public ApplicationUser? ApprovedBy { get; set; }
    public DateTime? ApprovedAt { get; set; }
    public string? ArchivedById { get; set; }
    public ApplicationUser? ArchivedBy { get; set; }
    public DateTime? ArchivedAt { get; set; }
    [MaxLength(500)]
    public string? ArchiveReason { get; set; }
    public DateTime? UpdatedAt { get; set; }

    public DateTime? SignedDate { get; set; }

    public DateTime? EffectiveDate { get; set; }
    public DateTime? EndDate { get; set; }
    [Obsolete("Use EndDate.")]
    public DateTime? ExpirationDate { get => EndDate; set => EndDate = value; }

    public ContractStatus Status { get; set; } = ContractStatus.Draft;

    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

    public bool IsArchived { get; set; }

    public ICollection<ProjectDocument> Documents { get; set; } = [];
    public ICollection<ContractProject> ContractProjects { get; set; } = [];
    public ICollection<ContractAmendment> Amendments { get; set; } = [];
    public ICollection<ContractValueHistory> ValueHistories { get; set; } = [];
    public ICollection<ContractAuditLog> AuditLogs { get; set; } = [];
}
