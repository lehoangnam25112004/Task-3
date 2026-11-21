using System.ComponentModel.DataAnnotations;
using ConstructionProjectManagement.Models;
using Microsoft.AspNetCore.Http;

namespace ConstructionProjectManagement.ViewModels;

public sealed class DocumentListViewModel
{
    public required IReadOnlyCollection<ProjectDocument> Documents { get; init; }
    public required IReadOnlyCollection<DocumentCategory> Categories { get; init; }
    public IReadOnlyCollection<Project> Projects { get; init; } = [];
    public IReadOnlyCollection<Contract> Contracts { get; init; } = [];
    public IReadOnlyCollection<ApplicationUser> Uploaders { get; init; } = [];
    public string? Keyword { get; init; }
    public int? ProjectId { get; init; }
    public int? CategoryId { get; init; }
    public int? ContractId { get; init; }
    public DocumentApprovalStatus? ApprovalStatus { get; init; }
    public string? UploadedById { get; init; }
    public DateTime? UploadedFrom { get; init; }
    public DateTime? UploadedTo { get; init; }
    public bool ShowArchived { get; init; }
    public int Page { get; init; }
    public int PageSize { get; init; }
    public int TotalItems { get; init; }
    public int TotalPages => Math.Max(1, (int)Math.Ceiling(TotalItems / (double)PageSize));
    public bool CanCreate { get; init; }
    public bool CanExport { get; init; }
}

public sealed class DocumentUploadItemViewModel
{
    [Required(ErrorMessage = "Vui lòng chọn tệp.")]
    public IFormFile? File { get; set; }

    [Required(ErrorMessage = "Vui lòng nhập tiêu đề tài liệu.")]
    [StringLength(250)]
    public string Title { get; set; } = string.Empty;

    [Required(ErrorMessage = "Vui lòng chọn danh mục.")]
    public int? CategoryId { get; set; }

    [StringLength(2000)]
    public string? Description { get; set; }
}

public sealed class DocumentCreateViewModel
{
    public int? ProjectId { get; set; }
    public int? ContractId { get; set; }
    public int? AmendmentId { get; set; }
    public bool IsSystemWide { get; set; }
    public List<DocumentUploadItemViewModel> Files { get; set; } = [];
    public IReadOnlyCollection<DocumentCategory> Categories { get; init; } = [];
    public IReadOnlyCollection<Project> Projects { get; init; } = [];
    public IReadOnlyCollection<Contract> Contracts { get; init; } = [];
    public IReadOnlyCollection<ContractAmendment> Amendments { get; init; } = [];
    public bool CanCreateSystemWide { get; init; }
}

public sealed class DocumentEditViewModel
{
    public int Id { get; set; }

    [Required(ErrorMessage = "Vui lòng nhập tiêu đề tài liệu.")]
    [StringLength(250)]
    public string Title { get; set; } = string.Empty;

    [StringLength(2000)]
    public string? Description { get; set; }

    [Required(ErrorMessage = "Vui lòng chọn danh mục.")]
    public int? CategoryId { get; set; }

    public int? ProjectId { get; set; }
    public int? ContractId { get; set; }
    public int? AmendmentId { get; set; }
    public bool IsSystemWide { get; set; }

    [StringLength(1000)]
    public string? VersionNote { get; set; }

    public IReadOnlyCollection<DocumentCategory> Categories { get; init; } = [];
    public IReadOnlyCollection<Project> Projects { get; init; } = [];
    public IReadOnlyCollection<Contract> Contracts { get; init; } = [];
    public IReadOnlyCollection<ContractAmendment> Amendments { get; init; } = [];
    public DocumentEditLock? EditLock { get; init; }
}

public sealed class DocumentDetailsViewModel
{
    public required ProjectDocument Document { get; init; }
    public IReadOnlyCollection<DocumentVersion> Versions { get; init; } = [];
    public IReadOnlyCollection<DocumentAuditLog> AuditLogs { get; init; } = [];
    public IReadOnlyCollection<DocumentApprovalLog> ApprovalLogs { get; init; } = [];
    public DocumentPermissionSet Permissions { get; init; } = new();
}

