using System.ComponentModel.DataAnnotations;

namespace ConstructionProjectManagement.Models;

public class ProjectDocument
{
    public int Id { get; set; }

    public int? ProjectId { get; set; }

    public Project? Project { get; set; }

    public int? ContractId { get; set; }

    public Contract? Contract { get; set; }
    public int? AmendmentId { get; set; }
    public ContractAmendment? Amendment { get; set; }
    [MaxLength(50)]
    public string DocumentCode { get; set; } = string.Empty;
    [MaxLength(250)]
    public string Title { get; set; } = string.Empty;
    public int? CategoryId { get; set; }
    public DocumentCategory? CategoryEntity { get; set; }
    [MaxLength(200)]
    public string? LegacyCategory { get; set; }
    [MaxLength(2000)]
    public string? Description { get; set; }
    public bool IsSystemWide { get; set; }
    public int CurrentVersionNumber { get; set; } = 1;
    public int CurrentApprovalStep { get; set; }
    public DocumentApprovalStatus ApprovalStatus { get; set; } = DocumentApprovalStatus.Draft;
    public string? SubmittedByUserId { get; set; }
    public string? ApprovedById { get; set; }
    public ApplicationUser? ApprovedBy { get; set; }
    public DateTime? ApprovedAt { get; set; }
    [MaxLength(1000)]
    public string? RejectionReason { get; set; }
    public string? ArchivedById { get; set; }
    public ApplicationUser? ArchivedBy { get; set; }
    public DateTime? ArchivedAt { get; set; }
    [MaxLength(500)]
    public string? ArchiveReason { get; set; }
    public DateTime? RestoredAt { get; set; }
    public string? RestoredByUserId { get; set; }
    public string CreatedByUserId { get; set; } = null!;
    public string? UpdatedByUserId { get; set; }
    public DateTime? UpdatedAt { get; set; }

    [MaxLength(200)]
    public required string FileName { get; set; }

    [MaxLength(200)]
    public required string StoredFileName { get; set; }

    [MaxLength(100)]
    public required string ContentType { get; set; }

    public long FileSize { get; set; }

    [MaxLength(100)]
    public required string Category { get; set; }

    public string UploadedById { get; set; } = null!;

    public ApplicationUser UploadedBy { get; set; } = null!;

    public ICollection<DocumentVersion> Versions { get; set; } = [];
    public ICollection<DocumentAuditLog> AuditLogs { get; set; } = [];
    public ICollection<DocumentApprovalLog> ApprovalLogs { get; set; } = [];
    public DocumentEditLock? EditLock { get; set; }

    public DateTime UploadedAt { get; set; } = DateTime.UtcNow;

    public bool IsArchived { get; set; }
}
