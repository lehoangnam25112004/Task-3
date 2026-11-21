using System.ComponentModel.DataAnnotations;

namespace ConstructionProjectManagement.Models;

public class DocumentVersion
{
    public int Id { get; set; }
    public int DocumentId { get; set; }
    public ProjectDocument Document { get; set; } = null!;
    public int VersionNumber { get; set; }
    [MaxLength(255)]
    public required string OriginalFileName { get; set; }
    [MaxLength(255)]
    public required string StoredFileName { get; set; }
    [MaxLength(500)]
    public required string RelativeStoragePath { get; set; }
    [MaxLength(20)]
    public string FileExtension { get; set; } = string.Empty;
    [MaxLength(120)]
    public required string ContentType { get; set; }
    public long FileSize { get; set; }
    public long FileSizeBytes { get; set; }
    [MaxLength(64)]
    public string Sha256Hash { get; set; } = string.Empty;
    [MaxLength(500)]
    public string? ChangeNote { get; set; }
    [MaxLength(1000)]
    public string? VersionNote { get; set; }
    public DocumentApprovalStatus ApprovalStatus { get; set; } = DocumentApprovalStatus.Draft;
    public required string UploadedById { get; set; }
    public ApplicationUser UploadedBy { get; set; } = null!;
    public string CreatedByUserId { get; set; } = null!;
    public DateTime UploadedAt { get; set; } = DateTime.UtcNow;
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    public bool IsCurrent { get; set; } = true;
}

public class DocumentAuditLog
{
    public int Id { get; set; }
    public int DocumentId { get; set; }
    public ProjectDocument Document { get; set; } = null!;
    [MaxLength(50)]
    public string DocumentCode { get; set; } = string.Empty;
    public int? VersionNumber { get; set; }
    [MaxLength(60)]
    public required string Action { get; set; }
    public required string PerformedById { get; set; }
    public ApplicationUser PerformedBy { get; set; } = null!;
    public string ActorUserId { get; set; } = null!;
    [MaxLength(200)]
    public string ActorDisplayName { get; set; } = string.Empty;
    [MaxLength(100)]
    public string ActorRole { get; set; } = string.Empty;
    public DateTime PerformedAt { get; set; } = DateTime.UtcNow;
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    public string? OldValuesJson { get; set; }
    public string? NewValuesJson { get; set; }
    [MaxLength(1000)]
    public string? Comment { get; set; }
    [MaxLength(1000)]
    public string? Reason { get; set; }
    [MaxLength(64)]
    public string? IpAddress { get; set; }
}

public class DocumentApprovalLog
{
    public int Id { get; set; }
    public int DocumentId { get; set; }
    public ProjectDocument Document { get; set; } = null!;
    public int DocumentVersionId { get; set; }
    public DocumentVersion DocumentVersion { get; set; } = null!;
    public int StepNumber { get; set; }
    [MaxLength(50)]
    public required string Action { get; set; }
    public required string ActorUserId { get; set; }
    public ApplicationUser Actor { get; set; } = null!;
    [MaxLength(100)]
    public required string ActorRole { get; set; }
    [MaxLength(2000)]
    public string? CommentOrReason { get; set; }
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
}

public class DocumentEditLock
{
    public int DocumentId { get; set; }
    public ProjectDocument Document { get; set; } = null!;
    public required string LockedById { get; set; }
    public ApplicationUser LockedBy { get; set; } = null!;
    public string LockedByUserId { get; set; } = null!;
    [MaxLength(200)]
    public string LockedByDisplayName { get; set; } = string.Empty;
    public DateTime LockedAt { get; set; } = DateTime.UtcNow;
    public DateTime AcquiredAt { get; set; } = DateTime.UtcNow;
    public DateTime ExpiresAt { get; set; }
    public bool IsReleased { get; set; }
}

public sealed class DocumentSearchOptions
{
    public string? Keyword { get; set; }
    public int? ProjectId { get; set; }
    public int? CategoryId { get; set; }
    public int? ContractId { get; set; }
    public DocumentApprovalStatus? ApprovalStatus { get; set; }
    public string? UploadedById { get; set; }
    public DateTime? UploadedFrom { get; set; }
    public DateTime? UploadedTo { get; set; }
    public bool ShowArchived { get; set; }
    public int Page { get; set; } = 1;
    public int PageSize { get; set; } = 20;
}

public sealed record DocumentPage(
    IReadOnlyCollection<ProjectDocument> Documents,
    int Page,
    int PageSize,
    int TotalItems);

public sealed record DocumentFileAccessResult(
    ProjectDocument? Document,
    DocumentVersion? Version,
    string? FilePath,
    string? Error);

public sealed class DocumentPermissionSet
{
    public bool CanEdit { get; init; }
    public bool CanSubmit { get; init; }
    public bool CanReview { get; init; }
    public bool CanApprove { get; init; }
    public bool CanArchive { get; init; }
    public bool CanRestore { get; init; }
    public bool CanDownload { get; init; }
}
